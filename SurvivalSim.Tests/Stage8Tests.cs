using System.Collections.Immutable;
using SurvivalSim.GameLogic;
using SurvivalSim.GameLogic.Native;
using SurvivalSim.GameLogic.World;
using SurvivalSim.Models;
using SurvivalSim.Models.World;
using SurvivalSim.Reports;

public static class Stage8Tests
{
    private static WorldState EmptyWorld() => new(
        ImmutableDictionary<WorldPosition, WorldTile>.Empty,
        ImmutableDictionary<Guid, WorldObject>.Empty,
        new WorldPosition(0, 0, 0));

    private static WorldState ReportWorld() => new(
        ImmutableDictionary<WorldPosition, WorldTile>.Empty
            .Add(new WorldPosition(0, 0, 0), new WorldTile("grass", ImmutableArray.Create(new ItemStack("stick", 2))))
            .Add(new WorldPosition(1, 0, 0), new WorldTile("sand", ImmutableArray.Create(new ItemStack("loose_stone", 2))))
            .Add(new WorldPosition(2, 0, 0), new WorldTile("grass", ImmutableArray.Create(new ItemStack("stick", 3)))),
        ImmutableDictionary<Guid, WorldObject>.Empty
            .Add(Guid.Parse("00000000-0000-0000-0000-000000000101"),
                new WorldObject(Guid.Parse("00000000-0000-0000-0000-000000000101"), "oak_tree", new WorldPosition(0, 0, 0), 100))
            .Add(Guid.Parse("00000000-0000-0000-0000-000000000102"),
                new WorldObject(Guid.Parse("00000000-0000-0000-0000-000000000102"), "boulder", new WorldPosition(1, 0, 0), 200)),
        new WorldPosition(0, 0, 0));

    public static void ReportsHandleEmptyWorldAndNoMatches()
    {
        var empty = EmptyWorld();
        Assert.Equal(0, WorldReports.BuildResourceDistribution(empty).Count);
        Assert.Equal(0, WorldReports.BuildTerrainAndObjectStatistics(empty).Count);

        var noItems = empty with
        {
            Tiles = ImmutableDictionary<WorldPosition, WorldTile>.Empty.Add(
                new WorldPosition(0, 0, 0), new WorldTile("grass", ImmutableArray<ItemStack>.Empty))
        };
        Assert.Equal(0, WorldReports.BuildResourceDistribution(noItems).Count);
    }

    public static void ResourceReportGroupsTotalsAndStableTies()
    {
        var rows = WorldReports.BuildResourceDistribution(ReportWorld());
        Assert.Equal(2, rows.Count);
        Assert.Equal("stick", rows[0].ItemId);
        Assert.Equal(5, rows[0].TotalQuantity);
        Assert.Equal(2, rows[0].TileCount);
        Assert.Equal("loose_stone", rows[1].ItemId);
        Assert.Equal(2, rows[1].TotalQuantity);
        Assert.Equal(1, rows[1].TileCount);

        var tiedWorld = ReportWorld() with
        {
            Tiles = ImmutableDictionary<WorldPosition, WorldTile>.Empty
                .Add(new WorldPosition(0, 0, 0), new WorldTile("grass", ImmutableArray.Create(new ItemStack("stick", 2))))
                .Add(new WorldPosition(1, 0, 0), new WorldTile("sand", ImmutableArray.Create(new ItemStack("loose_stone", 2))))
        };
        var tied = WorldReports.BuildResourceDistribution(tiedWorld);
        Assert.Equal("loose_stone", tied[0].ItemId);
        Assert.Equal("stick", tied[1].ItemId);
    }

