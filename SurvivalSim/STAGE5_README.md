# Stage 5 — Currying, lazy evaluation, and native C

Stage 5 integrates Assignment 8 concepts into the gathering workflow.

## 1. Curried gathering policy

`GameLogic/World/GatheringCostPolicy.cs` exposes:

```csharp
Func<ObjectDefinition, Result<int>> Create(GatheringConfig config)
```

The configuration is captured by the returned function. The resulting function accepts an `ObjectDefinition`, derives a difficulty percentage from its maximum durability, and asks the native calculator for the cost.

This is a curried shape:

`GatheringConfig -> (ObjectDefinition -> Result<int>)`

`InteractionService.Strike` uses the policy during actual gameplay. It chooses a tool-efficiency configuration based on whether the object requires a tool. The same object can therefore have different calculated costs under different configurations.

## 2. Deferred execution

`WorldPosition.GetVisiblePositions` is an iterator implemented with `yield return`. Calling it creates an enumerable; coordinates are produced only as the caller requests them. `GetNearbyObjects` returns a deferred LINQ query over the object's values.

Use `ToArray()` or `ToList()` when a stable snapshot is needed, such as when a result will be enumerated more than once. Re-enumerating an iterator repeats its work. Do not put file reads, state mutations, or expensive native calls inside a deferred query.

Assignment 8 tests wrap the actual `GetVisiblePositions` iterator in a test-only probe. They prove that creating the enumerable performs no iteration, `Take(2)` produces only two positions, a `ToArray` snapshot can be reused without rerunning the source, and re-enumerating the original deferred sequence runs it again.

## 3. Native C library

Source:
- `Native/survivalsim_native.c`
- `Native/survivalsim_native.h`

The exported C ABI function is:

```c
int calculate_gathering_cost(
    int base_cost,
    int tool_efficiency_percent,
    int difficulty_percent);
```

It computes the ceiling of `base_cost * difficulty / tool_efficiency`. It uses 64-bit intermediate arithmetic and returns `-1` for invalid inputs or `-2` on overflow.

C# imports it with `DllImport` and `CallingConvention.Cdecl` in `NativeGatheringCalculator`. The adapter validates arguments before calling native code and converts missing DLL, missing entry point, invalid binary, and native error returns into `Result<int>.Fail` values.

## 4. Build and run (Windows)

Requirements: .NET 8 SDK and MinGW-w64 GCC available as `gcc` on PATH.

From the repository root:

```powershell
dotnet build SurvivalSim/SurvivalSim.csproj
dotnet build SurvivalSim.Tests/SurvivalSim.Tests.csproj
powershell -ExecutionPolicy Bypass -File SurvivalSim/Native/build-native.ps1
dotnet run --project SurvivalSim.Tests/SurvivalSim.Tests.csproj --no-build
dotnet run --project SurvivalSim/SurvivalSim.csproj --no-build
```

The native build script copies the DLL beside both executables. If you clean or rebuild into a different configuration, rerun the native build script with the matching configuration:

```powershell
powershell -ExecutionPolicy Bypass -File SurvivalSim/Native/build-native.ps1 -Configuration Release
```

Linux build instructions are in `Native/README.md`.

## 5. Resource ownership

The managed wrapper does not manually allocate or retain native memory. The P/Invoke runtime loads the shared library for the process; the operating system releases the module when the process exits. The native function receives only integer values and returns an integer, so there are no native buffers or handles for the caller to release.

File operations remain scoped to their service methods. `File.ReadAllText` and `File.WriteAllText` own and close the underlying file handles before returning or throwing. The lazy world iterators do not own external resources.

## 6. Stage 5 test coverage

- Native calculation success.
- Invalid managed input rejected before native invocation.
- Two curried configurations produce exact gathering costs for the same oak tree: 11 at 100% efficiency and 6 at 200% efficiency (base cost 10, derived difficulty 110%).
- `yield return` deferred execution and repeated enumeration.
- Native overflow is mapped to a controlled `Result` failure.
- A controlled `DllNotFoundException` injected at the adapter boundary maps to `NATIVE_LIBRARY_UNAVAILABLE` without relying on deleting a library that may already be loaded.

The native-dependent tests and all existing Stage 3 strike tests require the native library to be built and present beside the test executable.
