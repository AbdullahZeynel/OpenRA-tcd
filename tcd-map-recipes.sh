#!/usr/bin/env bash
#
# The recipes behind the TCD map pack.
#
#     ./tcd-map-recipes.sh OUTPUTDIR [COUNT] [FILTER]
#
#     ./tcd-map-recipes.sh ~/tcd-maps          # 5 maps per recipe, 100 in total
#     ./tcd-map-recipes.sh ~/tcd-maps 20       # 20 each
#     ./tcd-map-recipes.sh ~/tcd-maps 5 Oil    # only recipes whose name contains Oil
#
# Every map lands as an .oramap next to a .png of itself, so a batch can be sifted
# by looking at pictures. Sift first; launch only what survives.
#
# Run it from the repository root after `make`. Seeds come from the clock unless
# Seed= is passed, so two runs of the same recipe give different maps.
#
# RA's map grid is Rectangular, so a map is square and its size is the playable
# width plus a two-cell border. 142 and 162 are well past anything the mod ships:
# the largest stock map is 128x128.
#
# The option vocabulary lives in mods/ra/rules/map-generators.yaml. Worth knowing:
#
#   Buildings=Standard   0-3 hospital, comm centre, oil derrick
#   Buildings=Extra      3-6, adds forward command posts
#   Buildings=OilRush    8-10 oil derricks and nothing else
#   Resources=None       no ore anywhere - derricks become the entire economy
#   Density=Area*        scales entity counts by map area rather than player count
#   Shape=CircleMountain a round arena walled by mountains
#   Shape=CircleWater    a landmass with an ocean around it

set -o errexit -o pipefail

OUT="${1:-$HOME/tcd-maps}"
COUNT="${2:-5}"
FILTER="${3:-}"

gen() {
	local title="$1"
	shift

	if [ -n "$FILTER" ] && [[ "$title" != *"$FILTER"* ]]; then
		return 0
	fi

	echo
	echo "=== $title ==="
	./utility.sh ra --generate-maps "$OUT" "$COUNT" Title="$title" "$@"
}

mkdir -p "$OUT"

# ============================================================================
# Terrain. Same economy throughout; the ground decides how the game is played.
# ============================================================================

# --- No sea. Ground war, terrain does the work. ---

# Two teams of four across a mountain spine. Corridors, and a lot to fight over.
gen "Iron Corridor" Tileset=TEMPERAT Players=8 Symmetry=2Rotations Size=142x142 \
	TerrainType=Mountains Resources=High Buildings=Extra CivilianDensity=High

# Four corners, broken rock, roads between. Open enough for armour.
gen "Rocky Standoff" Tileset=TEMPERAT Players=8 Symmetry=4Rotations Size=142x142 \
	TerrainType=Rocky Resources=High Buildings=Extra CivilianDensity=Medium

# Three teams of three in heavy woodland. Ambush country.
gen "Three Fronts" Tileset=TEMPERAT Players=9 Symmetry=3Rotations Size=142x142 \
	TerrainType=Woodlands Resources=High Buildings=Extra CivilianDensity=High

# --- Inner seas. Lakes cut the map up without cutting anyone off. ---

# Mountain lakes, two teams of five.
gen "Inland Lakes" Tileset=TEMPERAT Players=10 Symmetry=2Rotations Size=162x162 \
	TerrainType=MountainLakes Resources=High Buildings=Extra CivilianDensity=High

# Three teams of four, water in narrow channels. Bridges and chokepoints.
gen "Narrow Straits" Tileset=TEMPERAT Players=12 Symmetry=3Rotations Size=162x162 \
	TerrainType=NarrowWetlands Resources=Medium Buildings=Standard CivilianDensity=Medium

# --- Outer sea. The map is a landmass with an ocean around it. ---

# Six-way, twelve players, ringed by water.
gen "Ringed Sea" Tileset=TEMPERAT Players=12 Symmetry=6Rotations Size=162x162 \
	TerrainType=Wetlands Shape=CircleWater Resources=High Buildings=Extra CivilianDensity=Medium

# Islands, four-way. Naval play matters here.
gen "Archipelago" Tileset=TEMPERAT Players=12 Symmetry=4Rotations Size=162x162 \
	TerrainType=LargeIslands Resources=High Buildings=Extra CivilianDensity=High

# Two continents, eight a side. The biggest thing this pack builds.
gen "Two Continents" Tileset=TEMPERAT Players=16 Symmetry=2Rotations Size=162x162 \
	TerrainType=Continents Resources=High Buildings=Extra CivilianDensity=VeryHigh

