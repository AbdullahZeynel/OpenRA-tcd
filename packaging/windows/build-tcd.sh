#!/bin/bash
# Build a self-contained Red Alert TCD Windows x64 archive from a release tag.
set -euo pipefail

if [ "$#" -ne 2 ] || [[ "$1" != tcd-* ]]; then
	echo "Usage: $(basename "$0") tcd-x.y.z outputdir" >&2
	exit 1
fi

for tool in git dotnet python3 zip tar; do
	command -v "$tool" >/dev/null || { echo "Missing tool: $tool" >&2; exit 1; }
done

SRCDIR=$(cd "$(dirname "$0")/../.." && pwd)
TAG="$1"
COMMIT=$(git -C "$SRCDIR" rev-parse --verify "refs/tags/${TAG}^{commit}")
mkdir -p "$2"
OUTPUTDIR=$(cd "$2" && pwd)
ARCHIVE="OpenRA-TCD-${TAG}-win-x64.zip"
if [ -e "${OUTPUTDIR}/${ARCHIVE}" ]; then
	echo "Output already exists: ${OUTPUTDIR}/${ARCHIVE}" >&2
	exit 1
fi

WORKDIR=$(mktemp -d)
trap 'rm -rf "$WORKDIR"' EXIT
SOURCE="${WORKDIR}/source"
PACKAGE="${WORKDIR}/package"
mkdir -p "$SOURCE" "$PACKAGE"

# Export the tag, never the working tree: a release must retain its game rules.
git -C "$SRCDIR" archive "$COMMIT" | tar -x -C "$SOURCE"
. "${SOURCE}/packaging/functions.sh"
VERSION="release-${TAG}"
# Upstream's POSIX helpers terminate argument loops by reading an unset $1.
set +u
install_assemblies "$SOURCE" "$PACKAGE" win-x64 False True False
install_data "$SOURCE" "$PACKAGE" ra
set_engine_version "$VERSION" "$PACKAGE"
set_mod_version "$VERSION" "$PACKAGE/mods/ra/mod.yaml" "$PACKAGE/mods/ra-content/mod.yaml"
install_windows_launcher "$SOURCE" "$PACKAGE" win-x64 ra RedAlert \
	"Tactics & Command Dynamics" "https://github.com/AbdullahZeynel/OpenRA-tcd" "$VERSION"
set -u

# Catch a missing mod assembly or incomplete self-contained publish before zipping.
for file in RedAlert.exe OpenRA.Mods.Tcd.dll coreclr.dll SDL2.dll; do
	test -s "$PACKAGE/$file"
done
printf 'Source tag: %s\nSource commit: %s\nEngine version: %s\n' \
	"$TAG" "$COMMIT" "$VERSION" > "$PACKAGE/BUILD-INFO.txt"
printf 'Extract the entire ZIP, then run RedAlert.exe.\r\nGame assets download on first launch.\r\n' \
	> "$PACKAGE/START-HERE.txt"
(cd "$PACKAGE" && zip -q -r "$WORKDIR/$ARCHIVE" .)
mv "$WORKDIR/$ARCHIVE" "$OUTPUTDIR/$ARCHIVE"
echo "Created $OUTPUTDIR/$ARCHIVE"
