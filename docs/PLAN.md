# Tactics & Command Dynamics — Build Plan & Project Constitution

A command-layer mod for OpenRA Red Alert: persistent squads that select as one,
role-aware tactical formations, and one-button squad reconstitution. Open source,
open to contributors — human and machine — under rules a build server can
actually enforce.

| | |
|---|---|
| Base | fork of `OpenRA/OpenRA` |
| Mod id | `tcd` |
| Assembly | `OpenRA.Mods.Tcd.dll` |
| Target | skirmish + vs AI |
| License | GPL v3 (inherited from OpenRA) |

**Contents**

1. [Verdict](#1-verdict) · 2. [The repo](#2-the-repo-fork-dont-start-fresh) ·
3. [Architecture](#3-where-each-feature-plugs-in) · 4. [Feature specs](#4-feature-specs) ·
5. [The ten laws](#5-the-ten-laws) · 6. [Rules for AI](#6-rules-for-ai-contributors) ·
7. [Rules for humans](#7-rules-for-human-contributors) · 8. [Enforcement](#8-making-the-rules-real) ·
9. [Sprints](#9-eight-sprints) · 10. [Git workflow](#10-how-to-run-it-on-github) ·
11. [Risks](#11-what-could-go-wrong) · 12. [Next](#12-what-happens-next)

---

## 1. Verdict

All three features are buildable. None of them are YAML or Lua work — every one
touches selection, input, UI widgets, orders or the production queue, which all
live in engine C#. That is why we fork the engine rather than use the Mod SDK.

| What you asked for | Verdict | Lives in |
|---|---|---|
| Persistent squads — click any member, whole squad selects | Straightforward | `Selection.Combine()` is `virtual`. We subclass it. |
| Tactical formations with smart role placement | Moderate | New world trait + geometry solver + data-driven role tag in YAML. |
| One-button squad reproduction | Involved | Production orders are easy; gathering the new units is the fiddly part. |

**The lucky break:** in `OpenRA.Mods.Common/Traits/World/Selection.cs` the method
that decides what a click selects is `public virtual void Combine(...)`, and
`Selection` is attached to the world actor as an ordinary swappable trait in
`mods/ra/rules/world.yaml`. Feature 1 is therefore a subclass plus a two-line
YAML change — no engine surgery.

---

## 2. The repo: fork, don't start fresh

Fork `OpenRA/OpenRA`. Don't initialize an empty repo, and don't use the Mod SDK
for this project.

**Why fork**

- All three features need engine source access anyway.
- You want to play *Red Alert with better controls*, not ship a new game. A fork
  gives exactly that: `make && ./launch-game.sh`.
- Zero plumbing — the SDK would require re-mounting or duplicating the whole RA
  mod's data before you write any of your own code.
- Keeps the option of upstreaming a feature as a PR later.

**The one discipline that makes it work**

Keep *all* your C# in a new project, `OpenRA.Mods.Tcd`, and touch existing engine
files as little as possible. Your YAML goes in new override files, not edits
scattered through `mods/ra/rules/`. Do that and pulling a new upstream release is
a near-conflict-free merge. This is not a style preference — CI enforces it
(§8).

**License:** OpenRA is GPL v3. Your fork inherits it — your mod code must also be
GPL v3 and the repo stays public. It also means every contributor's work is GPL
v3: say so in `CONTRIBUTING.md` so nobody is surprised.

### Setup

Fork on GitHub, rename it `openra-tcd`, then:

```bash
# clone your fork, not upstream
git clone git@github.com:<your-username>/openra-tcd.git
cd openra-tcd

# keep a line back to upstream for future releases
git remote add upstream https://github.com/OpenRA/OpenRA.git
git fetch upstream --tags

# branch off upstream's development head, then pin the exact commit
git switch -c tcd upstream/bleed
git rev-parse HEAD > ENGINE_BASE
git push -u origin tcd
```

Set `tcd` as the default branch on GitHub. `bleed` stays untouched so you can
always diff against upstream. `ENGINE_BASE` holds the exact commit SHA this fork
is built on — every engine claim anyone makes is a claim about *that commit*.

**Why a pinned `bleed` commit and not a release tag.** The obvious move is to
base on the newest release tag, and that was the original plan. Checking it
changed the answer: `release-20250330` is the newest tag and it targets
**.NET 6**, which has been out of support since November 2024. Getting that SDK
on Arch means an EOL AUR package. Meanwhile `bleed` targets .NET 10, which the
official repos ship, and its head has been stable for months.

A pinned SHA gives exactly the same fixed floor a tag does — it does not move
unless you move it. The trade is that bleed carries changes that were never in a
release, so verify it builds *before* committing to it. Both hook points this
plan depends on were checked at both bases and are identical, so nothing in
sections 3–4 changes either way.

### First build

```bash
sudo pacman -S dotnet-sdk   # .NET 10 — required by INSTALL.md
make
./launch-game.sh Game.Mod=ra
```

On x86_64 the native libraries (SDL2, OpenAL, FreeType, Lua 5.1) are downloaded
automatically via NuGet — you only need system packages for
`make DEPENDENCIES=system`. Red Alert also needs its game assets; the in-game
content installer handles that on first launch.

---

## 3. Where each feature plugs in

OpenRA is trait-composition all the way down: an actor is a bag of traits, the
world itself is an actor with its own traits, and UI is YAML widgets bound to C#
"logic" classes. Every feature is a new trait or widget logic — nothing rewrites
the engine.

```
mouse click ──> UnitOrderGenerator ──> [NEW] TcdSelection : Selection.Combine()
                                        └──> [NEW] SquadManager (world, client-side)
                                              └──> whole squad selected

hotkey/button ──> SingleHotkeyBaseLogic ──> [NEW] FormationPlanner + FormationRole
                                             └──> Order.Move per actor (Mobile/Locomotor)
                                                   └──> units take up shape

rebuild button ──> composition snapshot ──> Order.StartProduction / ProductionQueue
                                             └──> [NEW] SquadRecruiter : INotifyProduction
                                                   └──> new squad at rally point
```

### What the finished repo adds

```
+ OpenRA.Mods.Tcd/                      new C# project, referenced from OpenRA.slnx
+   Traits/SquadManager.cs
+   Traits/TcdSelection.cs
+   Traits/FormationRole.cs
+   Traits/SquadRecruiter.cs
+   Formations/FormationPlanner.cs
+   Formations/FormationShapes.cs
+   Widgets/Logic/                       one hotkey logic class per command
+ mods/ra/rules/tcd.yaml                 trait swaps and role tags, one new file
+ mods/ra/hotkeys/tcd.yaml               key definitions
+ mods/ra/fluent/tcd.ftl                 UI strings
~ mods/ra/mod.yaml                       add dll to Assemblies:, include new yaml
~ mods/ra/chrome/ingame-player.yaml      new buttons in Container@COMMAND_BAR
~ mods/ra/rules/world.yaml               two lines, swapping Selection for ours
```

Four modified upstream files, all additive. That's the whole code footprint — and
it is the number CI watches.

Governance is counted separately and touched once, in sprint 00:
`CONTRIBUTING.md` and `.github/PULL_REQUEST_TEMPLATE.md` are replaced, because
GitHub surfaces those specific paths to contributors and they must describe our
rules. Upstream's issue templates are left alone; ours sit beside them under
`tcd-` filenames, so there is nothing to conflict. When the two replaced files
conflict during an upstream sync, resolve as *keep ours*.

---

## 4. Feature specs

### F1 — Persistent squads that select as one

A squad is a named, persistent group. While it exists, clicking any member
selects every member. It is *not* a control group — control groups stay as they
are, and a unit can be in both.

**How it works**

- `SquadManager` — a world trait holding `List<Squad>`, each squad a list of
  actors plus an id, a name, and a composition snapshot. Client-side only, like
  `ControlGroups` already is.
- `TcdSelection : Selection` overrides `Combine()`. If the click resolved to a
  single actor and that actor is in a squad, substitute the squad's members.
- An actor belongs to at most one squad. Forming a squad from units already in
  others pulls them out.
- `ITick` prunes dead and removed actors; an empty squad dissolves itself.

**The escape hatch matters.** If clicking always grabs eight units you can never
pick one out to repair it. So **Alt+click selects the single unit**, ignoring
squad membership. Drag-box selection also stays literal.

**Controls:** form squad · disband squad · add/remove selection from squad ·
cycle squads (camera).

```csharp
// the entire core of feature 1
public override void Combine(World world, IEnumerable<Actor> newSelection, bool isCombine, bool isClick)
{
    squads ??= world.WorldActor.Trait<SquadManager>();

    if (isClick && !Game.GetModifierKeys().HasModifier(Modifiers.Alt))
    {
        var clicked = newSelection.FirstOrDefault();
        if (clicked != null && squads.TryGetSquad(clicked, out var squad))
        {
            // isClick: false so the full list is taken, not just the first actor
            base.Combine(world, squad.Members, isCombine, false);
            return;
        }
    }

    base.Combine(world, newSelection, isCombine, isClick);
}
```

**Edge cases to file as issues now:** units loaded into a transport · units
captured or mind-controlled mid-game (`INotifyOwnerChanged` hook exists) · a
squad reduced to one unit · saving and restoring squads across a game save
(`IGameSaveTraitData`, same as `ControlGroups`).

### F2 — Formations that know what a rocket soldier is

Press one key, the squad arranges into a shape — and the arrangement is
role-aware, so riflemen end up in front of rocket soldiers.

**Roles come from YAML, not hardcoded lists.** A new `FormationRole` trait tagged
onto each unit type in your override file. This is the part that makes it
"smart", and it's data, so you can tune it without recompiling.

```yaml
E1:                     # rifle infantry
    FormationRole:
        Role: Line
        Rank: 1

E2:                     # grenadier
    FormationRole:
        Role: Skirmish
        Rank: 2

E3:                     # rocket soldier
    FormationRole:
        Role: Fire
        Rank: 3

1TNK:                   # heavy tank
    FormationRole:
        Role: Armor
        Rank: 0
```

Anything without the trait falls back to a guess from weapon range and armour
type, so the mod never breaks on a unit you forgot.

**Shapes**

- **Line** — ranks abreast, facing the enemy. Armour and short-range in front,
  grenadiers second, rockets rear.
- **Column** — for moving down a road without clumping.
- **Wedge** — armour at the point, infantry filling back along both edges.
- **Box** — all-round defence, rockets and support in the middle.
- **Screen** — deliberately loose spacing, so one artillery shell doesn't take
  the whole squad.

**The geometry, in order**

1. Take the squad's centre and a facing direction.
2. Generate ideal slots for the shape, one per unit, grouped by rank.
3. Snap each slot to a cell the unit can actually stand on (`Mobile` /
   `Locomotor` validity, nudge outward if blocked).
4. Assign units to slots by shortest total travel, so they don't cross.
5. Issue a `Move` order per unit.

**Build it in two passes, not one**

- **v1** — press the key, the squad forms up where it stands, facing wherever it
  last moved. Simple, testable, immediately useful.
- **v2** — a click-and-drag order generator: drag a line on the ground and the
  squad forms along it, facing perpendicular. This is the version that feels
  good, and it's far easier once v1's geometry is proven.
- **v3, optional** — formation-preserving movement: once formed, a normal move
  order keeps the shape by moving every unit to its offset from the destination.

**The part that is unit-testable — so it must be tested.** Slot generation and
slot-to-unit assignment are pure functions: given N units with roles, a centre,
and a facing, return N cells. No world, no rendering. That goes in `OpenRA.Test`
with real cases, and it is the one place in this project where L7 means automated
tests rather than a playtest.

### F3 — Rebuild this squad, one button

Your squad is 2 heavy tanks, 3 rocket soldiers, 2 riflemen. Press the button, all
eight get queued in the right production buildings, and when they roll out they
gather into a fresh squad.

**Queueing is the easy half**

- Each squad carries a composition snapshot: `{ "1TNK": 2, "E3": 3, "E1": 2 }`.
- For each type, read its `Buildable.Queue`, find the player's matching
  `ProductionQueue`, check `CanBuild()`, then issue
  `Order.StartProduction(queue, type, count, queued: true)`.
- The queue enforces cash and prerequisites — correct behaviour for free,
  including partial fulfilment when you're broke.
- The button reports what it couldn't queue and why, rather than silently
  dropping units.

**Gathering is the hard half**

- **v1 (this plan):** set every involved building's rally point to a gather
  point, then a `SquadRecruiter` trait implementing `INotifyProduction` watches
  for the units you ordered and enrols them locally as they appear.
- **v2:** a proper custom synced order, so gather behaviour is deterministic
  across clients. Needed only if you take this online.

**The trap to avoid.** OpenRA is lockstep-deterministic: every client simulates
the same game and only *orders* cross the network. Selection and squads are
client-side, which is why F1 and F2 are safe by construction. But if you make a
produced unit *move* based on client-side squad state, clients diverge and the
game desyncs. Rule for F3: squad membership stays local; anything that changes
the simulation goes through an order. This is **L8**, and it is the one rule with
no exceptions.

---

## 5. The ten laws

These bind everyone: you, human contributors, and any AI agent working in this
repo. They live in `CONTRIBUTING.md` and are mirrored into `AGENTS.md` so coding
agents read them automatically. Each has a short code so a review comment can just
say *"violates L3"*.

### L1 — One task. One branch. One pull request.

Every PR closes exactly one issue. If you can't write the PR title as a single
sentence without the word "and", it's two PRs. This is the rule that makes every
other rule checkable — a PR that does one thing can be reviewed, reverted, and
reasoned about; a PR that does five cannot.

### L2 — Nothing unrelated. Ever.

No drive-by fixes, no renames, no reformatting, no "while I was in there". If you
find a real bug outside your task, **open an issue and keep walking**. The only
exception is a change genuinely required to compile — and then you say so
explicitly in the PR body, in its own line, starting with `REQUIRED:`.

This is the rule AI agents break most often and most invisibly. It is also the
rule whose violation is easiest to catch: the diff shows it.

### L3 — The smallest change that satisfies the Definition of Done.

Not the most elegant, not the most general, not the most future-proof. Smallest.
No abstraction introduced for a second caller that doesn't exist yet. No
configuration option nobody asked for. No helper class wrapping one method.

Soft diff budget: **300 changed lines**, excluding generated files and assets.
Over that, CI posts a warning and you either split the PR or justify the size in
the body. A conversation trigger, not a hard block.

### L4 — Extend, don't edit.

Preference order, strongest first: new file in `OpenRA.Mods.Tcd` → subclass an
engine type → add a trait via YAML → modify an upstream file. You go down that
list only when the level above genuinely cannot work, and you say which ones you
tried in the PR.

Upstream files under `OpenRA.Game/`, `OpenRA.Mods.Common/`, `OpenRA.Mods.Cnc/`
are **protected paths**. Touching them requires the `engine-touch` label and a
written justification, and CI fails the PR without both.

### L5 — Data before code.

If a behaviour can be expressed as YAML on a trait, it is YAML. Unit roles,
formation spacing, squad size caps, key bindings, tooltips — all data. Hardcoded
lists of unit names in C# are a review rejection. The test: could a player who
doesn't write C# tune this? If yes, it belongs in YAML.

### L6 — Verify, don't assume. Cite the source.

Every claim about how the engine behaves must be backed by a real file and line
in the pinned `ENGINE_BASE`. Before you use any engine type, method or interface,
open it. Names in this codebase are easy to get *almost* right —
`INotifyProduction` exists, `INotifyUnitProduced` does not — and an almost-right
name is a compile error at best and a silent no-op at worst.

Verified facts get recorded once in `docs/ENGINE-NOTES.md` with file, line, and
the tag they were verified against, so the next person cites the ledger instead
of re-deriving it. Anything you could not verify is labelled `UNVERIFIED:` in the
PR body. Silence is not permitted; an honest "I could not confirm this" is always
acceptable, a confident guess never is.

### L7 — It builds, it lints, and it ran — or it isn't done.

No PR without `make` and `make check` passing locally, with the actual output
quoted in the body. OpenRA's lint catches bad YAML and trait misuse and it is
genuinely good — never work around it, never disable a rule to get green.

Gameplay changes additionally need evidence you played it: what map, what you
did, what happened. Pure logic (formation geometry, composition math) needs unit
tests in `OpenRA.Test`. "Should work" is not a test result and is a banned phrase
in this repo.

### L8 — Determinism is sacred.

The simulation must evolve identically on every client from the same orders.
Client-side state — selection, squads, camera, UI — may never influence it. Any
PR that touches simulation state ticks the sync checklist item in the template
and explains, in one sentence, why it cannot desync.

This is the only law with no "unless". A desync bug found three months later is
nearly impossible to trace back to the commit that caused it.

### L9 — When in doubt, stop and ask. Doubt is not failure.

Ambiguity in an issue, two defensible designs, a change that would exceed
declared scope, a second failed attempt at the same fix — all of these mean
**stop and ask**, not *pick one and hope*. Asking costs a comment. Guessing costs
a review cycle, and sometimes a subtle bug nobody finds for a month.

The explicit trigger list is in §6. It applies to humans too.

### L10 — Leave the record.

The PR body says what changed, why, what you verified and how, and what you
deliberately did not do. Six weeks from now this is the only surviving
explanation, and in an open-source project it is how a stranger learns to
contribute. Every PR must also be revertable with `git revert` alone — if undoing
it needs manual cleanup, restructure it.

---

## 6. Rules for AI contributors

AI agents are welcome in this repo, and they are held to a stricter procedure
than humans — not because they're worse, but because their failure modes are
specific, confident, and quiet. These rules live in `AGENTS.md` at the repo root,
with `CLAUDE.md` pointing at the same file.

### The work protocol

Five phases. None skipped. Phase 3 never begins before phase 2 is approved.

| Phase | What happens | Gate |
|---|---|---|
| 1 · Read | Open the issue and every file you intend to touch. Read the engine code you plan to call. Check `docs/ENGINE-NOTES.md` before deriving anything yourself. | — |
| 2 · Plan | Post a plan as a comment on the issue: the approach, **the exact list of files you will create or modify**, what you verified and where, and anything still unknown. | Human approves before any code is written. |
| 3 · Build | Implement exactly the approved plan. Nothing more. If reality diverges from the plan, go back to phase 2 — don't improvise. | Diff must match the declared file list. |
| 4 · Verify | Run `make`, run `make check`, run the tests, launch and play the relevant case. Record real output. | Green, with output quoted. |
| 5 · Report | Open the PR: what changed, why, verification evidence, what was deliberately left out, and every `UNVERIFIED:` assumption. | Human review. |

### The scope manifest

Phase 2's file list isn't a courtesy — it's a contract, and CI checks it. The PR
body carries a fenced block that a script compares against the actual diff:

````
```scope
OpenRA.Mods.Tcd/Traits/SquadManager.cs
OpenRA.Mods.Tcd/Traits/TcdSelection.cs
mods/ra/rules/tcd.yaml
```
````

A file in the diff that isn't in the block fails the build. This single mechanism
enforces L1, L2 and L3 at once, and it turns "don't change unrelated things" from
a hope into a red X. Widening scope is legitimate and easy — edit the block, say
why in a comment, get it re-approved.

### Anti-hallucination rules

**A1 — Never name an engine symbol you have not opened.** Traits, interfaces,
methods, YAML keys: grep for it, read it, then use it. Plausible-looking API
names are the single most common AI failure in a codebase this size, and OpenRA
has over 500 traits with families of near-identical names.

**A2 — Cite file and line, or mark it UNVERIFIED.** Two acceptable forms and no
third: `Selection.cs:92 — Combine is virtual`, or `UNVERIFIED: I believe rally
points are synced; not confirmed`. Hedge words carrying an unmarked claim —
"should", "typically", "I believe", "probably", "in most cases" — are banned in
plans, PR bodies and code comments.

**A3 — Never invent game data.** Unit names, costs, hitpoints, weapon names,
sprite sequences, sprite-sheet coordinates: read them out of the YAML in this
repo. Red Alert knowledge from training data is not a source — it may describe
the original 1996 game, a different mod, or nothing real at all.

**A4 — Two failed attempts, then stop.** If the same fix fails twice, stop and
report what you tried and what happened. Do not thrash, do not start rewriting
adjacent code to make the problem go away, do not disable the check that's
failing. A clear report of a stuck problem is a good outcome.

**A5 — Speak OpenRA.** Actor, trait, activity, order, locomotor, widget, chrome,
sequence, mod rules. Not "entity", "component", "sprite handler", "UI layer".
Shared vocabulary is how you notice that someone — human or model — is describing
a system they haven't actually read.

**A6 — Measure before you optimise.** No performance claim without a number from
OpenRA's perf overlay or a profiler. "This is faster" without a measurement is
rejected, and speculative optimisation is an L3 violation anyway. Do note
per-tick allocations in anything running inside `ITick` — that path is genuinely
hot, which is exactly why it deserves evidence rather than instinct.

**A7 — Disclose that you are an agent.** Commits carry a `Co-Authored-By` trailer
naming the model, and the PR body says which parts were AI-written. Not a badge
of shame — it tells reviewers where to look hardest, and an open-source project
that hides this loses trust it can't easily get back.

### Hard stops — never, under any instruction

- `git push --force`, or rebasing a branch that has been pushed
- `git add -A` / `git commit -a` — stage the files you named, one by one
- Amending or rewriting anyone else's commits
- Committing to `tcd` or `bleed` directly
- Editing files outside the declared scope block
- Adding, upgrading or removing a dependency without an approved issue
- Deleting or weakening a test to make a build pass
- Disabling a lint rule, or using `--no-verify`
- Touching `.github/workflows/`, `CODEOWNERS`, or the rule files themselves
  inside a feature PR
- Committing secrets, tokens, or anything from `~/.config/openra/`
- Committing Westwood or EA game assets — OpenRA ships none, and neither do we
- Bulk reformatting, re-indenting, or reordering existing code

### Stop-and-ask triggers

| When | Then |
|---|---|
| The issue can be read two ways | Ask which. Don't pick. |
| Two designs both look defensible | Present both with trade-offs |
| The fix needs a file outside the scope block | Ask to widen scope first |
| The fix needs a protected engine path | Ask — usually a design smell |
| The same attempt has failed twice | Report. Don't retry a third time. |
| A test fails and the test looks wrong | Ask. Never edit the test yourself. |
| The change might affect simulation state | Ask, and flag L8 explicitly |
| The diff is heading past ~300 lines | Ask whether to split |
| You'd need to delete existing behaviour | Ask. Deletion is never implied. |
| An engine fact can't be verified in source | Ask. Don't reason from priors. |

### One reframe worth making explicit

You asked for a rule that says "always find the most efficient and best way". I'd
write it as **L3** instead — the *smallest change that meets the Definition of
Done* — because "best" is unbounded, and an instruction to pursue it reliably
produces gold-plating: extra abstraction, speculative options, larger diffs, and
slower reviews. In a codebase you're still learning, small and boring beats
clever almost every time, and small changes are the ones you can safely revert.

Efficiency still has a home: A6 says optimise when you have a measurement, and L4
and L5 push you toward the cheap solution before the expensive one. That's the
enforceable version of what you're after.

---

## 7. Rules for human contributors

Shorter, because the ten laws already cover most of it. These go in
`CONTRIBUTING.md`, which is what GitHub shows a first-time contributor.

**H1 — Issue first, code second.** No surprise PRs, however good. Open an issue,
agree the approach, then build. A rejected 400-line PR wastes the contributor's
evening and the maintainer's goodwill — and that's a worse outcome for the
project than the feature not existing.

**H2 — Match the house style.** OpenRA has an established style and ships
analyzers that enforce it. Don't bring your own conventions, don't reformat what's
there, and let `make check` settle any argument about formatting.

**H3 — Sign off your commits, and mean it.** `git commit -s` — a Developer
Certificate of Origin sign-off certifying you wrote it and can license it under
GPL v3. If an AI wrote part of it, say so (A7). If you copied it from another
project, say where, and check the licence is compatible.

**H4 — Never commit game assets.** No sprites, audio, video or map files
originating from Command & Conquer, Red Alert, or any Westwood/EA release.
OpenRA ships none and downloads them from the player's own copy — the same line
applies here, and it's the line that keeps the project legal.

**H5 — If it belongs upstream, send it upstream.** A genuine engine bug fix or a
broadly useful trait should be a PR to OpenRA, not a permanent patch in our fork.
Every upstream file we carry is a file that will conflict at every release.

**H6 — Review like a colleague, not a gatekeeper.** Cite the law code, say what
you'd accept instead, and separate blocking objections from preferences. The
inherited Code of Conduct applies to everyone, and maintainers are held to it
hardest. A contributor's first PR sets whether they ever open a second.

**While you're solo:** you'll be reviewing your own PRs for a while. Do it
anyway, and leave the comment — write the review as if a stranger will read it,
because eventually one will. Self-merging without a written review is exactly the
habit that decays first.

---

## 8. Making the rules real

A rule nobody can check is a wish. Every law maps to a file an agent reads
automatically, a template checkbox a human has to tick, or a CI job that goes
red.

### Files that carry the rules

| File | Purpose |
|---|---|
| `AGENTS.md` | The AI contract — protocol, scope manifest, anti-hallucination rules, hard stops, stop-and-ask triggers. Read automatically by most coding agents. |
| `CLAUDE.md` | One line pointing at `AGENTS.md`. One source of truth, two filenames, no drift. |
| `CONTRIBUTING.md` | The ten laws, the human rules, how to build, how to test, how to open a PR. |
| `ENGINE_BASE` | One line: the pinned upstream commit SHA. Every engine claim is a claim about that commit. |
| `docs/ENGINE-NOTES.md` | The verified-facts ledger. Claim, file, line, tag, who verified, when. |
| `docs/PLAN.md` | This document, committed, so the plan lives beside the code. |
| `.github/pull_request_template.md` | Scope block, verification evidence, sync checklist, UNVERIFIED list. |
| `.github/ISSUE_TEMPLATE/` | Feature, bug, engine-note. Each asks for a Definition of Done up front. |
| `.github/protected-paths.txt` | Globs that require the `engine-touch` label. |
| `CODEOWNERS` | Rule files and workflows owned by the maintainer, so changes to them need explicit review. |

### CI jobs, and the law each one enforces

| Job | What it does | Enforces |
|---|---|---|
| `build` | `make` — the fork compiles | L7 |
| `lint` | `make check` — OpenRA's YAML and trait lint plus style analyzers | L7, H2 |
| `test` | `OpenRA.Test`, including formation geometry cases | L7 |
| `scope` | Parses the PR's `scope` block, diffs it against changed files, fails on anything undeclared | L1, L2, L3 |
| `protected-paths` | Fails if the diff touches a protected glob without the `engine-touch` label | L4 |
| `diff-size` | Warns (doesn't block) past 300 changed lines | L3 |
| `hedge-check` | Greps the PR body for banned hedge words outside an `UNVERIFIED:` line | A2 |
| `dco` | Every commit signed off | H3 |
| `assets` | Fails on `.mix`, `.shp`, `.aud`, `.vqa` and friends anywhere in the diff | H4 |

**The scope job is the keystone.** Of everything here, it earns its keep first.
"Don't change unrelated things" and "make the smallest change" are the two rules
that get broken most, by humans and models alike, and they're the two a reviewer
has the least reliable instinct for — an extra file in a 15-file diff simply
doesn't register. Comparing a declared list against the actual diff catches it
every time, costs about thirty lines of shell, and never gets tired.

### The engine notes ledger

Every verified fact about the engine gets one entry, and the ledger is the first
thing anyone reads before deriving something themselves. It's how a hallucination
gets caught by the next contributor rather than by a bug six weeks later.

```markdown
## Selection.Combine is virtual and safe to override

- **Claim:** `Selection.Combine(World, IEnumerable<Actor>, bool, bool)`
  is `public virtual`; `SelectionInfo` is a normal swappable world trait.
- **Source:** OpenRA.Mods.Common/Traits/World/Selection.cs
- **Also:** mods/ra/rules/world.yaml — `Selection:` under `^BaseWorld`
- **Verified against:** <ENGINE_BASE>
- **Verified by:** @abdullah — 2026-08-26
- **Used by:** F1 (squad selection)
```

Seed it in sprint 00 with the facts this plan already rests on: the `Selection`
override point, `Order.StartProduction`'s signature, `INotifyProduction` versus
`INotifyOtherProduction`, the `Assemblies:` line in `mods/ra/mod.yaml`, and the
command-bar container in `mods/ra/chrome/ingame-player.yaml`.

---

## 9. Sprints

Each sprint ends with something you can launch and play. No sprint leaves the
game unbuildable. One GitHub milestone per sprint, and **no work starts on sprint
N+1 while sprint N is open** — that's L1 at project scale.

### What actually happened

The record below is the work as it landed, which is not how this section first
imagined it. Formations turned out to be one sprint rather than two, squad
reproduction moved up, and the squad-UX polish slipped. The plan bends. The
record does not get rewritten to match the plan.

| Sprint | Landed as | PR |
|---|---|---|
| 00 — Constitution | `AGENTS.md`, `CONTRIBUTING.md`, `CLAUDE.md`, PR and issue templates, `protected-paths.txt`, `CODEOWNERS`, the TCD Rules workflow, `ENGINE_BASE`, a seeded engine-notes ledger | #1 |
| 01 — Ground truth | The `OpenRA.Mods.Tcd` project, wired into `OpenRA.slnx` and the `Assemblies:` line of `mods/ra/mod.yaml`, proving the dll loads | #2 |
| 02 + 03 — Squads | `SquadManager`, `TcdSelection`, form and disband hotkeys, the Alt+click escape hatch, dead-actor pruning, command-bar buttons and the icon sheet | #3 |
| 04 — Formations | `FormationRole` on 19 RA actors, `FormationPlanner`, Grid and Wedge, the drawn line (V), the marked shape (G), the collapsible tool tray, geometry tests | #5 |
| 05 — Squad rebuild | `SquadComposition`, `SquadProduction`, `SquadRecruiter`, the rebuild button and Ctrl+R | #7 |
| — | The five production and trait-location facts sprint 05 rested on, written into the ledger | #9 |

Shipped as **0.1.0**: a Red Alert AppImage on the releases page.

**Owed, not delivered.** The squad badge over each member, add-to and
remove-from squad, the cycle-squads camera key, and squad membership surviving a
game save. That was sprint 03's back half. It is still wanted; it is sprint 12.

### Sprint 06 — Housekeeping

The README still describes OpenRA rather than this fork. The changelog stops at
sprint 00. `AGENTS.md` section 7 names an upstream file we never edited and
omits one we did. `ci.yml` runs its build on push but not on pull requests to
`tcd`, so the checks gating a PR are thinner than they look.

**Done when** a stranger landing on the repo can tell what it is inside one
screen, the changelog covers every merged PR, section 7 matches a real diff of
`ENGINE_BASE` against `tcd`, and a PR to `tcd` is gated by the full build.

### Sprint 07 — Maps

Red Alert ships 74 map folders and 38 of them are campaign missions, so the
skirmish rotation was thin. That is the problem players actually felt.

Most of the machinery was already in the engine, and a play test settled the part
that mattered: a map generated in the lobby reaches every client, because OpenRA
sends the generation recipe rather than the map file and each client builds an
identical copy. No transfer, no wait.

This sprint first planned presets, a TCD entry in the terrain dropdown bundling
the settings for a large game. That was dropped, and rightly. The generator is
mature; a preset saves four clicks and adds nothing a player could not already
do. Touching a working system for that is not a trade worth making.

What was built instead is the tooling to curate a pack, and the pack:

- `--generate-maps`, a utility command that runs the generator headlessly and
  writes each map beside a preview image, so a batch is judged by looking at
  pictures rather than opening the game a hundred times (#11).
- `tcd-map-recipes.sh`, twenty recipes. Ten vary the terrain, ten vary the
  economy — no ore at all with oil derricks as the entire income, a
  mountain-walled arena, gems everywhere, a town at maximum civilian density
  (#11).
- 85 maps from those recipes, eight to sixteen players, shipped in
  `mods/ra/maps/` and therefore inside the AppImage: no download, no licence
  question (#13). Fifteen of the hundred attempts could not place valid spawns
  on the harder settings and were dropped rather than shipped broken.

The scope job had to learn shell patterns on the way: declaring a hundred maps
file by file would have been 255 lines of manifest (#12).

**Done.** The pack runs a night of games without repeating a map.

**Not done, deliberately.** An in-game browser for the 23,000 community maps.
The Resource Center's API documents lookup by hash and by id and nothing else, so
a browser would need an index we curate and host. Bundling those maps is out of
the question regardless: the site states no licence, so they stay with their
authors. A server can already advertise a map pool that clients download from,
which covers most of the want.


### Sprint 08 — A dedicated server anyone can join

Playing together means one of you hosting, which means that person being online
and their router forwarding a port. A dedicated server removes both.

`launch-dedicated.sh` already takes its whole configuration from environment
variables, so this is packaging rather than engineering:

- `Dockerfile` — builds with the .NET SDK image, runs on the runtime image,
  carrying `bin/`, `mods/` and the upstream launch script.
- `docker/entrypoint.sh` — Red Alert's `.mix` files are Westwood's and are never
  committed here, so the container fetches OpenRA's freeware package on first
  start, checks it against the SHA1 recorded in
  `mods/ra-content/installer/downloads.yaml`, and unpacks it into a volume.
- `docker-compose.yml` — one service, TCP 1234, one volume.

The server speaks OpenRA's protocol rather than HTTP, so none of it belongs
behind a reverse proxy. On a host running Caddy the game port is published
directly and Caddy carries on serving HTTP for everything else; routing raw TCP
through Caddy would need the layer4 plugin and buys nothing. `AdvertiseOnline`
registers the server with master.openra.net, so players find it in the browser
instead of typing an address.

**Done when** a friend opens the multiplayer browser, sees the server, joins, and
plays a TCD map with nobody hosting.

### Sprint 09 — Release engineering

Every release so far has been built on a laptop: comment two lines out of
`packaging/linux/buildpackage.sh`, run it, restore the file, upload by hand. That
is four chances to get it wrong, and it ties the artefact to one machine.

- Tagging `tcd-x.y.z` builds the Red Alert AppImage and attaches it to a draft
  release, and builds the server image from that same commit and pushes it to a
  registry, so a deployment pulls an image rather than compiling from source.
- The two mods this fork does not ship are commented out of
  `packaging/linux/buildpackage.sh` for the length of the run. Upstream's file
  stays untouched; the manual step goes away all the same.

**The image is built from the tag, not from every merge.** The plan first said
merge, and that was wrong. OpenRA is lockstep: a server running the tip of `tcd`
while its players run a released AppImage is two different programs agreeing to
simulate the same game, and the desync that follows is close to untraceable.
Building both from one commit is what makes them the same program. A server on
the tip of `tcd` is a separate thing, with its own tag, if we ever want one.

The AppImage is not built on every merge either. Sixty megabytes per merge is
waste, and multiplayer needs everyone on one build, so a release stays a
deliberate act - the draft is CI's work, publishing it is a person's.

**Done when** pushing a tag produces a draft release holding a working AppImage
and a matching server image in the registry, with nobody opening a terminal.

### Sprint 10 — Target priority by role

Send a squad into a crowd today and everyone shoots whatever is nearest. Rocket
soldiers empty themselves into infantry while riflemen scratch at a tank. Fixing
that by hand, unit by unit, is the micro tax Red Alert has charged since 1996.

`FormationRole` already knows what each unit is for. Give each role an ordered
list of what it should shoot at, in YAML: rockets before aircraft and vehicles,
rifles before infantry, artillery before buildings.

No weapon-versus-armour tables are read. The priority is declared rather than
derived, so a player can tune it and it stays legible.

**Done when** a mixed squad ordered onto a mixed group of enemies splits its fire
sensibly with nobody selected individually, and the ordering can be changed
without touching C#.

### Sprint 11 — Patrol

Three of the things we want turn out to be one thing. A patrol is a unit moving
between points until told otherwise. A medic holding a zone is a patrol that heals
what it passes. A scout sweeping the map is a patrol with a wider route. A guard
is a patrol with a route of one point. OpenRA has no patrol activity, so that is
the piece to build.

Build the patrol. The other three become configurations of it rather than features
of their own.

**Done when** a squad given a route walks it indefinitely, resumes it after being
interrupted by a fight, and a medic on a route keeps a defended area healed with
no further orders.

### Sprint 12 — A reserve that comes when called

Hold a squad back. When something of yours is attacked within a radius of it, it
goes, fights, and returns. The engine knows nothing of this, and it is the idea in
this fork furthest from anything Red Alert ships: not a better way to give an
order, but an order that keeps being obeyed while you are looking elsewhere.

The hard parts are honest ones. What counts as an attack worth answering, how far
is too far, and what stops the whole reserve chasing one harvester across the map.

**Done when** a reserve squad defends a base under attack while its owner is
fighting somewhere else, and returns to its post afterwards.

### Sprint 13 — Factions

Red Alert's five countries differ by one unique unit each, gated behind a
prerequisite like `~vehicles.england`. `SPY.England` and `AFLD.Ukraine` show that a
variant of an existing actor is a legitimate unique: the art is shared, the
statistics are not. Factions are declared on the `World` actor, and this fork
already has a `World:` node of its own, so none of this touches an upstream file.

| Faction | Side | Identity | Unique |
|---|---|---|---|
| Turkey | Allies | Reconnaissance and standoff strike | A drone centre building three UAVs - a scout, a strike drone, a kamikaze drone - and a defensive bonus |
| Italy | Allies | The sea | Cheaper, faster naval units and an armed landing craft |
| East Germany | Soviet | Precision and surveillance | A sniper: long range, expensive, slow to fire, and infantry drop to one shot |
| Poland | Soviet | Raiding | A fast medium tank with thin armour |
| Cuba | Soviet | Guerrilla | A cheap, lightly armed car with almost no armour |

The sniper needs no new art: `sniper.shp` and its weapon already ship inside
`mods/ra/maps/fort-lonestar/` as OpenRA's own freely licensed work. The drones did
need art and it is drawn - `tcd-drone-sprites.py` generates the frames and
`--png-to-shp` packs them, both waiting on `feat/turkey-drones`. The drone centre
reuses the helipad the way `AFLD.Ukraine` reuses the airfield.

**One faction at a time.** Balance never finishes, and five untested factions at
once is five unknowns rather than one. Turkey sets the pattern; the rest follow
once it has been played.

**Done when** Turkey is selectable, its drones fly, its bonus is felt, and a game
against it does not feel like a game against a different mod.

### Sprint 14 — Theme and presentation

The command layer works and looks bolted on. Redraw the icon set to sit beside
Westwood's sidebar art, fix the tray's placement and spacing, settle the tooltip
wording, give the squad badge some typography, and take colours from the faction
palettes instead of plain white.

**Done when** somebody who has played Red Alert cannot tell at a glance which
buttons we added.

### Sprint 15 — Nix flake

`flake.nix` at the repo root. A `devShells.default` carrying the .NET 10 SDK,
SDL2, OpenAL, lua5.1 and freetype, and a `packages.default` built with
`buildDotnetModule`. Tracked as issue #8.

**Done when** `nix develop` gives a shell where `make check && make tests && make`
all pass, `nix build` produces something that launches, and the README documents
both.

### Sprint 16 — Squad UX, the half sprint 03 owed

The squad badge drawn over each member (copy `WithTextControlGroupDecoration`),
add-to and remove-from squad, a cycle-squads camera key, and squad membership
surviving a game save through `IGameSaveTraitData`.

**Done when** you play a full skirmish using squads instead of control groups and
do not miss them.

### Not planned, deliberately

**Formation-preserving movement.** A squad was to keep its shape while crossing
the map. Planned, then dropped before any code was written: it changes how a walk
looks rather than how a fight goes, and the geometry it needs is the fiddliest in
the codebase. Reconsider only if holding a shape turns out to matter for something
else.

**An in-game browser for community maps.** The Resource Center's API documents
lookup by hash and by id and nothing else, so a browser would need an index we
curate and host. A server can already advertise a map pool that clients download
from, which covers most of the want. Bundling those maps is out of the question
either way: the site states no licence, so they stay with their authors.

**Native distribution packages and a Windows installer.** `packaging/` supports
neither, upstream OpenRA ships neither, and the AppImage already runs on every
distribution, NixOS included through `appimage-run`.

---

## 10. How to run it on GitHub

**Milestones and issues.** One milestone per sprint. Break each sprint into 3–6
issues before you start it, never during. Every issue carries its own Definition
of Done — if you can't write one, the issue isn't ready. Labels from day one:
`squads`, `formations`, `production`, `engine-touch`, `yaml`, `ui`,
`upstream-sync`, `ai-assisted`, `good-first-issue`, `later`.

**Branches.** Long-lived: `tcd` (default, protected) and `bleed` (untouched
upstream mirror). Everything else short-lived: `feat/squad-manager`,
`fix/squad-prune-on-death`. Merge to `tcd` via PR — yes, even alone. Turn on
branch protection in sprint 00: no direct pushes, CI must pass, one approving
review.

**Commits.** Conventional commits, scoped to the feature:

```
feat(squads): select whole squad on member click
fix(squads): drop dead actors on tick
feat(formations): add wedge shape
chore(upstream): merge release-2026XXXX
```

**Staying in sync with upstream** — between sprints, never during one:

```bash
git fetch upstream
git switch tcd
git merge upstream/bleed
make check && make tests && make
git rev-parse upstream/bleed > ENGINE_BASE   # re-pin
# re-verify every entry in docs/ENGINE-NOTES.md against the new commit
# play one skirmish before you trust it
```

Its own PR, labelled `upstream-sync`, exempt from the diff-size warning. Because
your code sits in its own project and your YAML in its own files, this should
touch only the four upstream files you modified.

**Worth setting up early**

- `CHANGELOG.md` — one line per merged PR.
- Screenshots in the README from sprint 03 onward. The difference between a repo
  people try and one they scroll past.
- A GitHub Project board with the sprint milestones, so "what's next" is never a
  question.
- Discussions enabled — it gives outsiders somewhere to ask before opening a bad
  issue.

---

## 11. What could go wrong

| Risk | How we handle it |
|---|---|
| Upstream drift | All code in its own project; four additive edits to upstream files, guarded by the protected-paths CI job so the number can't quietly grow. |
| AI-authored plausible nonsense | A1–A3 plus the engine-notes ledger. The ledger is the real defence: it turns verification into a shared asset instead of something each contributor redoes and each model re-guesses. |
| Scope creep inside a single PR | The scope manifest job. Mechanical, unarguable, catches humans and models equally. |
| Rules that everyone ignores | Every law maps to a CI job or a template field. If a rule can't be mapped to one, it's advice — say so, and don't call it a rule. |
| Formation pathfinding cost | Eight simultaneous move orders is fine, eighty is not. Cap squad size; profile before optimising (A6). |
| Slots on impassable terrain | Validate every slot against the unit's locomotor and spiral outward for the nearest legal cell. Budget real time in sprint 04. |
| Desync, if you ever go online | L8 and the sync checklist item on every PR. Costs nothing now, saves a rewrite later. |
| Process heavier than the project | Real risk. Review the rules after sprint 03: anything that has never caught a defect gets deleted. Governance earns its place or it goes. |
| Scope creep across the project | Formations v3 and multiplayer sync are out of scope. File them as issues, label `later`, don't start them. |

---

## 12. What happens next

Review this, and push back on anything heavier than the project needs — the
second-to-last row of the risk table is there because over-governing a
two-person repo is a real failure mode, not a hypothetical one.

When you're happy: connect `~/Git` in the Claude desktop app. Then: check which
.NET SDK is installed, which OpenRA version is actually on the machine, and
whether the machine can reach GitHub directly. Sprint 00 is mostly files, and
they can all be written in one pass once the fork exists.

---

*Sections 1–4 were checked against the current OpenRA source — trait names, file
paths, method signatures and hook points are real, not approximations. Line
numbers and the exact .NET version shift depending on which release tag you base
on; both get confirmed and recorded in `docs/ENGINE-NOTES.md` in sprint 00.*
