#!/bin/bash
# Build the TinyBVHNet native library for Linux or macOS (x64, arm64).
# Output:
#   Linux x64:   native/build/libTinyBVHNetNative.so       → build/libTinyBVHNetNative.so
#   Linux arm64: native/build/libTinyBVHNetNative-arm64.so  → build/libTinyBVHNetNative-arm64.so
#
# Prerequisites: CMake 3.20+, GCC/Clang with AVX2+FMA support
#
# Cross-compiling from Windows:
#   Use WSL (Windows Subsystem for Linux) to build the Linux .so.
#   For macOS, build on macOS directly.

set -e

CONFIG="${1:-Release}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
BUILD_DIR="$SCRIPT_DIR/build"

echo "=== Building TinyBVHNetNative ($CONFIG, ${ARCH:-x64}) ==="
echo "Build directory: $BUILD_DIR"

mkdir -p "$BUILD_DIR"
cd "$BUILD_DIR"

# Detect if cross-compiling for ARM64
if [ "${CROSS_COMPILE:-}" = "aarch64" ] || [ "${ARCH:-}" = "arm64" ]; then
    COMPILER_PREFIX="aarch64-linux-gnu-"
    DLL_SUFFIX="-arm64"
    echo "Cross-compiling for Linux ARM64"
else
    COMPILER_PREFIX=""
    DLL_SUFFIX=""
fi

cmake .. \
    -DCMAKE_BUILD_TYPE="$CONFIG" \
    -DCMAKE_CXX_COMPILER="${COMPILER_PREFIX}g++" \
    -DCMAKE_C_COMPILER="${COMPILER_PREFIX}gcc"

cmake --build . --config "$CONFIG" -j$(nproc 2>/dev/null || echo 4)

echo ""
echo "=== Build complete ==="

# Determine output path
OS_NAME=$(uname -s)
if [ "$OS_NAME" = "Linux" ]; then
    OUTPUT="$BUILD_DIR/libTinyBVHNetNative${DLL_SUFFIX}.so"
elif [ "$OS_NAME" = "Darwin" ]; then
    OUTPUT="$BUILD_DIR/libTinyBVHNetNative${DLL_SUFFIX}.dylib"
fi

if [ -f "$OUTPUT" ]; then
    echo "Output: $OUTPUT"

    # Copy to repo root build/ directory for NuGet packaging
    PKG_DIR="$REPO_ROOT/build"
    mkdir -p "$PKG_DIR"
    cp "$OUTPUT" "$PKG_DIR/$(basename "$OUTPUT")"
    echo "Copied to: $PKG_DIR/$(basename "$OUTPUT")"
else
    echo "WARNING: Expected output not found at $OUTPUT"
    ls -la *.so *.dylib 2>/dev/null || true
fi
