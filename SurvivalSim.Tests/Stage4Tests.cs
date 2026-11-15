using System.Collections.Immutable;
using SurvivalSim.GameLogic;
using SurvivalSim.GameLogic.World;
using SurvivalSim.Models;
using SurvivalSim.Models.World;
using SurvivalSim.Reports;

public static class Stage4Tests
{
    private static (SimulationState State, GameDefinitions Definitions) Setup()
    {
        var loaded = DefinitionLoader.Load(Path.Combine(AppContext.BaseDirectory, "Data", "Definitions"));
        if (loaded is not Result<GameDefinitions>.Success success)
            throw new Exception("Definitions could not be loaded for Stage 4 tests.");
        var game = new GameState(new Player(87, 91, 82), 525,
            ImmutableArray.Create(new ItemStack("stone_axe", 1), new ItemStack("stick", 4)));
        var worldPath = Path.Combine(AppContext.BaseDirectory, "Data", "Definitions", "world.json");
        var worldResult = WorldDataLoader.Load(worldPath, success.Value);
        if (worldResult is not Result<WorldState>.Success worldSuccess)
            throw new Exception("JSON world data could not be loaded for Stage 4 tests.");
        return (new SimulationState(game, worldSuccess.Value), success.Value);
    }

    public static void SaveLoadRoundTrip()
    {
        var (state, definitions) = Setup();
        var changed = state with
        {
            World = state.World with { PlayerPosition = new WorldPosition(6, 6, 0) }
        };
        var path = Path.Combine(Path.GetTempPath(), $"survivalsim-{Guid.NewGuid():N}", "save.json");
        try
        {
            var saved = WorldSaveService.Save(changed, path);
            Assert.True(saved is Result<string>.Success);
            var loaded = WorldSaveService.Load(path, definitions);
            Assert.True(loaded is Result<SimulationState>.Success);
            var restored = ((Result<SimulationState>.Success)loaded).Value;
            Assert.Equal(changed.Survival.TimeMinutes, restored.Survival.TimeMinutes);
            Assert.Equal(changed.Survival.Player, restored.Survival.Player);
            Assert.Equal(changed.World.PlayerPosition, restored.World.PlayerPosition);
            Assert.Equal(changed.World.Tiles.Count, restored.World.Tiles.Count);
            Assert.Equal(changed.World.Objects.Count, restored.World.Objects.Count);
            Assert.Equal(4, restored.Survival.Inventory.QuantityOf("stick"));
        }
        finally
        {
            var directory = Path.GetDirectoryName(path)!;
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    public static void MissingSaveReturnsFailure()
    {
        var (_, definitions) = Setup();
        var result = WorldSaveService.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"), definitions);
        Assert.True(result is Result<SimulationState>.Fail);
        Assert.Equal("SAVE_NOT_FOUND", ((Result<SimulationState>.Fail)result).Code);
    }

    public static void VisiblePositionsAreLazyAndCorrect()
    {
        var center = new WorldPosition(5, 8, -1);
        var positions = center.GetVisiblePositions(1, 1);
        Assert.Equal(9, positions.Count());
        Assert.True(positions.Contains(new WorldPosition(4, 7, -1)));
        Assert.True(positions.Contains(new WorldPosition(6, 9, -1)));
    }

    public static void NearbyObjectsUsesRadiusAndLevel()
    {
        var (state, _) = Setup();
        var nearby = state.World.GetNearbyObjects(new WorldPosition(6, 4, 0), 1).ToArray();
        Assert.True(nearby.Any(obj => obj.DefinitionId == "oak_tree"));
        Assert.True(nearby.All(obj => obj.Position.Z == 0));
        Assert.True(nearby.All(obj => Math.Abs(obj.Position.X - 6) <= 1 && Math.Abs(obj.Position.Y - 4) <= 1));
    }

    public static void ResourceReportAggregatesAndSorts()
    {
        var (state, _) = Setup();
        var rows = WorldReports.BuildResourceDistribution(state.World);
        Assert.Equal("loose_stone", rows[0].ItemId);
        Assert.Equal(3, rows[0].TotalQuantity);
        Assert.Equal("stick", rows[1].ItemId);
        Assert.Equal(2, rows[1].TotalQuantity);
    }

    public static void TerrainReportCountsTiles()
    {
        var (state, _) = Setup();
        var rows = WorldReports.BuildTerrainAndObjectStatistics(state.World);
        Assert.True(rows.Any(row => row.TerrainId == "grass" && row.TileCount > 0));
        var expected = rows.OrderByDescending(row => row.TileCount)
            .ThenBy(row => row.TerrainId, StringComparer.Ordinal).ToArray();
        Assert.True(expected.SequenceEqual(rows));
    }
}
