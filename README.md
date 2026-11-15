# SurvivalSim

SurvivalSim is a turn-based console survival simulator developed for an Introduction to Functional Programming project. It combines the action-processing and crafting workflows from Assignments 5–7 with an explorable multi-level tile world, resource gathering, JSON persistence, LINQ reports, curried policies, and native C integration.

The game state contains a player's energy, hunger, thirst, elapsed time, and inventory. An action consumes time and energy and reduces hunger and thirst according to a selected survival policy. Crafting looks up a recipe, validates its ingredients and parameters, executes the crafting action, consumes ingredients, and adds the result item.

The current executable provides a keyboard-driven world exploration loop with movement, stairs, an item-selection pickup menu, inventory, crafting, building, inspection, object gathering, saving/loading, and report export. It remains a prototype: procedural chunk generation, a technology progression system, and a graphical interface are not implemented.

## Features currently implemented

- Immutable record-based models for players, game state, actions, effects, outcomes, recipes, and item stacks.
- Action validation through a private-constructor `GameAction` factory.
- Batch execution with Normal and Harsh hunger/thirst policies.
- Fatigue: an action takes an additional half-duration when starting energy is below 30.
- Energy, hunger, and thirst clamped to the range 0–100.
- Explicit validation, success, and failure cases.
- Recipe lookup with `Option<T>` and required-recipe conversion to `Result<T>`.
- JSON recipe loading with validation and explicit missing-file, malformed-JSON, and other I/O failures.
- Crafting with ingredient aggregation, inventory transformation, action costs, and state preservation on failure.
- Interactive keyboard-driven crafting and building menus.
- Nearby ground-item pickup menu: use arrow keys or W/S to select, Enter to collect one stack, A to collect all, and Esc to close.
- A deterministic console test runner with 76 registered tests covering Assignments 5–8 and the integrated world stages.

Crafting and building are exposed through keyboard-driven menus in `Program.cs`. Pickup uses the existing `InteractionService.Pickup` transition and presents ground items as a selectable list rather than requiring item IDs.

## Project structure

```text
.
├── README.md
├── SurvivalSim/
│   ├── Data/Definitions/
│   ├── GameLogic/World/
│   ├── GameLogic/Native/
│   ├── Models/World/
│   ├── Native/
│   ├── Reports/
│   ├── UI/
│   ├── Program.cs
│   └── SurvivalSim.csproj
└── SurvivalSim.Tests/
    ├── Assignment6Tests.cs
    ├── Assignment7Tests.cs
    ├── Stage8Tests.cs
    ├── Program.cs
    └── SurvivalSim.Tests.csproj
```

`Models.cs` defines `Player`, `GameState`, `GameAction`, `ActionRequest`, `ActionEffects`, `ActionOutcome`, `BatchResult`, `ActionValidation`, `Result<T>`, and `Option<T>`. `CraftingRecipe.cs` defines `Item`, `ItemStack`, and `CraftRecipe`. `Main.cs` contains the action pipeline. The other `GameLogic` files separate recipe lookup, validation, inventory operations, result composition, and the higher-level crafting service. `Program.cs` is the application boundary for file loading and console output.

## Requirements and build

- .NET SDK 8 or newer.
- The projects target `net8.0`; the required SDK family is .NET 8. Later SDKs can also build this target. Record the exact `dotnet --version` output from the submission machine.
- No third-party NuGet packages are declared. The code uses .NET libraries such as `System.Text.Json` and `System.Collections.Immutable`.

From the repository root:

```powershell
dotnet --version
dotnet build SurvivalSim/SurvivalSim.csproj
dotnet run --project SurvivalSim/SurvivalSim.csproj
dotnet run --project SurvivalSim.Tests/SurvivalSim.Tests.csproj
```

The project targets .NET 8 and copies all JSON files under `SurvivalSim/Data/` to the application output directory.

### Windows native DLL setup

The native gathering calculator is a required C integration. On Windows x64, install **64-bit MinGW-w64** and ensure `gcc` is on `PATH`. From the repository root, run:

