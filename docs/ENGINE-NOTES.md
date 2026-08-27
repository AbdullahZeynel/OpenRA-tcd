# Engine notes

The verified-facts ledger. Every claim this project makes about how the OpenRA
engine behaves lives here, with a source you can open and check.

**Read this before deriving an engine fact yourself.** Add an entry whenever you
verify something new. This is how a wrong assumption gets caught by the next
contributor instead of by a bug six weeks later.

All entries below were verified against the commit in `ENGINE_BASE`:

```
f7dbaa1b6c3f27bda002f878cb121e507a10c6b5   (upstream bleed, 2026-04-24)
```

Line numbers are valid for that commit only. When `ENGINE_BASE` moves, every
entry is re-checked as part of the `upstream-sync` PR. An entry that no longer
holds is **corrected, not deleted** — note what changed and when.

### Entry format

```markdown
## Short claim as a heading

- **Claim:** what is true, precisely.
- **Source:** path/to/File.cs:LINE
- **Verified against:** <commit>
- **Verified by:** @who — YYYY-MM-DD
- **Used by:** which feature depends on this
```

---

## Selection.Combine is virtual and safe to override

- **Claim:** `Selection.Combine(World, IEnumerable<Actor>, bool, bool)` is
  declared `public virtual`, so a subclass can intercept every selection change.
  `SelectionInfo : TraitInfo` (line 18) is an ordinary trait info, and
  `Selection` carries `[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]`
  (line 23), so it can be swapped out in mod rules like any other world trait.
  `Add` (line 49) and `Remove` (line 62) are also virtual.
- **Source:** `OpenRA.Mods.Common/Traits/World/Selection.cs:91`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F1 (persistent squads)

## The Selection trait is attached to the world actor in RA's rules

- **Claim:** `^BaseWorld` lists `Selection:` and `ControlGroups:` as plain world
  traits, so replacing `Selection` with our subclass is a two-line YAML change.
- **Source:** `mods/ra/rules/world.yaml:5` (`Selection:`) and `:6` (`ControlGroups:`)
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F1 (persistent squads)

## Clicking takes only the first actor, box-select takes all

- **Claim:** inside `Combine`, when `isClick` is true the new selection is
  reduced with `.Take(1)`; when false the whole collection is used. Passing an
  already-resolved list with `isClick: false` is therefore how you select a
  group programmatically.
- **Source:** `OpenRA.Mods.Common/Traits/World/Selection.cs:91` (method body)
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F1 (persistent squads)

## ControlGroups is the reference pattern for client-side group state

- **Claim:** `ControlGroups` is a world trait holding `List<Actor>[]`, implements
  `IControlGroups, ITick, IGameSaveTraitData`, and calls
  `world.Selection.Combine(...)` to apply a group. It is client-side and does not
  participate in the simulation. Our `SquadManager` follows the same shape,
  including save/restore via `IGameSaveTraitData`.
- **Source:** `OpenRA.Mods.Common/Traits/World/ControlGroups.cs`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F1 (persistent squads), L8 (determinism boundary)

## Hotkeys bind to C# via SingleHotkeyBaseLogic

- **Claim:** a hotkey handler subclasses `SingleHotkeyBaseLogic`, is annotated
  `[ChromeLogicArgsHotkeys("SomeKey")]`, and is registered by name on a `Logic:`
  line in the mod's chrome YAML. Key definitions themselves live in
  `mods/*/hotkeys/*.yaml` and `mods/ra/hotkeys.yaml`.
- **Source:** `OpenRA.Mods.Common/Widgets/Logic/SingleHotkeyBaseLogic.cs`;
  example handler
  `OpenRA.Mods.Common/Widgets/Logic/Ingame/Hotkeys/RemoveFromControlGroupHotkeyLogic.cs`;
  registration at `mods/ra/chrome/ingame-player.yaml:6`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F1, F2, F3 (all hotkeys)

## The in-game command bar is a chrome container we can extend

- **Claim:** `Container@COMMAND_BAR` holds the attack-move / force-move / guard /
  stop buttons, is driven by `CommandBarLogic`, and its buttons are 34×26 with
  24×24 icons drawn from the `command-icons` sprite collection.
- **Source:** `mods/ra/chrome/ingame-player.yaml:51`; icon regions at
  `mods/ra/chrome.yaml:221`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F1, F2, F3 (UI buttons)
- **Note:** new icons need new 24×24 regions in the sprite sheet, or a new
  collection pointing at our own PNG. Deferred to sprint 07.

## Mods load extra assemblies through mod.yaml

