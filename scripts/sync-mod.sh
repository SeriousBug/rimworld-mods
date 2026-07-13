#!/usr/bin/env bash
# Copy a mod from this repo into the game's Mods/ directory.
#   sync-mod.sh <ModName> [--watch]
#
# Deletes files at the destination that no longer exist in the source, so the guards below
# refuse to touch anything that is not an already-recognisable mod folder inside Mods/.
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/rimworld-env.sh"
rw_require_install

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MOD="${1:-}"
[ -n "$MOD" ] || { echo "usage: sync-mod.sh <ModName> [--watch]" >&2; exit 1; }

SRC="$REPO/mods/$MOD"
DEST="$RIMWORLD_MODS/$MOD"

[ -f "$SRC/About/About.xml" ] || { echo "Not a mod: $SRC (no About/About.xml)" >&2; exit 1; }

# --delete is destructive, so refuse any destination that is not inside Mods/ and is not
# either absent or already a mod folder we would recognise.
case "$DEST" in
  "$RIMWORLD_MODS"/*) ;;
  *) echo "Refusing: destination escapes $RIMWORLD_MODS" >&2; exit 1 ;;
esac
if [ -e "$DEST" ] && [ ! -f "$DEST/About/About.xml" ]; then
  echo "Refusing: $DEST exists but has no About/About.xml. Not overwriting an unknown directory." >&2
  exit 1
fi

sync_once() {
  rsync -a --delete \
    --exclude 'Source/' --exclude '.git/' --exclude '.DS_Store' \
    --exclude 'bin/' --exclude 'obj/' \
    "$SRC/" "$DEST/"
  echo "$(date +%H:%M:%S) synced $MOD -> $DEST"
}

sync_once

if [ "${2:-}" = "--watch" ]; then
  command -v fswatch >/dev/null || { echo "--watch needs fswatch (brew install fswatch)" >&2; exit 1; }
  echo "Watching $SRC ..."
  fswatch -o "$SRC" | while read -r _; do sync_once; done
fi
