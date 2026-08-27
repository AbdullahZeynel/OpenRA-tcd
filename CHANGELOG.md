# Changelog

All notable changes to Tactics & Command Dynamics. One line per merged PR.
Format loosely follows [Keep a Changelog](https://keepachangelog.com/).

## [Unreleased]

Nothing yet.

## [0.2.0] — 2026-08-27

Eighty-five large maps, and the tooling that made them.

### Added
- The TCD map pack: 85 maps for eight to sixteen players, shipped inside the
  mod. Half vary the terrain, half vary the economy (#13).
- `--generate-maps`, a utility command that runs the map generator headlessly and
  writes each map beside a preview image, so a batch can be judged from pictures
  (#11).
- `tcd-map-recipes.sh` — the twenty recipes behind the pack (#11).
- Four engine notes on map generation: a generated map travels as a recipe and
  every client rebuilds it, the Resource Center answers by hash and cannot be
  searched, the generator already reaches sixteen players, and an option left
  unset contributes no parameters at all (#10, #11).
- A release checklist in `AGENTS.md`: releases are cut deliberately, not once per
  merge (#14).

### Changed
- `README.md` rewritten for this fork rather than describing OpenRA (#10).
- `AGENTS.md` section 7 corrected against a real diff of `ENGINE_BASE` against
  `tcd`, and section 10 given a rule for titles that land in people's inboxes
  (#10).
- `docs/PLAN.md` section 9 replaced with the real sprint record and sprints 06
  to 12 (#10).
- The scope block accepts a shell pattern, so a directory of generated assets is
  one line instead of hundreds (#12).
- `.github/workflows/ci.yml` runs on pull requests to `tcd`; it had only run on
  push (#10).

The AppImage grows from roughly 48 MB to 63 MB. Every player needs the same
build.

## [0.1.0] — 2026-08-27

First public build: a Red Alert AppImage on the releases page.

### Added
- Project constitution: the Ten Laws, `AGENTS.md`, contribution rules and the
  CI jobs that enforce them (sprint 00, #1).
- `ENGINE_BASE` pinning the upstream engine commit this fork is built on (#1).
- `docs/ENGINE-NOTES.md` — the verified engine-facts ledger (#1).
- The `OpenRA.Mods.Tcd` project, referenced from `OpenRA.slnx` and loaded
  through the `Assemblies:` line of `mods/ra/mod.yaml` (sprint 01, #2).
- Persistent squads: `SquadManager` and `TcdSelection`, so clicking one member
  selects the whole squad. Form with Ctrl+Q, disband with Ctrl+Shift+Q,
  Alt+click to escape back to a single unit. Dead actors are pruned on tick
  (sprint 02, #3).
- Command-bar buttons for form and disband, with a TCD icon sheet and tooltips
  through `mods/ra/fluent/tcd.ftl` (sprint 03, #3).
- Role-aware formations: a `FormationRole` trait tagged onto 19 RA actors,
  `FormationPlanner` placing units by role and rank, and Grid and Wedge presets
  (sprint 04, #5).
- Two drawing tools: V drags a straight line and distributes the squad along it
  by equal arc length, G marks a shape corner by corner. The two are mutually
  exclusive, and a marked shape is capped at 12 corners (sprint 04, #5).
- A collapsible tool tray on the command bar holding the formation buttons
  (sprint 04, #5).
- Unit tests for the pure geometry: `FormationShapesTest` and
  `FormationPathTest` (sprint 04, #5).
- Squad rebuild: `SquadComposition`, `SquadProduction` and `SquadRecruiter`
  re-queue a squad's exact composition from a button or Ctrl+R. New units leave
  at their factory's rally point and group into a fresh squad once the batch is
  complete; several batches can be in flight at once (sprint 05, #7).
- `SquadCompositionTest` covering the composition snapshot (sprint 05, #7).
- Five engine notes on production queues and trait locations (#9).

### Fixed
- `SquadRecruiter` moved from `^BaseWorld` to `World`. `^BaseWorld` is inherited
  by `EditorWorld` as well, and the trait declares `SystemActors.World`, so the
  editor world failed `CheckTraitLocation` (#7).

[Unreleased]: https://github.com/AbdullahZeynel/OpenRA-tcd/compare/tcd-0.1.0...tcd
[0.1.0]: https://github.com/AbdullahZeynel/OpenRA-tcd/releases/tag/tcd-0.1.0