```powershell
dotnet build SurvivalSim/SurvivalSim.csproj
dotnet build SurvivalSim.Tests/SurvivalSim.Tests.csproj
powershell -ExecutionPolicy Bypass -File SurvivalSim/Native/build-native.ps1 -Configuration Debug
```

The script builds `survivalsim_native.dll` beside both executables. It uses `-static-libgcc` so the generated DLL does not depend on a separately installed MinGW GCC runtime. For Release, build both projects with `-c Release` and pass `-Configuration Release` to the script.

You can also opt in to running the native build after a Windows project build:

```powershell
dotnet build SurvivalSim/SurvivalSim.csproj -p:SurvivalSimBuildNative=true
```

This opt-in target requires MinGW-w64 GCC. If the DLL or a dependency is missing, the native adapter returns `NATIVE_LIBRARY_UNAVAILABLE` with the build instructions instead of allowing an unhandled native-loading exception to escape.

The C# build and test suite have not been executed in this environment because the .NET SDK is unavailable here. Run the clean verification commands below on the target machine and record the actual result before submission.

## Keyboard controls

- `WASD` / arrow keys: move; `,` and `.`: climb stairs.
- `G`: open the ground-item pickup menu. Use Up/Down or W/S, Enter to collect the selected stack, A to collect all, and Esc to close.
- `V`: inventory; `P`: drop by item ID and quantity.
- `C`: crafting; `B`: building; `X`: strike/gather; `I`: inspect; `N`: nearby objects.
- `F5`: save; `F6`: load; `F7`: export reports; `Q`: quit.

The pickup menu lists the items on the tile occupied by the player. It uses `InteractionService.Pickup`, so the world tile and inventory are updated through the same validated state transition as service-level pickup.

## Usage and domain rules

`SurvivalSim/Data/actions.json` contains the sample requests:

| Request | Duration | Energy cost | Expected purpose |
|---|---:|---:|---|
| `Walk` | 20 | 5 | Normal successful action |
| `Gather sticks` | 40 | 10 | Shows survival-policy effects |
| `Collect stones` | 1 | 1 | Positive-duration boundary |
| `Chop wood` | 40 | 95 | Eventually fails because of energy |
| `Invalid backward action` | -10 | 3 | Invalid duration |
| `Rest` | 60 | 0 | Zero-cost action |

`Game.ExecuteBatch` processes requests in order. `GameAction.Create` rejects null input, a missing name, a non-positive duration, or an energy cost outside 0–100. A valid action is prepared, checked against current energy, converted to effects, and applied as a new state. A rejected action returns the state it received, so later requests continue from the last successful state.

Normal policies are `minutes / 20` hunger and `minutes / 15` thirst. Harsh policies are `minutes / 10` hunger and `minutes / 8` thirst. For example, a fresh state executing a 40-minute action costing 10 energy becomes `(energy=90, hunger=98, thirst=98, time=520)` under Normal policy and `(90, 96, 95, 520)` under Harsh policy.

## Architecture and functional design

The central types are:

- `GameState` contains the `Player`, elapsed time, and inventory.
- `Player` contains energy, hunger, and thirst.
- `ActionRequest` is unvalidated external input; `GameAction` is constructed only after validation.
- `ActionOutcome` reports success, the resulting state, and a message; `BatchResult` contains all outcomes and the final state.
- `CraftRecipe` describes an output item, quantity, ingredients, duration, and energy cost.
- `CraftingService` composes recipe lookup, validation, action execution, and inventory transformation.

The action pipeline in `Main.cs` is split into deterministic transformations: `PrepareAction`, `CalculateEffects`, and `ApplyEffects`. Records and `with` expressions produce new values rather than mutating the caller's state. The survival policies are functions passed into the pipeline, so the same workflow supports Normal and Harsh rules.

Assignment 7 adds explicit sum-like cases. `Result<T>.Success` carries a value and `Result<T>.Fail` carries a code and message. `Option<T>.Some` represents a found recipe and `Option<T>.None` represents a missing recipe. `Map` transforms only a successful result; `Bind` invokes the next computation only for a successful result. This keeps expected domain failures in the value flow and avoids exceptions for ordinary invalid input.

