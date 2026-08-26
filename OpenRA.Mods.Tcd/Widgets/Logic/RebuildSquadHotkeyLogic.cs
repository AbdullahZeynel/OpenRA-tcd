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
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Mods.Tcd.Production;
using OpenRA.Widgets;

namespace OpenRA.Mods.Tcd.Widgets.Logic
{
	[ChromeLogicArgsHotkeys("RebuildSquadKey")]
	public sealed class RebuildSquadHotkeyLogic : SingleHotkeyBaseLogic
	{
		readonly World world;

		[ObjectCreator.UseCtor]
		public RebuildSquadHotkeyLogic(Widget widget, ModData modData, World world,
			Dictionary<string, MiniYaml> logicArgs)
			: base(widget, modData, "RebuildSquadKey", "PLAYER_KEYHANDLER", logicArgs)
		{
			this.world = world;
		}

		protected override bool OnHotkeyActivated(KeyInput e)
		{
			SquadRebuild.Execute(world);
			return true;
		}
	}
}
