# Stage 3 — World interactions and architectural cleanup

## Changes
- Renamed `WorldPostition`, `PlayerPostition`, `Postition` to `WorldPosition`, `PlayerPosition`, `Position` across source/tests.
- `WorldTile.TerrainId` is now a string referencing `terrain.json`, removing the hardcoded enum-to-ID translation from movement and rendering. The old `TerrainType` enum remains for backward compatibility, but is unused by the new world.
- The renderer builds a coordinate index once per frame instead of scanning all objects for every visible tile.
- Added `InteractionService`: inspect, pick up, drop, strike adjacent object, reduce durability, and spawn JSON-defined drops on destruction.
- Starting inventory contains a stone axe and pickaxe for trying interactions; these are demo starter tools.
- Added 7 automated Stage 3 tests.

## Controls
WASD / arrows: move. `>`/`<`: stairs. `G`: pick up item on current tile (enter item ID and quantity). `P`: drop inventory item. `X`: strike adjacent object (then WASD direction). `I`: inspect current tile. `V`: inventory. `Q`: quit.

Try walking to (6,4,0), facing the oak tree at (7,4,0), and press `X`, then `D`. Strike four times to fell the tree; move onto the cleared tile and pick up logs with `G`.

## Limitations and next steps
- This is Stage 3, not the complete Assignment 8. Full LINQ reports, currying, lazy enumeration demonstration, native C/PInvoke, world save/load and report export remain.
- World generation remains a fixed demo. No chunk streaming yet.
- Pickup/drop are instantaneous; striking consumes time and energy via the existing survival action pipeline.
- World definitions are loaded from JSON; world save/load is not implemented yet.
- `Console.Clear` may flicker. A diff-based renderer can be added later.
- The full .NET test suite must be run locally; the build environment used to prepare this archive has no `dotnet` executable.

## Run
`dotnet run --project SurvivalSim/SurvivalSim.csproj`

`dotnet run --project SurvivalSim.Tests/SurvivalSim.Tests.csproj`
