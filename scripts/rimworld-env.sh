#!/usr/bin/env bash
# Shared paths. Source this; do not execute it.
set -euo pipefail

RIMWORLD_APP="${RIMWORLD_APP:-$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app}"

# The app bundle has two "Data" directories and they hold different things.
RIMWORLD_MANAGED="$RIMWORLD_APP/Contents/Resources/Data/Managed"   # DLLs
RIMWORLD_DATA="$RIMWORLD_APP/Data"                                 # Defs, Textures
RIMWORLD_MODS="$RIMWORLD_APP/Mods"
RIMWORLD_VERSION_FILE="$RIMWORLD_APP/Version.txt"

# Belongs to Ludeon. ~/Library/Logs/Unity/Player.log is a different game entirely.
PLAYER_LOG="$HOME/Library/Logs/Ludeon Studios/RimWorld by Ludeon Studios/Player.log"

REF="${RIMWORLD_REF:-$HOME/rimref}"
REF_SRC="$REF/src"
REF_VERSION_FILE="$REF/.version"

rw_require_install() {
  if [ ! -f "$RIMWORLD_VERSION_FILE" ]; then
    echo "RimWorld not found at: $RIMWORLD_APP" >&2
    echo "Set RIMWORLD_APP to the app bundle path." >&2
    exit 1
  fi
}