- **Claim:** `Assemblies:` is a single comma-separated line listing the DLLs a
  mod loads. RA currently loads `OpenRA.Mods.Common.dll, OpenRA.Mods.Cnc.dll`;
  we append `OpenRA.Mods.Tcd.dll`.
- **Source:** `mods/ra/mod.yaml:112`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** sprint 01 (project wiring)

## Production is queued with Order.StartProduction

- **Claim:** `Order.StartProduction(Actor subject, string item, int count, bool queued = true)`
  builds a `StartProduction` order carrying the count in `ExtraData` and the
  actor type in `TargetString`. `ProductionQueue.CanBuild(ActorInfo)` (line 330)
  and `CanQueue(ActorInfo, out string, out string)` (line 404) gate it, so cash
  and prerequisite handling come for free.
- **Source:** `OpenRA.Game/Network/Order.cs:295`;
  `OpenRA.Mods.Common/Traits/Player/ProductionQueue.cs:330`, `:404`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F3 (squad reproduction)

## Production completion is observable via INotifyProduction

- **Claim:** `INotifyProduction.UnitProduced(Actor self, Actor other, CPos exit)`
  fires on the producing actor. `INotifyOtherProduction.UnitProducedByOther(...)`
  (line 152) fires more broadly and carries the production type and init data.
  `INotifyUnitProduced` **does not exist** — a plausible name that is not real.
- **Source:** `OpenRA.Mods.Common/TraitsInterfaces.cs:151`, `:152`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F3 (squad reproduction)

## Rally points already exist and are order-driven

- **Claim:** `RallyPoint` is a building trait with its own order, and produced
  units are sent to it deterministically by the producer. Reusing it is the
  cheap, sync-safe way to gather newly produced units (F3 v1).
- **Source:** `OpenRA.Mods.Common/Traits/Buildings/RallyPoint.cs`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** F3 (squad reproduction)

## The engine targets .NET 10

- **Claim:** `TargetFramework` is `net10.0` for non-Mono builds, so a .NET 10 SDK
  is required. The last tagged release (`release-20250330`) targets `net6.0`,
  which is why this fork is based on a pinned `bleed` commit instead of that tag.
- **Source:** `Directory.Build.props`; `INSTALL.md`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** sprint 01 (toolchain)

## Native libraries are downloaded, not system-installed

- **Claim:** on x86_64 the default build fetches precompiled SDL2, FreeType,
  OpenAL and Lua 5.1 via NuGet. System packages are only needed for
  `make DEPENDENCIES=system` or non-x86_64 architectures.
- **Source:** `INSTALL.md`
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-26
- **Used by:** sprint 01 (toolchain), CONTRIBUTING quick start

## Hotkey definitions are "KEY Modifier, Modifier"

- **Claim:** `Hotkey.TryParse` splits the value on a space into at most two
  fields: the `Keycode`, then the `Modifiers`. Multiple modifiers therefore go in
  the second field comma-separated (`NUMBER_1 Ctrl, Shift`). Space-separating
  them (`Q Ctrl Shift`) fails to parse and aborts mod loading.
- **Source:** OpenRA.Game/Input/Hotkey.cs:27; `Modifiers` enum at
  OpenRA.Game/Input/IInputHandler.cs:38; example at
  mods/common/hotkeys/control-groups.yaml:121
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel - 2026-08-26
- **Used by:** F1 (squad hotkeys)

## Chrome sheets must have power-of-two dimensions

- **Claim:** any PNG loaded as a chrome sheet becomes an OpenGL texture, and
  texture creation throws `InvalidDataException: Non-power-of-two array WxH`
  unless both dimensions are powers of two. The crash surfaces at render time
  inside `WidgetUtils.DrawPanel`, not at mod load, so a bad sheet looks like a
  graphics bug rather than an asset bug. Pad the sheet and anchor the content at
  the origin; the unused area costs nothing. Engine sheets follow this:
  glyphs.png is 256x256, glyphs-2x.png 512x512, glyphs-3x.png 1024x1024.
- **Source:** OpenRA.Platforms.Default/Texture.cs:84;
  OpenRA.Game/Graphics/Sheet.cs:53
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel - 2026-08-26
- **Used by:** F1 (squad command bar icons)

## The right mouse button only reaches the order generator on release

- **Claim:** `WorldInteractionControllerWidget.HandleMouseInput` calls `ApplyOrders`
  for the right button only when `mi.Event == MouseInputEvent.Up`. Right-button
  `Down` and `Move` events are never passed to `IOrderGenerator.Order` at all, so
  an order generator cannot see a right-drag as it happens. `GetCursor` is the
  hook that does run every frame with the cell under the cursor, so continuous
  tracking has to be done there.
