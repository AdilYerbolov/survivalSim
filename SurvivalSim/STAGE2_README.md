# Stage 2: Interactive multilevel movement

Run: `dotnet run --project SurvivalSim/SurvivalSim.csproj` from the ZIP root.
Choose **1** to explore. Move with WASD or arrow keys, **>** (period key) to descend while standing on `>`, **<** (comma key) to ascend while standing on `<`, Q to quit.

- Movement uses terrain JSON `walkable` and `movementCost` and object JSON `blocksMovement`.
- Each successful step applies survival time and energy through the existing `Main.ExecuteAction` pipeline.
- Failed moves preserve the original `SimulationState`.
- Test world contains a 24x16 surface (Z=0), underground (Z=-1), trees, a boulder, a furnace, loose stones, water, and stairs at (5,5).
- Player-centered colored viewport and first-ground-item rendering are preserved from Stage 1.
- **2** in the startup menu runs the existing A5/A6 batch demonstration.
- Run tests: `dotnet run --project SurvivalSim.Tests/SurvivalSim.Tests.csproj`.

**Not yet implemented:** gathering, pickup/drop, crafting through interactive menu, procedural generation, save/load, native C integration, A8 reports. This is a gameplay milestone, not the completed Assignment 8.

**Compatibility:** Existing `WorldPostition`/`PlayerPostition` spellings remain unchanged in this stage to avoid a broad rename.

**Build status:** dotnet SDK was unavailable in the packaging environment; compile and run tests locally.
