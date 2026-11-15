# Stage 1 — JSON definitions

The existing game now loads `Data/Definitions/items.json`, `terrain.json`, and `objects.json` at startup. The renderer uses the symbols and colors from those files. `recipes.json` is also copied into `Data/Definitions` to match the existing Program.cs path. `actions.json` is preserved.

Definitions describe **types** of things. Individual world objects still store their own ID, position, and current durability. Object `drops` are data for a future destruction interaction; destroying objects is **not implemented** in this milestone.

Run: `dotnet run --project SurvivalSim/SurvivalSim.csproj` from the outer repository directory. Existing tests: `dotnet run --project SurvivalSim.Tests/SurvivalSim.Tests.csproj`.

Edit an item's `symbol` or `color` in its JSON file and run again to see the map change. Colors must be .NET `ConsoleColor` names. Invalid/missing/duplicate definitions and unknown drop item IDs produce a `Result<GameDefinitions>.Fail` instead of an unhandled error.

Remaining Assignment 8 work: extension methods, two LINQ reports and export, currying, lazy enumeration, resource-lifetime explanation, native C integration, and required tests. This package is **only Stage 1**, not the complete assignment.
