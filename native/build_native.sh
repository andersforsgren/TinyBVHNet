#!/bin/bash
# Build the TinyBVHNet native library for Linux or macOS (x64).
# Output: native/build/libTinyBVHNetNative.so (Linux)
#         native/build/libTinyBVHNetNative.dylib (macOS)
#
# Prerequisites: CMake 3.20+, GCC/Clang (Linux) or Xcode CLT (macOS)
#
# Cross-compiling from Windows:
#   Use WSL (Windows Subsystem for Linux) to build the Linux .so.
#   For macOS, build on a Mac directly (no supported cross-compilation from Windows).

set -e

BUILD_DIR="$(cd "$(dirname "$0")" && pwd)/native/build"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)/native"

echo "=== Building TinyBVHNetNative ==="
echo "Build directory: $BUILD_DIR"

mkdir -p "$BUILD_DIR"
cd "$BUILD_DIR"

# Detect OS
OS_NAME=$(uname -s)

cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --config Release -j$(nproc 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo 4)

echo ""
echo "=== Build complete ==="

if [ "$OS_NAME" = "Linux" ]; then
    OUTPUT="$BUILD_DIR/libTinyBVHNetNative.so"
elif [ "$OS_NAME" = "Darwin" ]; then
    OUTPUT="$BUILD_DIR/libTinyBVHNetNative.dylib"
fi

if [ -f "$OUTPUT" ]; then
    echo "Output: $OUTPUT"
    echo ""
    echo "To pack into NuGet, copy to the repo root build/ directory:"
    echo "  cp $OUTPUT ../../build/\$(basename $OUTPUT)"
else
    echo "WARNING: Expected output not found at $OUTPUT"
fi
