# Build the TinyBVHNet native library for Windows (x64).
# Output: native\build\TinyBVHNetNative.dll
#
# Prerequisites: CMake 3.20+, Visual Studio 2022 (with C++ desktop workload)
#
# For Linux/Mac builds, see build_native.sh

param(
    [string]$Configuration = "Release",
    [string]$Architecture = "x64"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$NativeDir = Join-Path $ScriptDir "..\native"
$BuildDir = Join-Path $NativeDir "build"

Push-Location $NativeDir
try {
    Write-Host "=== Building TinyBVHNetNative ($Configuration, $Architecture) ===" -ForegroundColor Cyan

    if (-not (Test-Path $BuildDir)) {
        New-Item -ItemType Directory -Path $BuildDir -Force | Out-Null
    }

    Push-Location $BuildDir
    try {
        # Configure (MSVC generator)
        cmake .. -G "Visual Studio 17 2022" -A $Architecture

        # Build
        cmake --build . --config $Configuration

        Write-Host ""
        Write-Host "=== Build complete ===" -ForegroundColor Green

        $output = Join-Path $BuildDir $Configuration "TinyBVHNetNative.dll"
        if (Test-Path $output) {
            Write-Host "Output: $output" -ForegroundColor Green
            Write-Host ""
            Write-Host "To pack into NuGet, place the DLL in the repo root build/ directory:"
            Write-Host "  copy $output ..\..\..\..\build\TinyBVHNetNative.dll"
            Write-Host ""
            Write-Host "For Linux/macOS cross-compilation:"
            Write-Host "  Linux (via WSL):  run build_native.sh on the target system or in WSL"
            Write-Host "  macOS:             run build_native.sh on the target system"
        } else {
            Write-Host "WARNING: Expected output not found at $output" -ForegroundColor Yellow
            Write-Host "Check the build/ directory for the actual output location." -ForegroundColor Yellow
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    Pop-Location
}