    public static void TerrainReportGroupsAndOrdersTies()
    {
        var rows = WorldReports.BuildTerrainAndObjectStatistics(ReportWorld());
        Assert.Equal(2, rows.Count);
        Assert.Equal("grass", rows[0].TerrainId);
        Assert.Equal(2, rows[0].TileCount);
        Assert.Equal(1, rows[0].ObjectCount);
        Assert.Equal("sand", rows[1].TerrainId);
        Assert.Equal(1, rows[1].TileCount);
        Assert.Equal(1, rows[1].ObjectCount);

        var tied = ReportWorld() with
        {
            Tiles = ImmutableDictionary<WorldPosition, WorldTile>.Empty
                .Add(new WorldPosition(0, 0, 0), new WorldTile("sand", ImmutableArray<ItemStack>.Empty))
                .Add(new WorldPosition(1, 0, 0), new WorldTile("grass", ImmutableArray<ItemStack>.Empty))
        };
        var ordered = WorldReports.BuildTerrainAndObjectStatistics(tied);
        Assert.Equal("grass", ordered[0].TerrainId);
        Assert.Equal("sand", ordered[1].TerrainId);
    }

    public static void ReportsRejectNullWorld()
    {
        bool resourceThrew = false;
        bool terrainThrew = false;
        try { WorldReports.BuildResourceDistribution(null!); }
        catch (ArgumentNullException) { resourceThrew = true; }
        try { WorldReports.BuildTerrainAndObjectStatistics(null!); }
        catch (ArgumentNullException) { terrainThrew = true; }
        Assert.True(resourceThrew && terrainThrew);
    }