## Assignment 5 — function composition and pipelining

The Assignment 5 action workflow is implemented in `SurvivalSim/GameLogic/Main.cs`:

1. `GameAction.Create` validates an `ActionRequest`.
2. `PrepareAction` adds half the original duration below the fatigue threshold.
3. `HasEnoughEnergy` checks the prepared action against the current state.
4. `CalculateEffects` applies the supplied hunger and thirst functions.
5. `ApplyEffects` returns a new state with time and reserves updated.
6. `ExecuteBatch` repeats the workflow over a read-only request sequence.

The test runner verifies successful actions, an empty batch, exact energy boundaries, invalid input, policy differences, deterministic repeated execution, caller-collection preservation, and zero-cost actions. The source-backed Assignment 5 mapping is:

| Requirement | Implementation | Verification | Status* |
|---|---|---|---|
| Typed immutable domain values | Records in `Models/Models.cs` | Assignment 5 tests and compilation | Complete |
| Validated action input | `GameAction.Create`, called by `Main.ExecuteAction` | Invalid duration, missing input, invalid energy tests | Complete |
| Three useful pipeline stages | `PrepareAction`, `CalculateEffects`, `ApplyEffects` | Pure pipeline test and normal workflow test | Complete |
| Configurable survival behavior | Policy delegates in `Main.cs` | `TwoPolicies` test | Complete |
| Sequential batch processing | `Main.ExecuteBatch` | Empty dataset and batch tests | Complete |
| State preservation after rejection | `ActionOutcome` returns the input state on failure | Insufficient-energy and invalid-input tests | Complete |

\* No Assignment 5 PDF is present in this workspace. These statuses are based on the existing source, tests, and previous README description, not an independent PDF comparison.

## Assignment 6 — validation and temporal coupling

Assignment 6 makes the ordering dependency explicit. Raw requests are validated before a `GameAction` can be used. The `GameAction` constructor is private, and `ActionValidation.Valid` carries the only constructed action exposed by the factory. `Main.ExecuteAction` pattern-matches `Valid` and `Invalid`; only the valid branch calls `ExecuteValidatedAction`.

`ExecuteValidatedAction` then prepares the action, checks energy before calculating or applying effects, and returns the original state for missing policies, insufficient energy, invalid policy output, or overflow. Repeated operations therefore consume the state produced by the previous successful operation, while a failed operation does not consume time or energy.

| Requirement | Implementation | Verification | Status* |
|---|---|---|---|
| Safe construction after validation | Private `GameAction` constructor and `GameAction.Create` | Factory success/invalid tests | Complete |
| Explicit mutually exclusive validation cases | `ActionValidation.Valid` and `.Invalid` | Pattern-matching test | Complete |
| Validated value passed to next stage | `ExecuteValidatedAction(GameState, GameAction, ...)` | Normal operation test | Complete |
| Prerequisite check before effects | `HasEnoughEnergy` before `CalculateEffects` and `ApplyEffects` | Missing-prerequisite test | Complete |
| Safe state transition | `with` expressions and unchanged failure state | State-preservation test | Complete |
| Repeated operation behavior | Repeated calls to `Main.ExecuteAction` | Repeated-operation test | Complete |

\* The Assignment 6 PDF is not present in the workspace; statuses are source- and test-based.

## Assignment 7 — results, options, and crafting

### Result and Option flow

`Result<T>` is declared in `Models/Models.cs`. `RecipeLookup.FindRecipe` returns `Option<CraftRecipe>.Some` for a matching ID and `.None` otherwise. `RequireRecipe` converts `.None` into `Result<CraftRecipe>.Fail("RECIPE NOT FOUND", ...)`.

`CraftingService.CheckCrafting` composes `RequireRecipe` with `CraftingValidation.ValidateIngredients` using `Bind`. `CraftWithResult` binds the validated recipe to `ExecuteCrafting`. On success, `CraftSummary` uses `Map` to turn the resulting `GameState` into a display string. A failed result is copied by `Map` or `Bind` without invoking the callback, as verified by the callback non-execution tests.

The real crafting chain is therefore:

