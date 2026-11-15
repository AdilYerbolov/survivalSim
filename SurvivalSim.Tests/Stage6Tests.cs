using System.Collections.Immutable;
using SurvivalSim.GameLogic;
using SurvivalSim.GameLogic.World;
using SurvivalSim.Models;
using SurvivalSim.Models.World;

public static class Stage6Tests
{
    private static (SimulationState State, GameDefinitions Definitions) Setup()
    {
        var loaded = DefinitionLoader.Load(Path.Combine(AppContext.BaseDirectory, "Data", "Definitions"));
        if (loaded is not Result<GameDefinitions>.Success success)
            throw new Exception("Definitions could not be loaded for Stage 6 tests.");
        var state = new SimulationState(
            new GameState(new Player(90, 90, 90), 500, ImmutableArray<ItemStack>.Empty),
            WorldGenerator.CreateTestWorld());
        return (state, success.Value);
    }

    public static void MalformedSaveReturnsFailure()
    {
        var (_, definitions) = Setup();
        var path = CreateTemporarySave("{ this is not valid JSON");
        try
        {
            var result = WorldSaveService.Load(path, definitions);
            Assert.True(result is Result<SimulationState>.Fail);
            Assert.Equal("LOAD_FAILED", ((Result<SimulationState>.Fail)result).Code);
        }
        finally { DeleteTemporarySave(path); }
    }

    public static void SaveWithMissingSectionsReturnsFailure()
    {
        var (_, definitions) = Setup();
        var path = CreateTemporarySave("{\"Version\":1,\"TimeMinutes\":0,\"Player\":null,\"Inventory\":null,\"Tiles\":null,\"Objects\":null}");
        try
        {
            var result = WorldSaveService.Load(path, definitions);
            Assert.True(result is Result<SimulationState>.Fail);
            Assert.Equal("INVALID_SAVE", ((Result<SimulationState>.Fail)result).Code);
        }
        finally { DeleteTemporarySave(path); }
    }

    public static void UnknownItemReferenceIsRejected()
    {
        var (_, definitions) = Setup();
        var path = CreateTemporarySave("""
        {
          "Version": 1,
          "TimeMinutes": 0,
          "Player": { "Energy": 100, "Hunger": 100, "Thirst": 100 },
          "Inventory": [{ "ItemId": "does_not_exist", "Quantity": 1 }],
          "PlayerPosition": { "X": 0, "Y": 0, "Z": 0 },
          "Tiles": [{ "Position": { "X": 0, "Y": 0, "Z": 0 }, "TerrainId": "grass", "GroundItems": [] }],
          "Objects": []
        }
        """);
        try
        {
            var result = WorldSaveService.Load(path, definitions);
            Assert.True(result is Result<SimulationState>.Fail);
            Assert.Equal("INVALID_SAVE", ((Result<SimulationState>.Fail)result).Code);
        }
        finally { DeleteTemporarySave(path); }
    }

    public static void SaveRejectsIncompleteState()
    {
        var result = WorldSaveService.Save(null!, Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"));
        Assert.True(result is Result<string>.Fail);
        Assert.Equal("INVALID_STATE", ((Result<string>.Fail)result).Code);
    }

    public static void FailedLoadDoesNotCreateOrMutateState()
    {
        var (state, definitions) = Setup();
        var originalPosition = state.World.PlayerPosition;
        var path = CreateTemporarySave("not-json");
        try
        {
            var result = WorldSaveService.Load(path, definitions);
            Assert.True(result is Result<SimulationState>.Fail);
            Assert.Equal(originalPosition, state.World.PlayerPosition);
            Assert.Equal(500, state.Survival.TimeMinutes);
        }
        finally { DeleteTemporarySave(path); }
    }

    private static string CreateTemporarySave(string contents)
    {
        var path = Path.Combine(Path.GetTempPath(), $"survivalsim-stage6-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, contents);
        return path;
    }

    private static void DeleteTemporarySave(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
