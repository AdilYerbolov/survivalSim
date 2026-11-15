using System.Collections.Immutable;
using SurvivalSim.Models;
using SurvivalSim.Models.World;
using SurvivalSim.GameLogic;
using SurvivalSim.GameLogic.World;

public static class Stage2Tests
{
    private static (SimulationState State, GameDefinitions Definitions) Setup(int energy = 100)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "Definitions");
        // Test executable may not copy main project's data; use the project source directory instead.
        if (!Directory.Exists(path))
            path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../SurvivalSim/Data/Definitions"));
        var result = DefinitionLoader.Load(path);
        if (result is not Result<GameDefinitions>.Success success)
            throw new Exception("Definitions did not load: " + result);
        return (new SimulationState(new GameState(new Player(energy, 100, 100), 480,
            ImmutableArray<ItemStack>.Empty), WorldGenerator.CreateTestWorld()), success.Value);
    }

    public static void MoveSuccess()
    {
        var (state, defs) = Setup();
        var result = MovementService.Move(state, defs, 1, 0);
        Assert.True(result is Result<SimulationState>.Success);
        var next = ((Result<SimulationState>.Success)result).Value;
        Assert.Equal(new WorldPosition(5, 4, 0), next.World.PlayerPosition);
        Assert.Equal(490, next.Survival.TimeMinutes);
        Assert.Equal(new WorldPosition(4, 4, 0), state.World.PlayerPosition);
    }
    public static void BlockedTree()
    {
        var (state, defs) = Setup();
        state = state with { World = state.World with { PlayerPosition = new WorldPosition(6, 4, 0) } };
        var result = MovementService.Move(state, defs, 1, 0);
        Assert.True(result is Result<SimulationState>.Fail);
        Assert.Equal("BLOCKED_OBJECT", ((Result<SimulationState>.Fail)result).Code);
        Assert.Equal(new WorldPosition(6, 4, 0), state.World.PlayerPosition);
    }
    public static void BlockedWater()
    {
        var (state, defs) = Setup();
        state = state with { World = state.World with { PlayerPosition = new WorldPosition(9, 3, 0) } };
        var result = MovementService.Move(state, defs, 0, 1);
        Assert.True(result is Result<SimulationState>.Fail);
        Assert.Equal("BLOCKED_TERRAIN", ((Result<SimulationState>.Fail)result).Code);
    }
    public static void Stairs()
    {
        var (state, defs) = Setup();
        state = state with { World = state.World with { PlayerPosition = new WorldPosition(5, 5, 0) } };
        var down = MovementService.ChangeLevel(state, defs, -1);
        Assert.True(down is Result<SimulationState>.Success);
        var underground = ((Result<SimulationState>.Success)down).Value;
        Assert.Equal(-1, underground.World.PlayerPosition.Z);
        var up = MovementService.ChangeLevel(underground, defs, 1);
        Assert.True(up is Result<SimulationState>.Success);
        Assert.Equal(0, ((Result<SimulationState>.Success)up).Value.World.PlayerPosition.Z);
    }
    public static void NoStairs()
    {
        var (state, defs) = Setup();
        var result = MovementService.ChangeLevel(state, defs, -1);
        Assert.True(result is Result<SimulationState>.Fail);
        Assert.Equal("NO_STAIRS", ((Result<SimulationState>.Fail)result).Code);
    }
    public static void InvalidDirection()
    {
        var (state, defs) = Setup();
        Assert.True(MovementService.Move(state, defs, 1, 1) is Result<SimulationState>.Fail);
    }
    public static void LowEnergy()
    {
        var (state, defs) = Setup(0);
        var result = MovementService.Move(state, defs, 1, 0);
        Assert.True(result is Result<SimulationState>.Fail);
        Assert.Equal("ACTION_FAILED", ((Result<SimulationState>.Fail)result).Code);
        Assert.Equal(480, state.Survival.TimeMinutes);
    }
}