```text
recipes + recipeId
  -> RequireRecipe (Option lookup converted to Result)
  -> ValidateIngredients
  -> ExecuteCrafting
  -> update inventory and return Result<GameState>
  -> Map (optional summary projection)
```

Meaningful expected failures include:

- `RECIPE NOT FOUND` when the requested recipe ID is absent.
- `INVALID QUANTITY` when the output or ingredient quantities are not positive.
- `MISSING_INGREDIENT` when grouped required quantities exceed the inventory.
- `NOT ENOUGH ENERGY` when the crafting action cannot be paid for.
- `INVALID DURATION`, `EMPTY RESULT ID`, `ID IS EMPTY`, and `INVALID ENERGY COST` for invalid recipe fields.

### JSON and I/O boundary

`RecipeLoader.Load` is the file-I/O boundary. It reads and deserializes JSON into `CraftRecipe[]`, validates non-empty IDs, positive quantities and durations, energy bounds, ingredient data, and duplicate recipe IDs, then returns an immutable array. It converts missing files to `FILE NOT FOUND`, malformed JSON to `INVALID JSON`, and other access/read failures to explicit result failures.

`CraftingService` and `CraftingValidation` receive already loaded recipes and a `GameState`; they do not read files or write to the console. This separates file I/O from recipe validation, action execution, and inventory transformation. `Program.cs` is responsible for loading files and reporting boundary failures.

### Crafting state transformation

For a recipe requiring two `loose_stone` and producing one `stone_blade`, an inventory containing five stones becomes three stones plus one blade. The crafting action also applies its duration and energy cost. `ExecuteCrafting` performs the action first and creates the updated inventory only after success. If lookup, validation, or execution fails, the original `GameState` is returned inside a failed `Result` and its inventory, energy, and time remain unchanged.

| Requirement | Implementation | Verification | Status* |
|---|---|---|---|
| Explicit success/failure result | `Result<T>.Success` and `.Fail` | Successful crafting and failure tests | Complete |
| Optional recipe lookup | `RecipeLookup.FindRecipe` and `Option<T>` | Found/missing lookup tests | Complete |
| Missing recipe converted to failure | `RecipeLookup.RequireRecipe` | Missing-recipe-result test | Complete |
| `Map` and `Bind` composition | `ResultExtensions.cs` and `CraftingService` | Map/Bind success and propagation tests | Complete |
| Failure callbacks are not executed | Pattern matches in `Map` and `Bind` | Callback non-execution tests | Complete |
| JSON deserialization and validation | `RecipeLoader.Load` | Valid JSON test | Complete |
| Missing/malformed file handling | `RecipeLoader.Load` exception cases | Missing JSON and malformed JSON tests | Complete |
| Successful inventory transformation | `CraftingService.UpdateInventory` | Successful crafting test | Complete |
| Failed crafting preserves state | Failure returned before inventory update | Failure-preservation test | Complete |
| Duplicate ingredient aggregation | `CraftingValidation` groups ingredients | Duplicate-ingredients test | Complete |

\* The Assignment 7 PDF is not present in this workspace; these statuses are supported by the implementation and tests, but cannot be checked against the PDF text here.

## Automated tests

The test project is a custom console runner in `SurvivalSim.Tests/Program.cs`; it does not use xUnit, NUnit, or another test framework. Run it with:

```powershell
dotnet run --project SurvivalSim.Tests/SurvivalSim.Tests.csproj
```

The current runner registers 76 tests:

- Assignment 5: 11 tests covering normal execution, empty input, boundaries, invalid input, policies, purity, caller collection preservation, and zero cost.
- Assignment 6: 9 tests covering factory construction, pattern matching, prerequisites, repetition, boundaries, and failure state preservation.
- Assignment 7: 16 tests covering successful and failed crafting, recipe lookup, `Map`, `Bind`, failure propagation, JSON loading errors, valid JSON, state preservation, duplicate ingredients, and missing-recipe failure.
- Stages 2–6: 30 tests covering movement, world interactions, persistence, original report behavior, currying, native C calculations, and malformed-save handling.
- Assignment 8 additions: 10 tests covering empty/no-match reports, grouping and totals, deterministic tie ordering, invalid report input, JSON-loaded world data, malformed/unknown world data, the actual `yield return` iterator, controlled native adapter failure, file-handle release after successful and failed convenience reads, and a complete native-strike/report workflow with both success and failure paths.