- **Note:** this only applies while the active generator *is* a
  `UnitOrderGenerator`. A generator that is not one takes an earlier branch that
  forwards every event, which is why a standalone `OrderGenerator` behaves
  differently from a `UnitOrderGenerator` subclass.
- **Source:** OpenRA.Mods.Common/Widgets/WorldInteractionControllerWidget.cs:93
  (the `is not UnitOrderGenerator` branch) and :170 (the right-button Up branch)
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel - 2026-08-26
- **Used by:** F2 (drawn formations)


## Production queues are found by category, not by name

- **Claim:** `AIUtils.FindQueuesByCategory(Player)` returns an
  `ILookup<string, ProductionQueue>` keyed by queue type. It is the same helper
  the bot modules use, so it is the supported route from a unit type to the
  queues that can build it.
- **Source:** OpenRA.Mods.Common/AIUtils.cs:38; called from
  OpenRA.Mods.Common/Traits/BotModules/BaseBuilderBotModule.cs:345
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel - 2026-08-26
- **Used by:** F3 (squad rebuild)

## Queueing production goes through Order.StartProduction

- **Claim:** `Order.StartProduction(Actor subject, string item, int count, bool queued = true)`
  is the order the sidebar itself issues. `subject` is the queue's actor, not the
  factory. Anything queued this way follows the normal production path.
- **Source:** OpenRA.Game/Network/Order.cs:295
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel - 2026-08-26
- **Used by:** F3 (squad rebuild)

## INotifyOtherProduction lives in OpenRA.Mods.Common

- **Claim:** `INotifyOtherProduction` is declared in `OpenRA.Mods.Common.Traits`,
  not `OpenRA.Traits`. Its single member is
  `void UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)`.
- **Source:** OpenRA.Mods.Common/TraitsInterfaces.cs:152
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel - 2026-08-26
- **Used by:** F3 (squad rebuild)

## A production queue accepts items the player cannot afford

- **Claim:** `ProductionQueueInfo.PayUpFront` defaults to `false`, and nothing
  under `mods/ra` overrides it. With it off, the queue accepts an item whatever
  the player's cash is and drains money as it arrives. Every cash check in the
  queue is guarded by `PayUpFront`. Money slows a rebuild down; it never rejects
  one.
- **Source:** OpenRA.Mods.Common/Traits/Player/ProductionQueue.cs:43 (the
  default), :415 and :493 (the guarded cash checks)
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel - 2026-08-26
- **Used by:** F3 (squad rebuild)

## ^BaseWorld reaches the map editor, and TraitLocation is enforced

- **Claim:** In RA, `World:` and `EditorWorld:` both inherit `^BaseWorld`. A
  trait declared `[TraitLocation(SystemActors.World)]` and added to `^BaseWorld`
  therefore lands on the editor world too and fails the `CheckTraitLocation`
  lint with "`X` does not belong on `editorworld`. It is a system trait meant
  for World." Such a trait goes under `World:` on its own.
- **Note:** the check runs in `make test`, not in `make check`. A green
  `make check` locally says nothing about it; CI catches it.
- **Source:** OpenRA.Mods.Common/Lint/CheckTraitLocation.cs:36;
  mods/ra/rules/world.yaml:161 (`World: Inherits: ^BaseWorld`) and :302
  (`EditorWorld: Inherits: ^BaseWorld`)
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel - 2026-08-26
- **Used by:** F3 (SquadRecruiter)

## A generated map travels as a recipe, not as a file

- **Claim:** A map produced by a map generator is shared with the rest of the
  lobby as its `MapGenerationArgs` — uid, generator, tileset, size, options,
  title and author — and every client builds an identical copy locally. No
  `.oramap` crosses the network, and there is no download step. The lobby
  accepts a map whose status is `Generatable` or `Generating` exactly as it
  accepts one that is already on disk.
- **Also:** the source says so itself. `MapGenerationArgs` carries the comment
  "Title and author are baked into the map.yaml and must agree across all
  clients, regardless of the local client's language."
- **Source:** OpenRA.Game/Map/MapGenerationArgs.cs:19;
  OpenRA.Mods.Common/ServerTraits/LobbyCommands.cs:668;
  OpenRA.Game/Map/MapPreview.cs:474 (`UpdateFromGenerationArgs`)
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-27, in play: two clients on one
  machine, a map generated in the lobby, both sides in the same game with no
  transfer
- **Used by:** sprint 07 (maps)

## The Resource Center can be queried by hash, and not searched

