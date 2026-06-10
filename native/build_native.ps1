# Build the TinyBVHNet native library for Windows (x64, arm64).
# Output: native\build\TinyBVHNetNative.dll      (x64)
#         native\build\TinyBVHNetNative-arm64.dll (arm64)
#
# Prerequisites: CMake 3.20+, MinGW-w64 (g++ with AVX2/FMA support)
#   Install via winget: winget install BrechtSanders.WinLibs.POSIX.UCRT
#   For arm64 cross-compilation, also install the aarch64 MinGW toolchain.
#
# For Linux/macOS builds, see build_native.sh

param(
    [string]$Configuration = "Release",
    [ValidateSet("x64", "arm64")]
    [string]$Architecture = "x64"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Join-Path $ScriptDir ".."
$NativeDir = $ScriptDir
$BuildDir = Join-Path $NativeDir "build"

Push-Location $NativeDir
try {
    Write-Host "=== Building TinyBVHNetNative ($Configuration, $Architecture) ===" -ForegroundColor Cyan

    if (-not (Test-Path $BuildDir)) {
        New-Item -ItemType Directory -Path $BuildDir -Force | Out-Null
    }

    Push-Location $BuildDir
    try {
        $generator = "MinGW Makefiles"
        $cmakeArgs = @("-G", $generator, "-DCMAKE_BUILD_TYPE=$Configuration", "..")

        if ($Architecture -eq "arm64") {
            $cmakeArgs += "-DCMAKE_CXX_COMPILER=aarch64-w64-mingw32-g++"
            $cmakeArgs += "-DCMAKE_C_COMPILER=aarch64-w64-mingw32-gcc"
        } else {
            $cmakeArgs += "-DCMAKE_CXX_COMPILER=g++"
        }

        # Configure
        & cmake @cmakeArgs
        if ($LASTEXITCODE -ne 0) { throw "CMake configure failed" }

        # Build
        & mingw32-make
        if ($LASTEXITCODE -ne 0) { throw "Build failed" }

        Write-Host ""
        Write-Host "=== Build complete ===" -ForegroundColor Green

        $dllName = if ($Architecture -eq "arm64") { "TinyBVHNetNative-arm64.dll" } else { "TinyBVHNetNative.dll" }
        $output = Join-Path $BuildDir $dllName
        if (Test-Path $output) {
            Write-Host "Output: $output" -ForegroundColor Green

            # Copy to the repo root build/ directory for NuGet packaging
            $pkgDir = Join-Path $RepoRoot "build"
            if (-not (Test-Path $pkgDir)) {
                New-Item -ItemType Directory -Path $pkgDir -Force | Out-Null
            }
            Copy-Item -Force $output $pkgDir
            Write-Host "Copied to: $(Join-Path $pkgDir $dllName)" -ForegroundColor Green
            Write-Host ""
            Write-Host "For Linux cross-compilation:"
            Write-Host "  Linux (via WSL):  run build_native.sh on the target system or in WSL"
            Write-Host "  macOS:             run build_native.sh on the target system"
        } else {
            Write-Host "WARNING: Expected output not found at $output" -ForegroundColor Yellow
            Get-ChildItem *.dll, *.so, *.dylib
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    Pop-Location
}