# --- Poor. Surviving is the game. ---

# Desert, thin ore, oil derricks and nothing else standing.
gen "Scorched Earth" Tileset=DESERT Players=10 Symmetry=2Rotations Size=142x142 \
	TerrainType=Plains Resources=Low Buildings=OilOnly CivilianDensity=None

# Snow, mountains, no tech buildings, no civilians. Bring your own economy.
gen "Frozen Siege" Tileset=SNOW Players=8 Symmetry=4Rotations Size=142x142 \
	TerrainType=Mountains Resources=Low Buildings=None CivilianDensity=None

# ============================================================================
# Economy. What there is to take, how much of it, and how hard it is to hold.
# ============================================================================

# --- Capture and hold. No ore at all: the derricks are the entire economy. ---

# Eight players, two teams, open ground. Whoever holds the derricks pays for the war.
gen "Oil War" Tileset=TEMPERAT Players=8 Symmetry=2Rotations Size=142x142 \
	TerrainType=Plains Resources=None Buildings=OilRush Density=AreaVeryHigh \
	CivilianDensity=Low

# Fourteen players, two teams of seven, desert. Nothing grows, nothing is free.
gen "Derrick Line" Tileset=DESERT Players=14 Symmetry=7Rotations Size=162x162 \
	TerrainType=Plains Resources=None Buildings=OilRush Density=AreaVeryHigh \
	CivilianDensity=None Roads=False

# --- King of the hill. A circular arena walled by mountains. ---

# Ten players thrown into one bowl. Thin ore, so the middle is worth taking.
gen "The Bowl" Tileset=TEMPERAT Players=10 Symmetry=5Rotations Size=142x142 \
	TerrainType=Rocky Shape=CircleMountain Resources=Low Buildings=Extra \
	Density=AreaHigh CivilianDensity=Medium

# Same idea in the snow, four corners, every tech building type in play.
gen "Winter Crown" Tileset=SNOW Players=12 Symmetry=4Rotations Size=162x162 \
	TerrainType=MountainLakes Shape=CircleMountain Resources=Low Buildings=Extra \
	Density=AreaVeryHigh CivilianDensity=High

# --- Rich. Long games, big armies, no excuse for losing. ---

# Everything turned up. Gems and ore everywhere, tech on every ridge.
gen "Boom Town" Tileset=TEMPERAT Players=12 Symmetry=2Rotations Size=162x162 \
	TerrainType=Lakes Resources=Full Buildings=Extra Density=AreaVeryHigh \
	CivilianDensity=VeryHigh

# Snow lakes, four corners, very high ore. Economy war rather than a rush.
gen "Frozen Vault" Tileset=SNOW Players=12 Symmetry=4Rotations Size=162x162 \
	TerrainType=Lakes Resources=VeryHigh Buildings=Extra Density=AreaHigh \
	CivilianDensity=High

# --- Town fighting. Civilian buildings everywhere, cover and crush. ---

# Parks and gardens at maximum civilian density. Infantry country.
gen "Suburbs" Tileset=TEMPERAT Players=10 Symmetry=2Rotations Size=142x142 \
	TerrainType=Parks Resources=Medium Buildings=Extra Density=AreaHigh \
	CivilianDensity=Max

# Three teams of four in overgrown woodland. You will not see them coming.
gen "Thicket" Tileset=TEMPERAT Players=12 Symmetry=3Rotations Size=162x162 \
	TerrainType=Overgrown Resources=Medium Buildings=Standard Density=AreaAndPlayers \
	CivilianDensity=High

# --- Wide open. Sixteen players and nowhere to hide. ---

# Puddles: flat, barely any water, room for everything. The big team game.
gen "Open Field" Tileset=TEMPERAT Players=16 Symmetry=2Rotations Size=162x162 \
	TerrainType=Puddles Resources=Medium Buildings=Standard Density=AreaAndPlayers \
	CivilianDensity=Medium

# Oceanic, ten players, real islands. Navy or nothing, and the ore is worth crossing for.
gen "Blue Water" Tileset=TEMPERAT Players=10 Symmetry=5Rotations Size=162x162 \
	TerrainType=Oceanic Resources=High Buildings=Extra Density=AreaHigh \
	CivilianDensity=Low

echo
echo "Done. $(find "$OUT" -name '*.oramap' | wc -l) maps in $OUT"
echo "Previews: $(find "$OUT" -name '*.png' | wc -l) PNGs alongside them."