    public static void WorldReportsUseJsonLoadedApplicationWorld()
    {
        var definitionsResult = DefinitionLoader.Load(Path.Combine(AppContext.BaseDirectory, "Data", "Definitions"));
        Assert.True(definitionsResult is Result<GameDefinitions>.Success);
        var definitions = ((Result<GameDefinitions>.Success)definitionsResult).Value;
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "Definitions", "world.json");
        var result = WorldDataLoader.Load(path, definitions);
        Assert.True(result is Result<WorldState>.Success);
        var world = ((Result<WorldState>.Success)result).Value;
        Assert.Equal(768, world.Tiles.Count);
        Assert.Equal(8, world.Objects.Count);
        Assert.Equal(3, WorldReports.BuildResourceDistribution(world)[0].TotalQuantity);
        var terrainRows = WorldReports.BuildTerrainAndObjectStatistics(world);
        Assert.Equal(6, terrainRows.Single(row => row.TerrainId == "grass").ObjectCount);
        Assert.Equal(2, terrainRows.Single(row => row.TerrainId == "stone").ObjectCount);
    }

    public static void WorldDataLoaderRejectsMalformedAndUnknownReferences()
    {
        var definitionsResult = DefinitionLoader.Load(Path.Combine(AppContext.BaseDirectory, "Data", "Definitions"));
        Assert.True(definitionsResult is Result<GameDefinitions>.Success);
        var definitions = ((Result<GameDefinitions>.Success)definitionsResult).Value;
        var directory = Path.Combine(Path.GetTempPath(), $"survivalsim-world-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "world.json");
        try
        {
            File.WriteAllText(path, "{ malformed");
            Assert.True(WorldDataLoader.Load(path, definitions) is Result<WorldState>.Fail);
            File.WriteAllText(path, """{"playerPosition":{"x":0,"y":0,"z":0},"tiles":[{"x":0,"y":0,"z":0,"terrainId":"unknown","groundItems":[]}],"objects":[]}""");
            var result = WorldDataLoader.Load(path, definitions);
            Assert.True(result is Result<WorldState>.Fail);
            Assert.Equal("INVALID_WORLD_TILE", ((Result<WorldState>.Fail)result).Code);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public static void ActualVisiblePositionIteratorIsDeferredAndSnapshotIsReusable()
    {
        int produced = 0;
        static IEnumerable<T> Probe<T>(IEnumerable<T> source, Action onYield)
        {
            foreach (var value in source)
            {
                onYield();
                yield return value;
            }
        }

        var actualWorldIterator = new WorldPosition(5, 8, -1).GetVisiblePositions(1, 1);
        var probed = Probe(actualWorldIterator, () => produced++);
        Assert.Equal(0, produced);

        var firstTwo = probed.Take(2).ToArray();
        Assert.Equal(2, produced);
        Assert.Equal(2, firstTwo.Length);

        var snapshot = probed.ToArray();
        Assert.Equal(11, produced);
        Assert.Equal(9, snapshot.Length);
        _ = snapshot.Count();
        Assert.Equal(11, produced); // Snapshot reuse does not rerun the iterator.

        _ = probed.Count();
        Assert.Equal(20, produced); // Re-enumerating the deferred query reruns its work.
    }

    public static void NativeAdapterFailureBecomesResultFailure()
    {
        var result = NativeGatheringCalculator.CalculateWithInvoker(
            10, 100, 100,
            (_, _, _) => throw new DllNotFoundException("controlled test failure"));
        Assert.True(result is Result<int>.Fail);
        Assert.Equal("NATIVE_LIBRARY_UNAVAILABLE", ((Result<int>.Fail)result).Code);
    }

    public static void ConvenienceFileReadsReleaseHandlesAfterSuccessAndFailure()
    {
        var definitionsPath = Path.Combine(AppContext.BaseDirectory, "Data", "Definitions");
        var success = DefinitionLoader.Load(definitionsPath);
        Assert.True(success is Result<GameDefinitions>.Success);
        using (var exclusive = new FileStream(Path.Combine(definitionsPath, "items.json"),
            FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.True(exclusive.Length > 0);

        var directory = Path.Combine(Path.GetTempPath(), $"survivalsim-recipe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var malformed = Path.Combine(directory, "recipes.json");
        try
        {
            File.WriteAllText(malformed, "{ malformed");
            Assert.True(RecipeLoader.Load(malformed) is Result<ImmutableArray<CraftRecipe>>.Fail);
            using var exclusive = new FileStream(malformed, FileMode.Open, FileAccess.Read, FileShare.None);
            Assert.True(exclusive.Length > 0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
    public static void NormalWorkflowIntegratesNativeStrikeAndReports()
    {
        var definitionsResult = DefinitionLoader.Load(Path.Combine(AppContext.BaseDirectory, "Data", "Definitions"));
        Assert.True(definitionsResult is Result<GameDefinitions>.Success);
        var definitions = ((Result<GameDefinitions>.Success)definitionsResult).Value;
        var worldResult = WorldDataLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Data", "Definitions", "world.json"), definitions);
        Assert.True(worldResult is Result<WorldState>.Success);
        var world = ((Result<WorldState>.Success)worldResult).Value with
        {
            PlayerPosition = new WorldPosition(6, 4, 0)
        };
        var withoutTool = new SimulationState(
            new GameState(new Player(100, 100, 100), 480, ImmutableArray<ItemStack>.Empty), world);

        var failed = InteractionService.Strike(withoutTool, definitions, new WorldPosition(7, 4, 0), 100);
        Assert.True(failed is Result<SimulationState>.Fail);
        Assert.Equal("TOOL_REQUIRED", ((Result<SimulationState>.Fail)failed).Code);
        Assert.Equal(100, withoutTool.Survival.Player.Energy);
        Assert.True(withoutTool.World.Objects.Values.Any(o => o.Position == new WorldPosition(7, 4, 0)));

        var ready = withoutTool with
        {
            Survival = withoutTool.Survival with
            {
                Inventory = ImmutableArray.Create(new ItemStack("stone_axe", 1))
            }
        };
        var succeeded = InteractionService.Strike(ready, definitions, new WorldPosition(7, 4, 0), 100);
        Assert.True(succeeded is Result<SimulationState>.Success);
        var updated = ((Result<SimulationState>.Success)succeeded).Value;
        Assert.Equal(98, updated.Survival.Player.Energy);
        Assert.True(!updated.World.Objects.Values.Any(o => o.Position == new WorldPosition(7, 4, 0)));

        var resourceRows = WorldReports.BuildResourceDistribution(updated.World);
        var log = resourceRows.Single(row => row.ItemId == "log");
        Assert.Equal(3, log.TotalQuantity);
        var stick = resourceRows.Single(row => row.ItemId == "stick");
        Assert.Equal(4, stick.TotalQuantity); // Initial 2 plus 2 dropped by the oak tree.
    }

}
