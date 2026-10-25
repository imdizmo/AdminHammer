#!/usr/bin/env bash
# Build and drop the DLL into the dev profile's ScriptEngine folder: the running game reloads it about a second later
# (all scripts reload together). The .pdb goes first (ScriptEngine reads symbols), the DLL by temp name + rename so the
# watcher never reads a half-written file.
set -euo pipefail
R="$(cd "$(dirname "$0")/.." && pwd)"
VALHEIM="${VALHEIM:-/mnt/games/SteamLibrary/steamapps/common/Valheim}"
PROFILE="${PROFILE:-$HOME/.local/share/com.kesomannen.gale/valheim/profiles/jatify-ui-dev}"
DEST="$PROFILE/BepInEx/scripts"
dotnet build "$R/AdminHammer.csproj" -c Release -v minimal -o "$R/build" -p:ValheimDir="$VALHEIM" -p:ProfileDir="$PROFILE" | grep -E "error|warn|Build succeeded" | sort -u
mkdir -p "$DEST"
cp "$R/build/AdminHammer.pdb" "$DEST/.AdminHammer.pdb.tmp" && mv -f "$DEST/.AdminHammer.pdb.tmp" "$DEST/AdminHammer.pdb"
cp "$R/build/AdminHammer.dll" "$DEST/.AdminHammer.tmp" && mv -f "$DEST/.AdminHammer.tmp" "$DEST/AdminHammer.dll"
echo "dropped into $DEST (reloads in-game if the dev profile is running)"