- **Claim:** OpenRA fetches a missing map from `MapRepository`, which defaults
  to `https://resource.openra.net/map/`, by requesting
  `hash/<uid>[,<uid>…]/yaml` in batches of 50. The published Map API documents
  download by hash, lookup by hash, lookup by id, and the newest map. It
  documents no endpoint that searches, filters or pages through the catalogue,
  so an in-game browser cannot be built against it without an index of our own.
- **Also:** a server can advertise a `MapPool` of uids. Clients in that lobby
  see the pool in the map chooser and download from it. That is the supported
  route to putting a curated set of maps in front of players.
- **Source:** OpenRA.Mods.Common/WebServices.cs:23;
  OpenRA.Game/Map/MapCache.cs:244; OpenRA.Mods.Common/Widgets/Logic/MapChooserLogic.cs:284;
  https://github.com/OpenRA/OpenRA-Resources/wiki/MapAPI
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-27
- **Used by:** sprint 07 (maps)

## The map generator already reaches sixteen players

- **Claim:** RA's `ClassicMapGenerator` exposes a `Players` option offering
  2, 4, 6, 8, 10, 12, 14 and 16, alongside roughly seventy other parameters
  covering water and mountain coverage, forest clumpiness, symmetry
  enforcement, roads, spawn-region reservation, resources per player and
  expansion sizing. Large-player-count maps need no engine work, only presets.
- **Source:** mods/ra/rules/map-generators.yaml:232 (the `Players` option) and
  :248; OpenRA.Mods.Common/Traits/World/ClassicMapGenerator.cs
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel — 2026-08-27
- **Used by:** sprint 07 (maps)

## A generator option with no value contributes no parameters

- **Claim:** `MapGeneratorBase.GenerateParameterYaml` walks the generator's
  options in ascending `Priority` and skips any option missing from
  `MapGenerationArgs.Options`. Most of a generator's parameters arrive through
  option groups the UI never shows: RA's `hidden_defaults` alone carries
  `Mirror`, `Rotations` and around sixty others, and `hidden_tileset_overrides`
  carries the per-tileset land and water tiles. Generating with only the options
  a caller cares about therefore fails inside the parameter loader on the first
  missing key. It does not fall back.
- **Also:** a `MultiChoiceOption` value that is invalid for the current tileset
  and player count is quietly replaced by that option's default, so a wrong
  choice produces a map rather than an error. Callers that care have to check
  `ValidChoices` themselves.
- **Source:** OpenRA.Mods.Common/Traits/World/MapGeneratorBase.cs:99;
  OpenRA.Mods.Common/MapGenerator/MapGeneratorOptions.cs:141;
  mods/ra/rules/map-generators.yaml:7 and :78
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel - 2026-08-27, after --generate-maps crashed
  with "No node with key 'Mirror'"
- **Used by:** sprint 07 (maps)

## AutoTargetPriority needs AutoTarget, and rules remove traits one name at a time

- **Claim:** `AutoTargetPriority` requires `AutoTarget`. An actor carrying a
  priority whose `AutoTarget` has been removed does not fail quietly - it fails
  to construct, and every map using that actor fails with it.
- **Why that happens by accident:** RA's own rules strip `AutoTarget` from a few
  actors and then remove the priority traits that came with it **by name**.
  `campaign-rules.yaml:63` defines `e7.noautotarget`, which inherits `E7`, writes
  `-AutoTarget:` and then removes the two upstream `AutoTargetPriority@...`
  instances it knows about. A removal names one instance; it cannot name an
  instance added later. So a priority added to a base actor in mod rules is
  inherited by the derived actor, arrives without `AutoTarget`, and breaks it.
- **How to check before adding one:** grep `-AutoTarget:` across `mods/ra`. At
  `ENGINE_BASE` the actors that strip it are `E7` (campaign rules), `GNRL`,
  `VOLK` and `PBOX`. Giving any of those a priority means also adding a matching
  `-AutoTargetPriority@...` wherever the parent is stripped. Leaving them out of
  the priorities entirely is the smaller change, and the one we made.
- **Also:** priority strictly beats range in `ChooseTarget`, and range only
  separates targets of equal priority. The scan radius stays the weapon's maximum
  range and a unit on Defend stance may not move, so raising a priority never
  sends a unit walking towards what it prefers.
- **Source:** OpenRA.Mods.Common/Traits/AutoTargetPriority.cs:18;
  OpenRA.Mods.Common/Traits/AutoTarget.cs:458, :312, :146;
  mods/ra/rules/campaign-rules.yaml:63
- **Verified against:** f7dbaa1b
- **Verified by:** @AbdullahZeynel - 2026-08-27, after `make test` reported
  ``Actor `e7.noautotarget` is not constructible ... Errors: 61``
- **Used by:** sprint 10 (target priority)
