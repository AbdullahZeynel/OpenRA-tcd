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
using NUnit.Framework;
using OpenRA.Mods.Tcd.Production;

namespace OpenRA.Test
{
	[TestFixture]
	sealed class SquadCompositionTest
	{
		[TestCase(TestName = "Counts each actor type in the squad")]
		public void CountsEachType()
		{
			var composition = SquadComposition.Of(["1TNK", "E3", "1TNK", "E1", "E3", "E3"]);

			Assert.That(composition["1TNK"], Is.EqualTo(2));
			Assert.That(composition["E3"], Is.EqualTo(3));
			Assert.That(composition["E1"], Is.EqualTo(1));
			Assert.That(composition.Count, Is.EqualTo(3), "an actor type was invented or lost");
		}

		[TestCase(TestName = "Total matches the number of units that went in")]
		public void TotalMatchesInput()
		{
			string[] units = ["1TNK", "E3", "1TNK", "E1", "E3", "E3"];
			Assert.That(SquadComposition.Total(SquadComposition.Of(units)), Is.EqualTo(units.Length));
		}

		[TestCase(TestName = "An empty squad has no composition")]
		public void EmptySquad()
		{
			var composition = SquadComposition.Of([]);

			Assert.That(composition, Is.Empty);
			Assert.That(SquadComposition.Total(composition), Is.EqualTo(0));
			Assert.That(SquadComposition.Describe(composition), Is.EqualTo("nothing"));
		}

		[TestCase(TestName = "Description leads with the biggest group and is stable")]
		public void DescriptionIsOrderedAndStable()
		{
			var first = SquadComposition.Of(["E1", "1TNK", "E3", "E3", "1TNK", "E3"]);
			var second = SquadComposition.Of(["E3", "E3", "1TNK", "E3", "E1", "1TNK"]);

			Assert.That(SquadComposition.Describe(first), Is.EqualTo("3x E3, 2x 1TNK, 1x E1"));
			Assert.That(SquadComposition.Describe(second), Is.EqualTo(SquadComposition.Describe(first)),
				"the same squad should describe itself the same way whatever order it was built in");
		}

		[TestCase(TestName = "Blank actor names are ignored")]
		public void BlanksAreIgnored()
		{
			var composition = SquadComposition.Of(["E1", "", null, "E1"]);

			Assert.That(composition.Count, Is.EqualTo(1));
			Assert.That(composition["E1"], Is.EqualTo(2));
		}

		[TestCase(TestName = "Null input is rejected")]
		public void NullIsRejected()
		{
			Assert.Throws<ArgumentNullException>(() => SquadComposition.Of(null));
			Assert.Throws<ArgumentNullException>(() => SquadComposition.Total(null));
			Assert.Throws<ArgumentNullException>(() => SquadComposition.Describe(null));
		}
	}
}