Assignment 8 test cases and expected outcomes:

| Test | Expected result |
|---|---|
| Empty world and no matching resource records | Both empty reports return zero rows. |
| Resource grouping, totals, and stable ties | `stick` totals 5 across 2 tiles; `loose_stone` totals 2 across 1 tile; equal totals sort by item ID. |
| Terrain grouping and stable ties | `grass` has 2 tiles and `sand` has 1; equal counts sort `grass` before `sand`. |
| Null report input | Both report builders reject null with `ArgumentNullException`. |
| JSON-loaded application world | Loader returns 768 tiles and 8 objects; the first resource row totals 3. |
| Malformed/unknown world data | Malformed JSON and unknown terrain return controlled `Result` failures. |
| Actual `yield return` iterator | Zero items before enumeration, two after `Take(2)`, nine in the snapshot, snapshot reuse adds no work, and a second deferred enumeration adds nine more. |
| Controlled native adapter failure | Injected `DllNotFoundException` becomes `NATIVE_LIBRARY_UNAVAILABLE`. |
| Convenience file cleanup | Source files can be reopened with exclusive sharing after successful and malformed-content reads. |
| Complete native/report workflow | Missing axe returns `TOOL_REQUIRED` without changing the input state; with an axe, the native strike succeeds, energy becomes 98, and reports include 3 logs and 4 sticks. |

The runner exits with a non-zero exit code if any test fails. **The 76 tests have not yet been executed in this environment** because the .NET SDK is unavailable; do not report them as passing until the commands below have been run successfully on your machine.

## Known limitations and discrepancies

- `Program.cs` loads recipes but does not expose a user-facing crafting command. Crafting is reachable through `CraftingService` and the tests, not through an interactive menu.
- `GameState.Inventory` is declared as `IReadOnlyList<ItemStack>`; the current callers commonly use `ImmutableArray`, but the type itself does not enforce an immutable backing collection.
- `GameAction.Create` uses `string.IsNullOrEmpty`, so whitespace-only action names are accepted.
- `CraftingSystem` contains the older `Craft` path and the misspelled helper `HasIngridients`; the Assignment 7 tests exercise `CraftingService` instead.
- `CraftingService.UpdateInventory` uses `ToDictionary`; callers should use inventories with unique item IDs. Duplicate recipe ingredients are handled by validation before this update.
- The exact Assignment 5–7 PDFs are not included in this repository. The assignment mapping below describes the functionality demonstrated by the source and tests; compare it with your instructors’ rubrics before submission.

## Submission

Include the source projects, all required JSON under `SurvivalSim/Data/` (including `Data/Definitions/world.json`), tests, project files, native source/header/build scripts, and this `README.md`. Exclude generated or machine-specific content such as:

```text
bin/
obj/
.vs/
*.user
*.suo
```

Before submission, build and run the test project from a clean extraction. The project file copies JSON data files under `Data/` into the build output. Save files and exported reports are written relative to the executable output directory, not back into the source tree.

## Stage 4 — Persistence, world queries, and reports

Stage 4 adds versioned JSON save/load for the complete simulation, lazy world-query extension methods, and two LINQ reports. See `SurvivalSim/STAGE4_README.md` for the save schema, controls, report definitions, functional-programming notes, and test coverage.

New controls: `F5` save, `F6` load, `F7` export reports, and `N` list nearby objects. The playable starting world is loaded from `Data/Definitions/world.json` by `WorldDataLoader`, which validates terrain, item, object, position, and quantity references before the game starts.


## Stage 5 — Assignment 8 functional and native integration

Stage 5 adds a curried gathering-cost policy (`GatheringConfig -> ObjectDefinition -> Result<int>`) that is used by the real object-strike workflow. The policy captures configuration in a closure and delegates the calculation to a native C library. The native function is imported with `DllImport` and `CallingConvention.Cdecl`; its managed adapter validates inputs and maps missing-library, binding, binary-format, and native calculation failures into `Result<int>`.

