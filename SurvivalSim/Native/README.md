# Native gathering-cost library

The library exports `calculate_gathering_cost` using the C ABI. The C# adapter imports it with `DllImport` and `CallingConvention.Cdecl`.

## Function contract

```c
int32_t calculate_gathering_cost(
    int32_t base_cost,
    int32_t tool_efficiency_percent,
    int32_t difficulty_percent);
```

All three inputs and the return value use fixed-width `int32_t`, matching C# `System.Int32` exactly. All inputs must be positive. The result is:

`ceil(base_cost * difficulty_percent / tool_efficiency_percent)`

Return values:
- positive integer: calculated cost
- `-1`: at least one invalid (non-positive) input
- `-2`: calculated result is outside the positive `int32_t` range

Manually checked example: `(10, 200, 150)` gives `ceil(10 * 150 / 200) = ceil(7.5) = 8`.

## Verified build platform: Linux x86_64

Verified environment: Debian GNU/Linux 13, x86_64 (`x86_64-linux-gnu`), GCC 14.2.0.

From the repository root, build both managed projects and then the native library:

```bash
dotnet build SurvivalSim/SurvivalSim.csproj
dotnet build SurvivalSim.Tests/SurvivalSim.Tests.csproj
chmod +x SurvivalSim/Native/build-native.sh
./SurvivalSim/Native/build-native.sh
```

The script uses this compiler command:

```bash
cc -std=c11 -O2 -Wall -Wextra -Werror -fPIC -shared \
  SurvivalSim/Native/survivalsim_native.c \
  -o SurvivalSim/bin/Debug/net8.0/libsurvivalsim_native.so
```

It copies the resulting `.so` beside the test executable as well. The `Configuration` argument can be `Release` instead of `Debug`.

## Windows x64 (MinGW-w64)

Prerequisites: .NET 8 SDK and 64-bit MinGW-w64 GCC on `PATH`. The compiler target must be `x86_64-w64-mingw32`.

From the repository root:

```powershell
dotnet build SurvivalSim/SurvivalSim.csproj
dotnet build SurvivalSim.Tests/SurvivalSim.Tests.csproj
powershell -ExecutionPolicy Bypass -File SurvivalSim/Native/build-native.ps1
```

The script creates `survivalsim_native.dll` beside both managed executables and statically links GCC's support runtime (`-static-libgcc`) to avoid a missing `libgcc_s_seh-1.dll` dependency. For Release builds, build both projects with `-c Release` and run the script with `-Configuration Release`.

The Linux x64 build was verified with GCC 14.2.0. The Windows script is included for the Windows target but must be run on the submission machine to verify its MinGW-w64 installation and DLL output.

## Interop and loading

- Logical library name in C#: `survivalsim_native`.
- Linux file name: `libsurvivalsim_native.so`.
- Windows file name: `survivalsim_native.dll`.
- Export name: `calculate_gathering_cost`.
- Calling convention: Cdecl.
- Architecture: x86_64 for the documented build scripts.
- The scripts place the library beside the application and test executables, where the .NET native loader can resolve it.

The managed adapter validates all domain inputs before crossing the interop boundary. It converts missing-library, missing-export, invalid-binary, and native error-code outcomes into explicit `Result<int>.Fail` values.

## Ownership and integration issue

The function accepts and returns integers only. It does not allocate native memory or return a pointer/handle, so there is no native buffer ownership or manual release step. The operating system releases the loaded module when the process exits.

An integration issue was ensuring the runtime could locate the native library after a clean build. Building only beside the C source does not place it in the managed application's probing location. The build scripts resolve this by copying the generated shared library into both managed output directories.
