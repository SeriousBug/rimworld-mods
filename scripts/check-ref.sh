#!/usr/bin/env bash
# SessionStart hook. Stdout is injected into the agent's context.
# Fast: compares two version strings. Never decompiles.
set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/rimworld-env.sh" 2>/dev/null || exit 0

if [ ! -f "$RIMWORLD_VERSION_FILE" ]; then
  echo "RimWorld reference: game not found at $RIMWORLD_APP. Set RIMWORLD_APP."
  exit 0
fi

GAME_VERSION="$(cat "$RIMWORLD_VERSION_FILE")"
REF_VERSION="$(cat "$REF_VERSION_FILE" 2>/dev/null || echo none)"

if [ "$GAME_VERSION" = "$REF_VERSION" ]; then
  echo "RimWorld reference tree is current for game $GAME_VERSION (decompiled C# at $REF_SRC, indexes at $REF/defnames.txt)."
else
  echo "STALE RimWorld reference tree: game is $GAME_VERSION, decompiled tree is $REF_VERSION."
  echo "Run scripts/refresh-ref.sh before trusting anything in $REF. Do not verify field names against the stale tree."
fi
