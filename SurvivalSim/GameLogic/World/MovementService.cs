using SurvivalSim.Models;
using SurvivalSim.Models.World;
using Game = SurvivalSim.GameLogic.Main;

namespace SurvivalSim.GameLogic.World;

/// <summary>Pure movement transitions. The caller owns rendering and keyboard input.</summary>
public static class MovementService
{
    public static Result<SimulationState> Move(
        SimulationState state, GameDefinitions definitions, int dx, int dy)
    {
        if ((dx, dy) is not ((1, 0) or (-1, 0) or (0, 1) or (0, -1)))
            return new Result<SimulationState>.Fail("INVALID_DIRECTION", "Move one tile in a cardinal direction.");

        var current = state.World.PlayerPosition;
        var target = current with { X = current.X + dx, Y = current.Y + dy };
        return Travel(state, definitions, target, "Move");
    }

    public static Result<SimulationState> ChangeLevel(
        SimulationState state, GameDefinitions definitions, int dz)
    {
        if (dz is not (1 or -1))
            return new Result<SimulationState>.Fail("INVALID_DIRECTION", "Use +1 or -1 for stairs.");

        string required = dz > 0 ? "stairs_up" : "stairs_down";
        if (!state.World.Objects.Values.Any(o =>
                o.Position == state.World.PlayerPosition && o.DefinitionId == required))
            return new Result<SimulationState>.Fail("NO_STAIRS", $"No {required} at your position.");

        var p = state.World.PlayerPosition;
        var target = p with { Z = p.Z + dz };
        string destinationStair = dz > 0 ? "stairs_down" : "stairs_up";
        if (!state.World.Objects.Values.Any(o => o.Position == target && o.DefinitionId == destinationStair))
            return new Result<SimulationState>.Fail("UNCONNECTED_STAIRS", "The stairs do not connect to another level.");

        return Travel(state, definitions, target, "Climb stairs");
    }

    private static Result<SimulationState> Travel(
        SimulationState state, GameDefinitions definitions, WorldPosition target, string actionName)
    {
        if (!state.World.Tiles.TryGetValue(target, out var tile))
            return new Result<SimulationState>.Fail("OUT_OF_WORLD", "No world tile exists there.");

        var terrainId = tile.TerrainId;
        if (!definitions.Terrain.TryGetValue(terrainId, out var terrain))
            return new Result<SimulationState>.Fail("UNKNOWN_TERRAIN", $"Missing terrain definition: {terrainId}");
        if (!terrain.Walkable)
            return new Result<SimulationState>.Fail("BLOCKED_TERRAIN", $"{terrain.Name} is impassable.");

        foreach (var obj in state.World.Objects.Values.Where(o => o.Position == target))
        {
            if (!definitions.Objects.TryGetValue(obj.DefinitionId, out var definition))
                return new Result<SimulationState>.Fail("UNKNOWN_OBJECT", $"Missing object definition: {obj.DefinitionId}");
            if (definition.BlocksMovement)
                return new Result<SimulationState>.Fail("BLOCKED_OBJECT", $"{definition.Name} blocks movement.");
        }

        if (terrain.MovementCost <= 0)
            return new Result<SimulationState>.Fail("INVALID_COST", "Walkable terrain needs a positive movement cost.");

        // The terrain movement cost is interpreted as minutes per tile.
        // Energy cost is deliberately small until the native cost calculation is introduced.
        int energyCost = Math.Clamp((terrain.MovementCost + 9) / 10, 1, 100);
        var outcome = Game.ExecuteAction(state.Survival,
            new ActionRequest(actionName, terrain.MovementCost, energyCost),
            Game.normalHunger, Game.normalThirst);
        if (!outcome.Success)
            return new Result<SimulationState>.Fail("ACTION_FAILED", outcome.Message);

        return new Result<SimulationState>.Success(state with
        {
            Survival = outcome.State,
            World = state.World with { PlayerPosition = target }
        });
    }

}
