# Tactics & Command Dynamics

A fork of [OpenRA](https://github.com/OpenRA/OpenRA) that adds a command layer to
the Red Alert mod: squads that stay together, formations that know what a rocket
soldier is, and a button that rebuilds a squad you just lost.

Red Alert's unit control has not changed much since 1996. Control groups
remember a set of units, and that is the whole of it — click one member and you
select one member, order a group to move and they arrive as a crowd, lose a
squad and you rebuild it one icon at a time. TCD keeps everything else about
OpenRA and replaces that layer.

[![Latest release](https://img.shields.io/github/v/release/AbdullahZeynel/OpenRA-tcd?label=download)](https://github.com/AbdullahZeynel/OpenRA-tcd/releases)

---

## What it adds

**Persistent squads.** Box-select a mixed group, press <kbd>Ctrl</kbd>+<kbd>Q</kbd>,
and they become a squad. From then on, clicking any one of them selects all of
them. <kbd>Alt</kbd>+click is the escape hatch when you want the single unit.
Dead members drop out on their own. This is separate from control groups, which
still work exactly as they did.

**Role-aware formations.** Every RA infantry and vehicle actor carries a
`FormationRole` — armour, line, skirmish, fire support. Formations place units by
role and rank rather than by whatever order they happened to be selected in, so
tanks take the front and rocket soldiers stand behind them. Two presets (Grid and
Wedge) and two drawing tools: <kbd>V</kbd> to drag a straight line and have the
squad distribute itself along it, <kbd>G</kbd> to mark out a shape corner by
corner.

**Squad rebuild.** <kbd>Ctrl</kbd>+<kbd>R</kbd> queues the selected squad's exact
composition again — three riflemen, two grenadiers, one light tank, whatever it
was. The new units leave their factory at its rally point and group themselves
into a fresh squad once the whole batch is out. Press it several times and you
get several squads. Money never blocks the order; it only slows it down.

### Keys

| | |
|---|---|
| <kbd>Ctrl</kbd>+<kbd>Q</kbd> | Form a squad from the selection |
| <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>Q</kbd> | Disband the squad |
| <kbd>Ctrl</kbd>+<kbd>R</kbd> | Rebuild the squad's composition |
| <kbd>V</kbd> | Draw a line, then place the squad along it |
| <kbd>G</kbd> | Mark a shape corner by corner, press again to close it |
| <kbd>Alt</kbd>+click | Select one member instead of the whole squad |

Everything on that list also has a button, under the TCD toggle at the right-hand
end of the command bar. All of it is rebindable in the in-game hotkey browser.

---

## Play

### Windows x64

For a Windows portable build, extract the entire `OpenRA-TCD-tcd-*-win-x64.zip`
archive into a writable folder and run `RedAlert.exe`. The .NET runtime and
native libraries are included; game assets download on first launch.
Use the same TCD release as the other players.

To create the archive on Linux with .NET 10 SDK, Git, Python 3, zip, tar, and
curl or wget installed:

```sh
./packaging/windows/build-tcd.sh tcd-0.2.0 "$PWD/build/windows"
```

The script exports the named local tag into a temporary directory, so uncommitted
changes and newer gameplay commits do not enter the package. Its engine version
is `release-<tag>`, matching the Linux AppImage. It produces a portable ZIP;
no installer or system-wide installation is needed.

### Linux

Download the AppImage from the
[releases page](https://github.com/AbdullahZeynel/OpenRA-tcd/releases). It is
self-contained — .NET, SDL2, OpenAL, Lua and freetype all travel inside it — and
it needs FUSE 2 on the host.

```sh
chmod +x OpenRA-Red-Alert-x86_64.AppImage
./OpenRA-Red-Alert-x86_64.AppImage
```

If FUSE 2 is missing:

```sh
sudo pacman -S --needed fuse2          # Arch, CachyOS, Manjaro
sudo apt install libfuse2t64           # Debian, Ubuntu, Mint
sudo dnf install fuse fuse-libs        # Fedora
sudo zypper install fuse libfuse2      # openSUSE
```

Without FUSE at all, this works anywhere:

```sh
./OpenRA-Red-Alert-x86_64.AppImage --appimage-extract-and-run
```

On NixOS:

```sh
nix-shell -p appimage-run --run "appimage-run ./OpenRA-Red-Alert-x86_64.AppImage"
```

Red Alert's assets download on first launch. Multiplayer needs every player on
the same build.

---

## Build from source

You need the .NET 10 SDK. Everything else the build fetches itself.

```sh
git clone https://github.com/AbdullahZeynel/OpenRA-tcd
cd OpenRA-tcd
make            # release build
make check      # lint and analysers, warnings are errors
make tests      # unit tests
make test       # mod and YAML validation
./launch-game.sh Game.Mod=ra
```

`INSTALL.md` covers the platform-specific parts, inherited from upstream.

---

## Contribute

The project runs on a small set of rules that bind humans and AI agents alike —
one task per pull request, nothing unrelated in a diff, cite the source or mark
it unverified, and stop and ask when in doubt. They are short, and CI enforces
most of them mechanically.

- Humans start at [`CONTRIBUTING.md`](CONTRIBUTING.md).
- AI agents start at [`AGENTS.md`](AGENTS.md). `CLAUDE.md` points to the same
  file; there is one source of truth.
- [`docs/PLAN.md`](docs/PLAN.md) has the feature specs and the sprint record.
- [`docs/ENGINE-NOTES.md`](docs/ENGINE-NOTES.md) is the ledger of verified engine
  facts. Read it before deriving anything about OpenRA yourself, and add to it
  when you verify something new.

The engine commit this fork is built on is pinned in `ENGINE_BASE`. Every claim
about engine behaviour in this repository is a claim about that commit.

---

## Upstream

TCD is a fork, not a rewrite. The engine, the three mods, the maps, the art and
the vast majority of the code are OpenRA's work, and bug reports about anything
outside `OpenRA.Mods.Tcd/` and the files named `tcd.*` belong
[upstream](https://github.com/OpenRA/OpenRA/issues).

- Website: [openra.net](https://www.openra.net)
- Repository: [github.com/OpenRA/OpenRA](https://github.com/OpenRA/OpenRA)
- Wiki and FAQ: [github.com/OpenRA/OpenRA/wiki](https://github.com/OpenRA/OpenRA/wiki)

Licensed under the GPL v3, inherited. See [`COPYING`](COPYING). Contributors are
listed in [`AUTHORS`](AUTHORS), and the
[Code of Conduct](CODE_OF_CONDUCT.md) applies here too.

EA has not endorsed and does not support this product.
