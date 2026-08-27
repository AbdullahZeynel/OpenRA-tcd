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
using System.Linq;

namespace OpenRA.Mods.Tcd.Production
{
	// What a squad is made of, as actor type to count. Kept separate from the squad
	// itself so it outlives the units: losing the whole squad is exactly when you
	// want to rebuild it.
	//
	// Pure: no World, no Actor, so it is unit testable.
	public static class SquadComposition
	{
		public static Dictionary<string, int> Of(IEnumerable<string> actorTypes)
		{
			ArgumentNullException.ThrowIfNull(actorTypes);

			var composition = new Dictionary<string, int>();
			foreach (var type in actorTypes)
			{
				if (string.IsNullOrEmpty(type))
					continue;

				composition.TryGetValue(type, out var count);
				composition[type] = count + 1;
			}

			return composition;
		}

		public static int Total(IReadOnlyDictionary<string, int> composition)
		{
			ArgumentNullException.ThrowIfNull(composition);
			return composition.Values.Sum();
		}

		// "2x 1TNK, 3x E3" - ordered by count so the backbone of the squad reads first,
		// then by name so the same squad always describes itself the same way.
		public static string Describe(IReadOnlyDictionary<string, int> composition)
		{
			ArgumentNullException.ThrowIfNull(composition);

			return composition.Count == 0
				? "nothing"
				: string.Join(", ", composition
					.OrderByDescending(kv => kv.Value)
					.ThenBy(kv => kv.Key, StringComparer.Ordinal)
					.Select(kv => $"{kv.Value}x {kv.Key}"));
		}
	}
}
