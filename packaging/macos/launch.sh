#!/bin/sh
set -eu
# Finder launches this thin, shared-core desktop browser host. Not a WKWebView/widget fork.
APP_RESOURCES=$(CDPATH= cd -- "$(dirname -- "$0")/../Resources" && pwd)
exec "$APP_RESOURCES/runtime/AuralObservatory.SearchDesktop" "$@"
