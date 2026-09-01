#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * version 3 or later. For more information, see COPYING.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.Tcd.Production;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class SquadProductionTest
	{
		[TestCase(5, 0, 0, 0, 0, 0, 0, false, 5, TestName = "Unlimited queues admit the full request")]
		[TestCase(5, 8, 2, 0, 10, 0, 0, false, 2, TestName = "Queue limits clip the request")]
		[TestCase(5, 4, 2, 0, 0, 3, 0, false, 1, TestName = "Item limits clip the request")]
		[TestCase(5, 4, 1, 2, 0, 0, 4, false, 1, TestName = "Build limits include owned and queued actors")]
		[TestCase(5, 10, 3, 1, 10, 3, 4, false, 0, TestName = "Exhausted limits reject the request")]
		[TestCase(5, 10, 3, 1, 10, 3, 4, true, 5, TestName = "AllTech bypasses production limits")]
		[TestCase(0, 0, 0, 0, 0, 0, 0, false, 0, TestName = "Empty requests admit nothing")]
		public void CalculatesAdmittedCount(int requested, int queued, int queuedOfType, int ownedOfType,
			int queueLimit, int itemLimit, int buildLimit, bool allTech, int expected)
		{
			Assert.That(SquadProduction.AdmittedCount(requested, queued, queuedOfType, ownedOfType,
				queueLimit, itemLimit, buildLimit, allTech), Is.EqualTo(expected));
		}
	}
}
