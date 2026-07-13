#!/usr/bin/env bash
# Show what the game said. A change is not "working" until this file says so.
#   tail-log.sh              errors and exceptions only
#   tail-log.sh <pattern>    that pattern, plus errors
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/rimworld-env.sh"

[ -f "$PLAYER_LOG" ] || { echo "No Player.log at: $PLAYER_LOG (has the game run?)" >&2; exit 1; }

PATTERN='Exception|Error|error|Could not|Failed|XML error'
[ -n "${1:-}" ] && PATTERN="$1|$PATTERN"

echo "--- $PLAYER_LOG (modified $(date -r "$PLAYER_LOG" '+%Y-%m-%d %H:%M:%S'))"
rg -n --color always "$PATTERN" "$PLAYER_LOG" || echo "(no matches)"
