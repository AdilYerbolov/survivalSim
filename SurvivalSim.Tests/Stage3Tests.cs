using System.Collections.Immutable;
using SurvivalSim.Models;
using SurvivalSim.Models.World;
using SurvivalSim.GameLogic;
using SurvivalSim.GameLogic.World;

public static class Stage3Tests
{
    private static (SimulationState, GameDefinitions) Setup()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "Definitions");
        var result = DefinitionLoader.Load(path);
        if (result is not Result<GameDefinitions>.Success success) throw new Exception("Cannot load definitions: " + result);
        var game = new GameState(new Player(100, 100, 100), 480,
            ImmutableArray.Create(new ItemStack("stone_axe", 1), new ItemStack("stone_pickaxe", 1)));
        return (new SimulationState(game, WorldGenerator.CreateTestWorld()), success.Value);
    }
    public static void PickupAndDrop()
    {
        var (state, defs) = Setup();
        state = state with { World = state.World with { PlayerPosition = new WorldPosition(6, 6, 0) } };
        var pickup = InteractionService.Pickup(state, defs, "loose_stone", 2);
        Assert.True(pickup is Result<SimulationState>.Success);
        var next = ((Result<SimulationState>.Success)pickup).Value;
        Assert.Equal(2, next.Survival.Inventory.Single(i => i.ItemId == "loose_stone").Quantity);
        Assert.Equal(1, next.World.Tiles[next.World.PlayerPosition].GroundItems.Single(i => i.ItemId == "loose_stone").Quantity);
        var drop = InteractionService.Drop(next, defs, "loose_stone", 1);
        Assert.True(drop is Result<SimulationState>.Success);
        var after = ((Result<SimulationState>.Success)drop).Value;
        Assert.Equal(2, after.World.Tiles[after.World.PlayerPosition].GroundItems.Single(i => i.ItemId == "loose_stone").Quantity);
        Assert.Equal(480, after.Survival.TimeMinutes);
    }
    public static void FailedPickupPreservesState()
    {
        var (state, defs) = Setup();
        var result = InteractionService.Pickup(state, defs, "loose_stone", 1);
        Assert.True(result is Result<SimulationState>.Fail);
        Assert.Equal("NOT_ENOUGH_ITEMS", ((Result<SimulationState>.Fail)result).Code);
        Assert.Equal(480, state.Survival.TimeMinutes);
    }
    public static void ToolRequired()
    {
        var (state, defs) = Setup();
        state = state with { World = state.World with { PlayerPosition = new WorldPosition(6, 4, 0) },
            Survival = state.Survival with { Inventory = ImmutableArray<ItemStack>.Empty } };
        var result = InteractionService.Strike(state, defs, new WorldPosition(7, 4, 0));
        Assert.True(result is Result<SimulationState>.Fail);
        Assert.Equal("TOOL_REQUIRED", ((Result<SimulationState>.Fail)result).Code);
    }
    public static void DestroyTreeSpawnsDrops()
    {
        var (state, defs) = Setup();
        state = state with { World = state.World with { PlayerPosition = new WorldPosition(6, 4, 0) } };
        var target = new WorldPosition(7, 4, 0);
        var result = InteractionService.Strike(state, defs, target, 100);
        Assert.True(result is Result<SimulationState>.Success);
        var next = ((Result<SimulationState>.Success)result).Value;
        Assert.True(!next.World.Objects.Values.Any(o => o.Position == target));
        Assert.Equal(3, next.World.Tiles[target].GroundItems.Single(i => i.ItemId == "log").Quantity);
        Assert.Equal(2, next.World.Tiles[target].GroundItems.Single(i => i.ItemId == "stick").Quantity);
        Assert.Equal(480, state.Survival.TimeMinutes);
        Assert.Equal(490, next.Survival.TimeMinutes);
    }
    public static void PartialDamage()
    {
        var (state, defs) = Setup();
        state = state with { World = state.World with { PlayerPosition = new WorldPosition(6, 4, 0) } };
        var result = InteractionService.Strike(state, defs, new WorldPosition(7, 4, 0));
        var next = ((Result<SimulationState>.Success)result).Value;
        Assert.Equal(75, next.World.Objects.Values.Single(o => o.Position == new WorldPosition(7, 4, 0)).Durability);
    }
    public static void LowEnergyStrike()
    {
        var (state, defs) = Setup();
        state = state with { World = state.World with { PlayerPosition = new WorldPosition(6, 4, 0) },
            Survival = state.Survival with { Player = state.Survival.Player with { Energy = 0 } } };
        var result = InteractionService.Strike(state, defs, new WorldPosition(7, 4, 0), 100);
        Assert.True(result is Result<SimulationState>.Fail);
        Assert.True(state.World.Objects.Values.Any(o => o.Position == new WorldPosition(7, 4, 0)));
    }
    public static void TerrainIsDataDriven()
    {
        var (state, defs) = Setup();
        Assert.Equal("grass", state.World.Tiles[new WorldPosition(4, 4, 0)].TerrainId);
        Assert.True(defs.Terrain.ContainsKey("grass"));
    }
}
