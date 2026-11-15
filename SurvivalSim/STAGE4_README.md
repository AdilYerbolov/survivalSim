# Stage 4 — Persistence, world queries, and LINQ reports

## What changed

### Save and load
- `GameLogic/World/WorldSaveService.cs` saves and loads the complete `SimulationState`.
- The JSON document stores a version, player statistics, game time, inventory, player position, tiles, ground items, and object instances/durability.
- Tiles and objects are written as arrays. This avoids relying on JSON support for `WorldPosition` dictionary keys.
- Loading validates the version, player statistics, terrain/item/object definition references, quantities, durability, duplicate keys, and player position.
- Save/load return `Result<T>` rather than throwing for expected file or data failures.

Default save path: `Data/Saves/world-save.json` under the application's output directory. The directory is created automatically.

### Extension methods
`GameLogic/World/WorldExtensions.cs` contains:
- `GetVisiblePositions(this WorldPosition center, int radiusX, int radiusY)`: lazily yields viewport coordinates. Used by `WorldRenderer`.
- `GetNearbyObjects(this WorldState world, WorldPosition center, int radius)`: lazily filters objects on the same Z-level within a square radius. Used by the `N` gameplay command.
- `QuantityOf(...)`: sums a selected item ID in a stack collection; exercised by inventory quantity assertions in tests.

### LINQ reports
`Reports/WorldReports.cs` contains two pure report projections:
1. **Resource distribution**: flattens ground item stacks, groups by item ID, sums quantities, counts the number of tiles containing each item, and sorts by quantity descending then ID ascending.
2. **Terrain and object statistics**: groups tiles by terrain ID, counts tiles and objects on each terrain, and sorts by tile count descending then terrain ID ascending.

Press `F7` to export both reports as text files under `Data/Reports` in the application output directory.

## Controls added
- `F5`: save the current world.
- `F6`: load the saved world.
- `F7`: generate and export both LINQ reports.
- `N`: list objects within radius 3 on the current Z-level.

Existing controls from Stage 3 remain available.

## Build and test
From the repository folder:

```powershell
dotnet build SurvivalSim/SurvivalSim.csproj
dotnet run --project SurvivalSim.Tests/SurvivalSim.Tests.csproj
```

The Stage 4 tests cover save/load round-trip, missing saves, viewport enumeration, nearby-object filtering, and both reports.

## Functional-programming notes
- The report methods are pure projections: they don't write files or mutate the world. The playable map is loaded from `Data/Definitions/world.json` by `WorldDataLoader`; both reports operate on that same loaded world and later reflect gameplay changes.
- File I/O is isolated in `WorldSaveService` and the report-export section of the application.
- `GetVisiblePositions` and `GetNearbyObjects` are lazy `IEnumerable` queries. The renderer explicitly materializes visible positions with `ToArray()` because it traverses the viewport as a fixed frame snapshot.
- JSON files are read/written inside methods that own the file operation; `File.ReadAllText` and `File.WriteAllText` manage their underlying handles internally.

## Current limitations
- Save format version 1 is intentionally simple; future structural changes should increment the version or add migration logic.
- The report's resource inventory currently counts items on the ground, not items carried by the player.
- Report files are overwritten each time `F7` is pressed.
- The playable world is currently a fixed JSON scenario; chunk streaming and procedural generation are future work.
