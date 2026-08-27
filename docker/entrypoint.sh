#!/bin/sh
# Fetch the Red Alert content on first start, then hand over to the upstream
# dedicated-server script.
#
# mods/ra/mod.yaml lists allies.mix, conquer.mix and the rest under
# ContentPackages, so the server will not load without them. They are Westwood's
# files and are never committed here. OpenRA publishes a freeware package and a
# mirror list; this is the same download the in-game installer performs, and the
# checksum below is the one recorded in mods/ra-content/installer/downloads.yaml.

set -o errexit

SUPPORT_DIR="${SupportDir:-/data/}"
CONTENT_DIR="${SUPPORT_DIR}Content/ra/v2"
MIRROR_LIST="https://www.openra.net/packages/ra-quickinstall-mirrors.txt"
QUICKINSTALL_SHA1="44241f68e69db9511db82cf83c174737ccda300b"

if [ ! -f "${CONTENT_DIR}/allies.mix" ]; then
	echo "Red Alert content is missing. Fetching the freeware package."
	mkdir -p "${CONTENT_DIR}"

	tmp=$(mktemp -d)
	trap 'rm -rf "${tmp}"' EXIT

	url=$(curl -fsSL --retry 3 "${MIRROR_LIST}" | tr -d '\r' | head -n 1)
	if [ -z "${url}" ]; then
		echo "Could not read the mirror list at ${MIRROR_LIST}." >&2
		exit 1
	fi

	echo "  ${url}"
	curl -fSL --retry 3 -o "${tmp}/ra.zip" "${url}"

	echo "${QUICKINSTALL_SHA1}  ${tmp}/ra.zip" | sha1sum -c - >/dev/null || {
		echo "Checksum mismatch. Refusing to install." >&2
		exit 1
	}

	unzip -qo "${tmp}/ra.zip" -d "${CONTENT_DIR}"
	rm -rf "${tmp}"
	trap - EXIT
	echo "Content installed into ${CONTENT_DIR}"
else
	echo "Red Alert content found in ${CONTENT_DIR}"
fi

export SupportDir="${SUPPORT_DIR}"

# launch-dedicated.sh restarts the server after each game, which is what keeps a
# lobby alive. It does not forward signals, so give the container a short stop
# grace period rather than expecting a clean shutdown.
exec ./launch-dedicated.sh
