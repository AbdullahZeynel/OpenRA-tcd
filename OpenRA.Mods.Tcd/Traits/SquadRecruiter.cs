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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Tcd.Production;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Tcd.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Groups units the player ordered as a squad rebuild once they finish building.",
		"Several rebuilds can be in flight at once; each one becomes its own squad.",
		"Purely client-side: it never issues an order, so it cannot desync. Where the",
		"units walk to is left to the producing building's rally point.")]
	public sealed class SquadRecruiterInfo : TraitInfo
	{
		[Desc("Ticks to wait for the last unit of a batch before grouping whoever turned",
			"up. At the default game speed 1500 is one minute.")]
		public readonly int Patience = 1500;

		[Desc("Most rebuilds that can be waiting at once. The oldest is dropped past this.")]
		public readonly int MaxPendingBatches = 32;

		public override object Create(ActorInitializer init) { return new SquadRecruiter(init.World, this); }
	}

	public sealed class SquadRecruiter : INotifyOtherProduction, ITick
	{
		sealed class Batch
		{
			public readonly Dictionary<string, int> Wanted = [];
			public readonly List<Actor> Recruited = [];
			public int Waited;
		}

		readonly World world;
		readonly SquadRecruiterInfo info;

		// Ordered oldest first, so a produced unit joins the rebuild that asked for it
		// earliest. Without this, ordering five copies of a squad grouped only one.
		readonly List<Batch> batches = [];

		public SquadRecruiter(World world, SquadRecruiterInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public int PendingBatches => batches.Count;

		public void Expect(IReadOnlyDictionary<string, int> composition)
		{
			var batch = new Batch();
			foreach (var (type, count) in composition)
				batch.Wanted[type] = count;

			batches.Add(batch);

			while (batches.Count > info.MaxPendingBatches)
				batches.RemoveAt(0);
		}

		void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producer, Actor produced,
			string productionType, TypeDictionary init)
		{
			if (produced.Owner != world.LocalPlayer || batches.Count == 0)
				return;

			var batch = batches.FirstOrDefault(b => b.Wanted.TryGetValue(produced.Info.Name, out var n) && n > 0);
			if (batch == null)
				return;

			var remaining = batch.Wanted[produced.Info.Name] - 1;
			if (remaining == 0)
				batch.Wanted.Remove(produced.Info.Name);
			else
				batch.Wanted[produced.Info.Name] = remaining;

			batch.Recruited.Add(produced);

			if (batch.Wanted.Count == 0)
			{
				Assemble(batch);
				batches.Remove(batch);
			}
		}

		void ITick.Tick(Actor self)
		{
			// Reverse so removing a finished batch does not skip the next one.
			for (var i = batches.Count - 1; i >= 0; i--)
			{
				if (++batches[i].Waited < info.Patience)
					continue;

				// Out of patience: group whoever made it rather than waiting forever for
				// a unit the player can no longer afford or build.
				Assemble(batches[i]);
				batches.RemoveAt(i);
			}
		}

		void Assemble(Batch batch)
		{
			var alive = batch.Recruited.Where(a => a.IsInWorld && !a.IsDead).ToList();
			if (alive.Count == 0)
				return;

			var squads = world.WorldActor.TraitOrDefault<SquadManager>();
			var squad = squads?.Form(alive);
			if (squad == null)
				return;

			var missing = SquadComposition.Total(batch.Wanted);
			TextNotificationsManager.Debug(missing > 0
				? $"Squad {squad.Id} grouped with {squad.Members.Count} units, {missing} never arrived."
				: $"Squad {squad.Id} rebuilt: {squad.Members.Count} units.");
		}
	}
}