The native source and build instructions are in `SurvivalSim/Native/` and `SurvivalSim/STAGE5_README.md`. On Windows, build both managed projects, then run `powershell -ExecutionPolicy Bypass -File SurvivalSim/Native/build-native.ps1`. The script copies the DLL beside both application and test executables. Run the complete tests with `dotnet run --project SurvivalSim.Tests/SurvivalSim.Tests.csproj --no-build`.

The existing `GetVisiblePositions` iterator uses `yield return`. Assignment 8 tests now probe this actual world-coordinate iterator: creating the enumerable produces no coordinates, `Take(2)` produces only two, enumerating the remainder produces the rest, and a `ToArray` snapshot can be reused without rerunning the iterator. Re-enumerating the original deferred sequence runs it again. `GetNearbyObjects` remains a deferred LINQ query. Materialize with `ToArray` or `ToList` only when a stable snapshot or repeated access is required.

Native-dependent gameplay and tests require the platform library to be built. If it is missing, gathering returns a controlled `NATIVE_LIBRARY_UNAVAILABLE` failure rather than crashing the application. See `SurvivalSim/STAGE5_README.md` for formulas, resource ownership, and platform build commands.


## Assignment 8 — rubric mapping and demonstration guide

The playable world is loaded from `SurvivalSim/Data/Definitions/world.json`. `Program.cs` calls `WorldDataLoader.Load` before starting the exploration loop; the two report commands operate on that same loaded and subsequently updated `WorldState`. The JSON file contains 768 tiles, 8 placed objects, the player start position, and ground item stacks. `WorldDataLoader` validates definition references and world structure and returns a `Result<WorldState>` failure for missing, malformed, or inconsistent input.

### Task 1 — custom extension methods

| Extension | Receiver and return type | Purpose and application call |
|---|---|---|
| `WorldPosition.GetVisiblePositions(int radiusX, int radiusY)` | `WorldPosition` → `IEnumerable<WorldPosition>` | The receiver is the viewport center. It lazily produces coordinates around that position. `WorldRenderer.Render` calls it and materializes the current frame with `ToArray()`. |
| `WorldState.GetNearbyObjects(WorldPosition center, int radius)` | `WorldState` → `IEnumerable<WorldObject>` | The receiver owns the world-object collection being queried. It filters objects by level and radius. The `N` command in `Program.cs` calls it. |
| `IEnumerable<ItemStack>.QuantityOf(string itemId)` | `IEnumerable<ItemStack>` → `int` | A collection-level aggregate belongs on the stack sequence. It returns zero when the item is absent. Exercised by inventory quantity assertions in tests; the first two methods are the two required application-called extensions. |

The first two are the required custom extensions used by the running application. The third is an additional reusable collection query.

### Task 2 — two LINQ reports

Both report methods are pure transformations in `SurvivalSim/Reports/WorldReports.cs`. They do not write to the console or files; `Program.cs` formats and exports their results after pressing `F7`.

| Report | Input and intended user | Filters and transformations | Output and expected sample result |
|---|---|---|---|
| Resource distribution | Ground item stacks in the loaded world; intended for a player checking gathered resources | `SelectMany` flattens stacks, `Where` excludes non-positive quantities, `GroupBy` groups by item ID, `Sum` totals quantity, and a tile count records how many tiles contain the item. Sort: quantity descending, then item ID ordinal ascending. | Item ID, total quantity, tile count. The initial JSON world contains `loose_stone` quantity 3 on 1 tile and `stick` quantity 2 on 1 tile. |
| Terrain and object statistics | Loaded world tiles and placed objects; intended for checking world composition | `GroupBy` groups tiles by terrain; `Count` totals tiles and objects whose tile has that terrain. Sort: tile count descending, then terrain ID ordinal ascending. | Terrain ID, tile count, object count. The initial JSON map has 381 grass tiles (6 objects), 308 stone tiles (2 objects), 78 deep-water tiles (0 objects), and 1 sand tile (0 objects). |

