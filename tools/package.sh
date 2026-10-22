#!/bin/sh
# Build the plugin and the Thunderstore zip: dist/Jatify-AdminHammer-<version>.zip
set -e
R="$(cd "$(dirname "$0")/.." && pwd)"
V=$(python3 -c "import json;print(json.load(open('$R/package/manifest.json'))['version_number'])")
dotnet build "$R/AdminHammer.csproj" -c Release -v minimal -o "$R/build" "$@" >/dev/null
S=$(mktemp -d); trap 'rm -rf "$S"' EXIT
mkdir -p "$S/plugins" "$R/dist"
cp "$R/package/manifest.json" "$R/package/icon.png" "$R/package/README.md" "$R/package/CHANGELOG.md" "$S/"
cp "$R/build/AdminHammer.dll" "$S/plugins/"
rm -f "$R/dist/Jatify-AdminHammer-$V.zip"
(cd "$S" && zip -qr -X "$R/dist/Jatify-AdminHammer-$V.zip" .)
echo "$R/dist/Jatify-AdminHammer-$V.zip"
