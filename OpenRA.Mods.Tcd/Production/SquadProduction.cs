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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Tcd.Production
{
	public readonly record struct QueueResult(int Queued, IReadOnlyList<string> Rejected,
		IReadOnlyDictionary<string, int> Accepted);

	// Puts a squad's composition into the right production queues.
	//
	// Cash is deliberately not checked. RA leaves PayUpFront off, so a queue accepts
	// everything and builds it as money arrives - which is the behaviour we want:
	// order the whole squad now, it fills in over time.
	public static class SquadProduction
	{
		public static QueueResult Queue(World world, IReadOnlyDictionary<string, int> composition)
		{
			var rejected = new List<string>();
			var accepted = new Dictionary<string, int>();
			if (world.LocalPlayer == null || composition == null || composition.Count == 0)
				return new QueueResult(0, rejected, accepted);

			var queues = AIUtils.FindQueuesByCategory(world.LocalPlayer);
			var queued = 0;

			foreach (var (type, count) in composition)
			{
				if (!world.Map.Rules.Actors.TryGetValue(type, out var actorInfo))
				{
					rejected.Add(type);
					continue;
				}

				var buildable = actorInfo.TraitInfoOrDefault<BuildableInfo>();
				if (buildable == null)
				{
					rejected.Add(type);
					continue;
				}

				var queue = buildable.Queue
					.SelectMany(category => queues[category])
					.FirstOrDefault(q => q.CanBuild(actorInfo));

				if (queue == null)
				{
					// No factory for it, or prerequisites are gone.
					rejected.Add(type);
					continue;
				}

				world.IssueOrder(Order.StartProduction(queue.Actor, type, count, true));
				accepted[type] = count;
				queued += count;
			}

			return new QueueResult(queued, rejected, accepted);
		}
	}
}
