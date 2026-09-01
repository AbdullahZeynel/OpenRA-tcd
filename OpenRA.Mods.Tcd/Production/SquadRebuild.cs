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

using OpenRA.Mods.Tcd.Traits;

namespace OpenRA.Mods.Tcd.Production
{
	// Shared by the command bar button and the hotkey so the two can never drift.
	public static class SquadRebuild
	{
		public static void Execute(World world)
		{
			var squads = world.WorldActor.TraitOrDefault<SquadManager>();
			var recruiter = world.WorldActor.TraitOrDefault<SquadRecruiter>();
			if (squads == null || recruiter == null)
				return;

			var composition = squads.CompositionToRebuild(world.Selection.Actors);
			if (composition == null || composition.Count == 0)
			{
				TextNotificationsManager.Debug("No squad to rebuild. Form one first.");
				return;
			}

			var result = SquadProduction.Queue(world, composition);
			if (result.Queued == 0)
			{
				TextNotificationsManager.Debug(
					$"Cannot rebuild {SquadComposition.Describe(composition)}. Not queued: " +
					$"{SquadComposition.Describe(result.Rejected)}.");
				return;
			}

			// Only wait for what actually made it into a queue, or the recruiter would
			// sit there expecting a unit that was never ordered.
			recruiter.Expect(result.Accepted);

			TextNotificationsManager.Debug(result.Rejected.Count > 0
				? $"Rebuilding {SquadComposition.Describe(result.Accepted)}. " +
					$"Not queued: {SquadComposition.Describe(result.Rejected)}."
				: $"Rebuilding squad: {SquadComposition.Describe(result.Accepted)}.");
		}
	}
}
