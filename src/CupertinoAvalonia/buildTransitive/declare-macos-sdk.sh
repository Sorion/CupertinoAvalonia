#!/bin/sh
# Declares a newer macOS SDK in an executable's LC_BUILD_VERSION, keeping its minimum macOS version, then
# signs it again ad hoc (editing the binary breaks its signature, and Apple silicon won't run it unsigned).
# Usage: declare-macos-sdk.sh <executable> <sdk version>
set -e
executable="$1"
sdk="$2"

if vtool -show-build "$executable" | grep -q "sdk $sdk\$"; then
  exit 0
fi

minos=`vtool -show-build "$executable" | awk '/minos/ { print $2; exit }'`
unsigned="$executable.unsigned"
declared="$executable.sdk"
trap 'rm -f "$unsigned" "$declared"' EXIT

cp "$executable" "$unsigned"
codesign --remove-signature "$unsigned"
vtool -set-build-version macos "$minos" "$sdk" -replace -output "$declared" "$unsigned"
codesign --force --sign - "$declared"
mv "$declared" "$executable"
