param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $PSScriptRoot "survivalsim_native.c"
$header = Join-Path $PSScriptRoot "survivalsim_native.h"

$gcc = Get-Command gcc -ErrorAction SilentlyContinue
if (-not $gcc) {
    throw "gcc was not found. Install MinGW-w64 and ensure gcc is on PATH."
}

$targetTriple = (& $gcc.Source -dumpmachine).Trim()
if (-not $targetTriple.StartsWith("x86_64-w64-mingw32", [StringComparison]::OrdinalIgnoreCase)) {
    throw "Expected 64-bit MinGW-w64 GCC (x86_64-w64-mingw32), but gcc targets '$targetTriple'."
}
Write-Host "Using GCC target $targetTriple"
& $gcc.Source --version | Select-Object -First 1

$managedOutput = Join-Path $projectRoot "bin\$Configuration\net8.0"
$testsOutput = Join-Path (Split-Path -Parent $projectRoot) "SurvivalSim.Tests\bin\$Configuration\net8.0"
New-Item -ItemType Directory -Force -Path $managedOutput, $testsOutput | Out-Null

$targets = @($managedOutput, $testsOutput)
foreach ($target in $targets) {
    $dll = Join-Path $target "survivalsim_native.dll"
    # Statically link GCC's support runtime. Without this, Windows may find
    # survivalsim_native.dll but fail to load it because libgcc_s_seh-1.dll
    # (or another MinGW runtime dependency) is not on PATH.
    & $gcc.Source -std=c11 -O2 -Wall -Wextra -Werror -shared -static-libgcc $source -o $dll
    if ($LASTEXITCODE -ne 0) { throw "Native compilation failed." }
    Write-Host "Built $dll"
}