Both reports define deterministic secondary ordering with `ThenBy`. Tests cover empty worlds, no ground-item matches, grouping and totals, and tied primary sort values. The source data is loaded from JSON by the application, not generated only inside the report methods.

### Task 3 — currying and partial application

`GatheringCostPolicy.Create(GatheringConfig)` returns `Func<ObjectDefinition, Result<int>>`. It fixes the configuration in a closure and accepts the object definition later. `InteractionService.Strike` passes the configured function into the gathering workflow; it does not change a global setting.

For the `oak_tree` definition (`MaxDurability = 100`), difficulty is `100 + 100 / 10 = 110%`. With base cost 10:

- Basic configuration, efficiency 100%: `ceil(10 × 110 / 100) = 11`.
- Efficient configuration, efficiency 200%: `ceil(10 × 110 / 200) = 6`.

`Stage5Tests.CurriedPoliciesUseDifferentConfigurations` asserts both exact results on the same oak-tree definition.

### Tasks 4–6 — deferred execution, resource lifetime, and retention

`WorldPosition.GetVisiblePositions` is a `yield return` iterator over world coordinates. The Assignment 8 probe wraps this real iterator in test code, so the production calculation has no diagnostic counter or other test side effect. The test asserts zero produced values before enumeration, two after `Take(2)`, nine in a full snapshot, no additional work when reusing the snapshot, and nine more when re-enumerating the deferred sequence.

`WorldRenderer` calls `ToArray()` at the frame boundary because the renderer needs a stable set of coordinates for one frame. This trades a small short-lived array allocation for predictable repeated access. `GetNearbyObjects` is deferred and retains the queried `WorldState` while the query is being enumerated; callers should materialize it if they need a stable snapshot beyond that operation. The `GetVisiblePositions` iterator state machine retains only the center coordinate and radius values, not a world collection. The curried gathering policy closure retains only its small immutable `GatheringConfig`, not a world or file handle. There are no long-lived application caches or event subscriptions in this project; therefore there is no cache/event retention category to manage.

`WorldDataLoader`, `DefinitionLoader`, and `RecipeLoader` use `File.ReadAllText`; report/save export uses `File.WriteAllText`. These convenience APIs open and close their internal file handles before returning or throwing, so the calling method owns the bounded read/write operation and no stream escapes. `WorldSaveService` owns its explicit temporary-file write and cleanup path. The native function accepts and returns integers only; it allocates no native buffers or handles, and the operating system releases the loaded shared library when the process exits. Tests verify that the source file can be reopened with exclusive sharing after both a successful convenience read and a malformed-JSON failure.

### Tasks 7–9 — native C integration

