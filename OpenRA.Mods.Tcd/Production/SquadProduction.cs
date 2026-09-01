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
	public readonly record struct QueueResult(int Queued, IReadOnlyDictionary<string, int> Rejected,
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
			var rejected = new Dictionary<string, int>();
			var accepted = new Dictionary<string, int>();
			if (world.LocalPlayer == null || composition == null || composition.Count == 0)
				return new QueueResult(0, rejected, accepted);

			var queues = AIUtils.FindQueuesByCategory(world.LocalPlayer);
			var allTech = world.LocalPlayer.PlayerActor.Trait<DeveloperMode>().AllTech;
			var plannedByQueue = new Dictionary<ProductionQueue, int>();
			var queued = 0;

			foreach (var (type, count) in composition)
			{
				if (!world.Map.Rules.Actors.TryGetValue(type, out var actorInfo))
				{
					rejected[type] = count;
					continue;
				}

				var buildable = actorInfo.TraitInfoOrDefault<BuildableInfo>();
				if (buildable == null)
				{
					rejected[type] = count;
					continue;
				}

				ProductionQueue queue = null;
				var admitted = 0;
				var owned = world.ActorsHavingTrait<Buildable>()
					.Count(a => a.Info.Name == type && a.Owner == world.LocalPlayer);

				foreach (var candidate in buildable.Queue
					.SelectMany(category => queues[category])
					.Where(q => q.CanBuild(actorInfo))
					.Distinct())
				{
					var current = candidate.AllQueued().ToList();
					var planned = plannedByQueue.GetValueOrDefault(candidate);
					var candidateAdmitted = AdmittedCount(count, current.Count + planned,
						current.Count(i => i.Item == type), owned, candidate.Info.QueueLimit,
						candidate.Info.ItemLimit, buildable.BuildLimit, allTech);

					if (candidateAdmitted <= admitted)
						continue;

					queue = candidate;
					admitted = candidateAdmitted;
				}

				if (queue == null || admitted == 0)
				{
					// No factory, missing prerequisites, or no capacity under the active limits.
					rejected[type] = count;
					continue;
				}

				world.IssueOrder(Order.StartProduction(queue.Actor, type, admitted, true));
				accepted[type] = admitted;
				queued += admitted;
				plannedByQueue[queue] = plannedByQueue.GetValueOrDefault(queue) + admitted;

				if (admitted < count)
					rejected[type] = count - admitted;
			}

			return new QueueResult(queued, rejected, accepted);
		}

		public static int AdmittedCount(int requested, int queued, int queuedOfType, int ownedOfType,
			int queueLimit, int itemLimit, int buildLimit, bool allTech)
		{
			if (requested <= 0)
				return 0;

			if (allTech)
				return requested;

			var admitted = requested;
			if (queueLimit > 0)
				admitted = int.Min(admitted, queueLimit - queued);

			if (itemLimit > 0)
				admitted = int.Min(admitted, itemLimit - queuedOfType);

			if (buildLimit > 0)
				admitted = int.Min(admitted, buildLimit - queuedOfType - ownedOfType);

			return int.Max(0, admitted);
		}
	}
}
