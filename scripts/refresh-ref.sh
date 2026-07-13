#!/usr/bin/env bash
# Decompile Assembly-CSharp.dll and rebuild the Def indexes.
# No-op when the reference tree already matches the installed game version.
#   refresh-ref.sh [--force]
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/rimworld-env.sh"
rw_require_install

GAME_VERSION="$(cat "$RIMWORLD_VERSION_FILE")"
REF_VERSION="$(cat "$REF_VERSION_FILE" 2>/dev/null || echo none)"

if [ "${1:-}" != "--force" ] && [ "$GAME_VERSION" = "$REF_VERSION" ] && [ -d "$REF_SRC" ]; then
  echo "Reference tree is current ($GAME_VERSION). Use --force to rebuild."
  exit 0
fi

export PATH="$PATH:$HOME/.dotnet/tools"
command -v ilspycmd >/dev/null || { echo "ilspycmd not on PATH. dotnet tool install -g ilspycmd" >&2; exit 1; }
command -v rg >/dev/null || { echo "ripgrep (rg) not on PATH." >&2; exit 1; }

echo "Game $GAME_VERSION, reference $REF_VERSION. Rebuilding $REF ..."
mkdir -p "$REF"
rm -rf "$REF_SRC"
ilspycmd -p -o "$REF_SRC" "$RIMWORLD_MANAGED/Assembly-CSharp.dll"

rg --no-heading -H -o '<defName>[^<]+</defName>' "$RIMWORLD_DATA" -g '*.xml' > "$REF/defnames.txt"
rg --no-heading -H -o 'Name="[^"]+"' "$RIMWORLD_DATA" -g '*.xml' > "$REF/defparents.txt"

echo "$GAME_VERSION" > "$REF_VERSION_FILE"
echo "Done. $(find "$REF_SRC" -name '*.cs' | wc -l | tr -d ' ') C# files, $(wc -l < "$REF/defnames.txt" | tr -d ' ') defNames."