- C source/header: `SurvivalSim/Native/survivalsim_native.c` and `.h`.
- Export: `calculate_gathering_cost(int32_t base_cost, int32_t tool_efficiency_percent, int32_t difficulty_percent)`; fixed-width C `int32_t` inputs and output (matching C# `System.Int32`); C ABI; positive inputs required.
- Formula: `ceil(base_cost × difficulty_percent / tool_efficiency_percent)`. Return `-1` for invalid arguments and `-2` if the result exceeds `INT32_MAX`.
- C# binding: `NativeGatheringCalculator` uses `DllImport`, the exact export name, `System.Int32` parameters/return, and `CallingConvention.Cdecl`.
- Adapter: validates positive domain inputs and maps missing library, missing entry point, invalid binary, and native error codes to `Result<int>.Fail`.
- Real call: `Stage5Tests.NativeCalculationReturnsExpectedCost` checks `(10, 200, 150)`, whose manually calculated result is `ceil(1500 / 200) = 8`. Gameplay calls the same adapter from `InteractionService.Strike`.
- Failure path: an internal injected-call seam makes a `DllNotFoundException` test deterministic; `Stage8Tests.NativeAdapterFailureBecomesResultFailure` expects `NATIVE_LIBRARY_UNAVAILABLE`. This complements, and does not replace, the real native call test.
- Integration issue and resolution: the managed runtime must find the shared library beside the application or test executable, not merely beside the C source. The platform build scripts therefore copy the generated library into both output directories.

**Verified native build environment:** Debian GNU/Linux 13, x86_64 (`x86_64-linux-gnu`), GCC 14.2.0. The Linux script builds `libsurvivalsim_native.so` with `-std=c11 -O2 -Wall -Wextra -Werror -fPIC -shared`. The Windows script targets 64-bit MinGW-w64 (`x86_64-w64-mingw32`) and builds `survivalsim_native.dll`; the Windows build must be verified locally with MinGW-w64 GCC on PATH. See `SurvivalSim/Native/README.md` for exact commands and output locations.

### Contribution record — complete before uploading

- Student: **[replace with your full name]**
- Group: **Individual submission**
- Contribution to confirm/edit: world data loading and validation, two LINQ reports, extension methods, curried gathering policy, lazy world iteration, native C binding/adapter, automated tests, and README documentation.

### Current verification status

The native C source was compiled in the environment stated above with warnings treated as errors, and a small native harness returned the expected normal, invalid-input, and overflow values. JSON files and the ZIP archive were checked. The .NET SDK is not installed in this editing environment, so the 76 C# tests and the complete managed build are **not yet verified**. Run the clean Windows commands below and update this status with the actual output before submitting.

## Stage 6 — Final integration and validation hardening

Stage 6 hardens world persistence and prepares a clean submission. Save writes now use a temporary file in the destination directory followed by an overwrite move, reducing the chance that an interrupted write corrupts the previous save. Load validates required sections, null entries, definition references, positive quantities, object IDs/durability, duplicate positions/IDs, and object/player tile membership before constructing immutable state. Invalid or malformed files return a `Result` failure rather than partially constructing a world.

Five additional tests cover malformed JSON, missing sections, unknown item references, incomplete save state, and preservation of the caller's state after a failed load. Assignment 8 then adds ten focused tests. The full runner registers 76 tests; the C# suite still requires a local .NET build and execution.

### Clean verification on Windows

From a fresh extraction, open PowerShell in the repository root and run:

```powershell
dotnet --version
dotnet build SurvivalSim/SurvivalSim.csproj
dotnet build SurvivalSim.Tests/SurvivalSim.Tests.csproj
powershell -ExecutionPolicy Bypass -File SurvivalSim/Native/build-native.ps1
dotnet run --project SurvivalSim.Tests/SurvivalSim.Tests.csproj --no-build
dotnet run --project SurvivalSim/SurvivalSim.csproj --no-build
```

The native DLL must be built after the managed projects, because the script places it beside both executables. For a Release build, build both projects with `-c Release` and invoke the native script with `-Configuration Release`. Do not include `bin/`, `obj/`, `.vs/`, or user-specific IDE files in the submission.


## Survival interface and gameplay additions

The exploration screen now shows a persistent status HUD with health, energy, hunger, and thirst bars. The in-game clock is displayed as `HH:MM`. Press `V` to open the inventory panel; it lists readable item names and quantities and supports dropping selected stacks. Press `C` to browse JSON-defined crafting recipes and craft an item. Press `B` to choose a building recipe and place the structure on one of the four adjacent tiles.

Building recipes are stored in `SurvivalSim/Data/Definitions/building_recipes.json`. They reference existing object definitions, consume inventory ingredients, spend in-game time and energy, and add a real `WorldObject` to the map. The current starter structures are wooden walls and stone furnaces. Built structures are included in normal world save/load data.

### Survival rate tuning

Normal hunger now decreases by one point per 180 in-game minutes (three hours), and normal thirst by one point per 60 minutes. Elapsed minutes are accumulated across short actions, so repeatedly moving for a few minutes does not discard fractional survival costs. The accelerated policies remain available for demonstrations/tests. Health is displayed and begins to decline by one point per 60 in-game minutes while hunger or thirst remains at zero.

### New controls

- `V`: open/close inventory; press `D` inside it to drop an item stack.
- `C`: open crafting; choose a listed recipe by number.
- `B`: open building; choose a structure, then select an adjacent tile with `W/A/S/D`.

The project remains a console application, so these panels are keyboard-driven rather than separate desktop windows. Run the app in a terminal with ANSI/Unicode support for the best presentation.
