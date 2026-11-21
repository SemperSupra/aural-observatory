#!/usr/bin/env bash
set -euo pipefail
appdir="$HOME/.local/opt/aural-observatory"
desktop="${XDG_DATA_HOME:-$HOME/.local/share}/applications/org.sempersupra.auralobservatory.desktop"
if [[ "$appdir" != "$HOME/.local/opt/aural-observatory" || -z "$HOME" ]]; then
  echo 'Unsafe uninstall path; aborting' >&2; exit 2
fi
rm -f -- "$desktop"
if [[ -d "$appdir" ]]; then rm -rf -- "$appdir"; fi
echo 'Removed application binaries/desktop shortcut; user observations and preferences were intentionally retained.'
