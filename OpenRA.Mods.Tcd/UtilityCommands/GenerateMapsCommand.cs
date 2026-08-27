#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Tcd.UtilityCommands
{
	// Batch map generation. The map chooser can already generate one map at a time,
	// which is fine for playing and slow for curating: building a pack means judging
	// dozens of maps, and every one of them costs a trip through the menus.
	//
	// This runs the same generator headlessly and writes both the .oramap and its
	// preview image, so a batch can be sifted by looking at pictures.
	sealed class GenerateMapsCommand : IUtilityCommand
	{
		const string SeedKey = "Seed";
		const string GeneratorKey = "Generator";
		const string TilesetKey = "Tileset";
		const string SizeKey = "Size";
		const string TitleKey = "Title";
		const string AuthorKey = "Author";
		const string PlayersOption = "Players";

		string IUtilityCommand.Name => "--generate-maps";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 3 && Exts.TryParseInt32Invariant(args[2], out var count) && count > 0;
		}

		[Desc(
			"OUTPUTDIR COUNT [KEY=VALUE ...]",
			"Generate COUNT maps into OUTPUTDIR, writing each as an .oramap next to a .png preview.",
			"Generator, Tileset, Size (WIDTHxHEIGHT), Title and Author configure the run;",
			"Seed is the base seed and each map takes the next one, so a run repeats exactly.",
			"Every other key is a generator option, for example Players=8 or TerrainType=Mountains.",
			"Options left unset take the generator's own default, including the hidden groups",
			"that carry most of the parameters. Tileset defaults to the generator's first.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: the engine assumes Game.ModData is set. FuzzMapGeneratorCommand does the same.
			var modData = Game.ModData = utility.ModData;

			var outputDir = args[1];
			var count = Exts.ParseInt32Invariant(args[2]);

			var settings = new Dictionary<string, string>();
			foreach (var arg in args.Skip(3))
			{
				var split = arg.IndexOf('=');
				if (split <= 0)
					throw new ArgumentException($"`{arg}` is not KEY=VALUE");

				settings[arg[..split]] = arg[(split + 1)..];
			}

			var generatorType = Take(settings, GeneratorKey) ?? "classic";
			var generators = modData.DefaultRules.Actors[SystemActors.EditorWorld].TraitInfos<IEditorMapGeneratorInfo>();
			var generator = generators.FirstOrDefault(info => info.Type == generatorType)
				?? throw new ArgumentException(
					$"No map generator `{generatorType}`. Known: {string.Join(", ", generators.Select(info => info.Type))}");

			var tileset = Take(settings, TilesetKey) ?? generator.Tilesets[0];
			if (!generator.Tilesets.Contains(tileset))
				throw new ArgumentException(
					$"`{tileset}` is not a tileset of `{generatorType}`. Known: {string.Join(", ", generator.Tilesets)}");

			var size = ParseSize(Take(settings, SizeKey) ?? "120x120");
			var title = Take(settings, TitleKey) ?? "TCD Generated";
			var author = Take(settings, AuthorKey) ?? "Tactics & Command Dynamics";

			var baseSeedValue = Take(settings, SeedKey);
			var baseSeed = baseSeedValue != null ? Exts.ParseInt32Invariant(baseSeedValue) : Environment.TickCount;

			// An unknown key is a typo, not a default: dropping it quietly would produce a
			// batch that looks configured and is not.
			var optionIds = generator.Options.Select(o => o.Id).ToHashSet();
			var unknown = string.Join(", ", settings.Keys.Where(k => !optionIds.Contains(k)).Order());
			if (unknown.Length > 0)
			{
				var offered = string.Join(", ", optionIds.Order());
				throw new ArgumentException($"Unknown option(s): {unknown}. This generator offers: {offered}");
			}

			var terrainInfo = modData.DefaultTerrainInfo[tileset];
			var options = ResolveOptions(generator, terrainInfo, tileset, size, settings);

			Directory.CreateDirectory(outputDir);

			var players = options.TryGetValue(PlayersOption, out var playersValue) ? playersValue : "0";
			var slug = Slug($"{title}-{players}p");
			var written = 0;
			var failed = 0;

			Console.WriteLine($"Generating {count} map(s) into {outputDir}");
			Console.WriteLine($"  {generatorType}, {tileset}, {size.Width}x{size.Height}, {players} players, base seed {baseSeed}");

			for (var i = 0; i < count; i++)
			{
				var seed = baseSeed + i;
				var suffix = i.ToString("000", NumberFormatInfo.InvariantInfo);
				var name = $"{slug}-{suffix}";

				var generationArgs = new MapGenerationArgs
				{
					Generator = generatorType,
					Tileset = tileset,
					Size = size,
					Title = $"{title} {suffix}",
					Author = author,
					Options = new Dictionary<string, string>(options),
				};

				generationArgs.Options[SeedKey] = FieldSaver.FormatValue(seed);

				try
				{
					var map = generator.Generate(modData, generationArgs);

					var mapPath = Path.Combine(outputDir, name + ".oramap");
					using (var package = new ZipFileLoader.ReadWriteZipFile(mapPath, true))
						map.Save(package);

					File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), map.SavePreview());

					written++;
					Console.WriteLine($"  {name}  seed {seed}");
				}
				catch (Exception e) when (e is MapGenerationException || e is YamlException)
				{
					failed++;
					Console.WriteLine($"  {name}  seed {seed}  FAILED: {e.Message}");
				}
			}

			Console.WriteLine($"Wrote {written} map(s), {failed} failed.");
		}

		// Fill in a value for every option the generator declares, not only the ones asked
		// for. Most of the generator's parameters arrive through hidden option groups, and
		// an option carrying no value contributes nothing at all, so leaving one out makes
		// the generator fail on a missing parameter rather than fall back to anything.
		static Dictionary<string, string> ResolveOptions(
			IEditorMapGeneratorInfo generator,
			ITerrainInfo terrainInfo,
			string tileset,
			Size size,
			Dictionary<string, string> settings)
		{
			var options = new Dictionary<string, string>(settings);

			// GetPlayerCount reads the Players option out of this, so it stays a live view.
			var probe = new MapGenerationArgs
			{
				Generator = generator.Type,
				Tileset = tileset,
				Size = size,
				Title = "probe",
				Author = "probe",
				Options = options,
			};

			foreach (var option in generator.Options)
			{
				var playerCount = generator.GetPlayerCount(probe);
				switch (option)
				{
					case MapGeneratorMultiChoiceOption multiChoice:
					{
						var valid = multiChoice.ValidChoices(terrainInfo, playerCount);
						if (options.TryGetValue(option.Id, out var chosen))
						{
							// The engine would quietly substitute its own default here. Say so
							// instead: an ignored choice is worse than a failed run.
							if (!valid.Contains(chosen))
								throw new ArgumentException(
									$"`{chosen}` is not a valid {option.Id} for {playerCount} player(s) on {tileset}. " +
									$"Valid: {string.Join(", ", valid)}");
						}
						else
							options[option.Id] = multiChoice.DefaultFor(terrainInfo, playerCount);

						break;
					}

					case MapGeneratorMultiIntegerChoiceOption multiInteger:
					{
						if (options.TryGetValue(option.Id, out var chosen))
						{
							if (!Exts.TryParseInt32Invariant(chosen, out var value) || !multiInteger.Choices.Contains(value))
								throw new ArgumentException(
									$"`{chosen}` is not a valid {option.Id}. Valid: {string.Join(", ", multiInteger.Choices)}");
						}
						else
							options[option.Id] = FieldSaver.FormatValue(multiInteger.Default.Value);

						break;
					}

					case MapGeneratorBooleanOption boolean:
					{
						if (!options.ContainsKey(option.Id))
							options[option.Id] = FieldSaver.FormatValue(boolean.Default);

						break;
					}

					case MapGeneratorIntegerOption integer:
					{
						if (!options.ContainsKey(option.Id))
							options[option.Id] = FieldSaver.FormatValue(integer.Default);

						break;
					}

					default:
						throw new ArgumentException($"Option `{option.Id}` has unhandled type {option.GetType().Name}");
				}
			}

			return options;
		}

		static string Take(Dictionary<string, string> settings, string key)
		{
			return settings.Remove(key, out var value) ? value : null;
		}

		static Size ParseSize(string value)
		{
			var parts = value.Split('x');
			if (parts.Length != 2
				|| !Exts.TryParseInt32Invariant(parts[0], out var width)
				|| !Exts.TryParseInt32Invariant(parts[1], out var height)
				|| width <= 0 || height <= 0)
				throw new ArgumentException($"`{value}` is not a map size. Use WIDTHxHEIGHT, for example 142x142.");

			return new Size(width, height);
		}

		static string Slug(string value)
		{
			var builder = new StringBuilder(value.Length);
			foreach (var c in value.ToLowerInvariant())
				builder.Append(char.IsAsciiLetterOrDigit(c) ? c : '-');

			return builder.ToString().Trim('-');
		}
	}
}
