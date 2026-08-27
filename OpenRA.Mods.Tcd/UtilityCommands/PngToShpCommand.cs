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
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.SpriteLoaders;
using OpenRA.Primitives;

namespace OpenRA.Mods.Tcd.UtilityCommands
{
	// The other direction of --png. The engine can already turn a sprite into images;
	// this turns images back into a sprite, which is what adding new art needs.
	//
	// Frames arrive as 8 bit indexed PNGs and the index is used as it stands: no colour
	// matching happens here. Whatever drew the frames decided which palette entry each
	// pixel is, and that decision is the one that reaches the game.
	sealed class PngToShpCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--png-to-shp";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 3;
		}

		[Desc(
			"OUTPUT.shp FRAME.png [FRAME.png ...]",
			"Pack indexed PNG frames into a ShpTD sprite, in the order given.",
			"Every frame has to be 8 bit indexed and the same size as the first;",
			"index 0 is transparent, and the indices are those of the mod's palette.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var output = args[1];
			var inputs = args.Skip(2).ToArray();

			Size? size = null;
			var frames = new List<byte[]>();

			foreach (var path in inputs)
			{
				Png png;
				using (var stream = File.OpenRead(path))
					png = new Png(stream);

				if (png.Type != SpriteFrameType.Indexed8)
					throw new InvalidDataException(
						$"{path} is {png.Type}. Frames have to be 8 bit indexed PNGs, so that a pixel is a palette entry.");

				var frameSize = new Size(png.Width, png.Height);
				size ??= frameSize;

				if (frameSize != size.Value)
					throw new InvalidDataException(
						$"{path} is {frameSize.Width}x{frameSize.Height} but the first frame is " +
						$"{size.Value.Width}x{size.Value.Height}. Every frame of a sprite is the same size.");

				frames.Add(png.Data);
			}

			using (var stream = File.Create(output))
				ShpTDSprite.Write(stream, size.Value, frames);

			Console.WriteLine($"{output}: {frames.Count} frame(s) at {size.Value.Width}x{size.Value.Height}");
		}
	}
}
