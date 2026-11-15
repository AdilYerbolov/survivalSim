#!/usr/bin/env bash
set -euo pipefail
CONFIGURATION="${1:-Debug}"
if [[ "$(uname -m)" != "x86_64" ]]; then
  echo "This script targets Linux x86_64; detected $(uname -m)." >&2
  exit 1
fi
CC_TARGET="$(cc -dumpmachine)"
if [[ "$CC_TARGET" != x86_64* ]]; then
  echo "Expected an x86_64 C compiler target; detected $CC_TARGET." >&2
  exit 1
fi
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TESTS_ROOT="$(cd "$ROOT/.." && pwd)/SurvivalSim.Tests"
SOURCE="$ROOT/Native/survivalsim_native.c"
mkdir -p "$ROOT/bin/$CONFIGURATION/net8.0" "$TESTS_ROOT/bin/$CONFIGURATION/net8.0"
cc -std=c11 -O2 -Wall -Wextra -Werror -fPIC -shared "$SOURCE" \
  -o "$ROOT/bin/$CONFIGURATION/net8.0/libsurvivalsim_native.so"
cp "$ROOT/bin/$CONFIGURATION/net8.0/libsurvivalsim_native.so" \
  "$TESTS_ROOT/bin/$CONFIGURATION/net8.0/"
echo "Built native library for Linux x64."
