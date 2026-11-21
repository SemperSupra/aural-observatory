#!/usr/bin/env bash
set -euo pipefail
# Explicit opt-in, non-root installer. Never overwrites or removes per-user research observations.
src="${1:-$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)}"
if [[ ! -x "$src/AuralObservatory.SearchDesktop" ]]; then
  printf 'Missing executable: %s/AuralObservatory.SearchDesktop\n' "$src" >&2; exit 2
fi
appdir="$HOME/.local/opt/aural-observatory"
appreg="${XDG_DATA_HOME:-$HOME/.local/share}/applications"
mkdir -p "$appdir" "$appreg"
cp -a "$src/." "$appdir/"
chmod u+x "$appdir/AuralObservatory.SearchDesktop"
desktop="$appreg/org.sempersupra.auralobservatory.desktop"
cat > "$desktop" <<EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=Aural Observatory
GenericName=Auditory research utility
Comment=Local-first experimental sound configuration and observations
Exec="$appdir/AuralObservatory.SearchDesktop"
TryExec=$appdir/AuralObservatory.SearchDesktop
Icon=audio-headphones
Terminal=false
Categories=Education;Science;AudioVideo;
StartupNotify=true
EOF
chmod 644 "$desktop"
printf 'Installed per-user launcher: %s\n' "$desktop"
printf 'Research observations in your OS user-data folder remain separate and are not changed.\n'
