using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using SurvivalSim.Models;
using SurvivalSim.Models.World;

namespace SurvivalSim.GameLogic.World;

/// <summary>JSON persistence using arrays in the file, avoiding complex dictionary keys.</summary>
public static class WorldSaveService
{
    private const int CurrentVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private sealed record SaveDocument(
        int Version, int TimeMinutes, Player Player, List<ItemStack> Inventory,
        WorldPosition PlayerPosition, List<TileSave> Tiles, List<ObjectSave> Objects);
    private sealed record TileSave(WorldPosition Position, string TerrainId, List<ItemStack> GroundItems);
    private sealed record ObjectSave(Guid Id, string DefinitionId, WorldPosition Position, int Durability);

    public static Result<string> Save(SimulationState state, string path)
    {
        if (state is null || state.Survival is null || state.Survival.Player is null ||
            state.Survival.Inventory is null || state.World is null ||
            state.World.Tiles is null || state.World.Objects is null)
            return new Result<string>.Fail("INVALID_STATE", "The simulation state is incomplete.");
        if (string.IsNullOrWhiteSpace(path))
            return new Result<string>.Fail("INVALID_PATH", "Save path is required.");

        string? temporaryPath = null;
        try
        {
            var document = new SaveDocument(
                CurrentVersion,
                state.Survival.TimeMinutes,
                state.Survival.Player,
                state.Survival.Inventory.ToList(),
                state.World.PlayerPosition,
                state.World.Tiles.Select(pair => new TileSave(
                    pair.Key, pair.Value.TerrainId, pair.Value.GroundItems.ToList())).ToList(),
                state.World.Objects.Values.Select(obj => new ObjectSave(
                    obj.Id, obj.DefinitionId, obj.Position, obj.Durability)).ToList());

            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath)!;
            Directory.CreateDirectory(directory);
            temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, JsonOptions));
            File.Move(temporaryPath, fullPath, overwrite: true);
            temporaryPath = null;
            return new Result<string>.Success(fullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException)
        {
            return new Result<string>.Fail("SAVE_FAILED", ex.Message);
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
                catch (IOException) { /* Best-effort cleanup; preserve the original save error. */ }
                catch (UnauthorizedAccessException) { /* Best-effort cleanup. */ }
            }
        }
    }

    public static Result<SimulationState> Load(string path, GameDefinitions definitions)
    {
        if (string.IsNullOrWhiteSpace(path))
            return new Result<SimulationState>.Fail("INVALID_PATH", "Save path is required.");
        if (definitions is null)
            return new Result<SimulationState>.Fail("INVALID_DEFINITIONS", "Game definitions are required.");

        try
        {
            var json = File.ReadAllText(path);
            var document = JsonSerializer.Deserialize<SaveDocument>(json, JsonOptions);
            if (document is null)
                return new Result<SimulationState>.Fail("EMPTY_SAVE", "Save file contains no data.");
            if (document.Version != CurrentVersion)
                return new Result<SimulationState>.Fail("UNSUPPORTED_VERSION", $"Save version {document.Version} is not supported.");

            // JSON can omit non-nullable record properties or explicitly set them to null.
            // Validate every collection and element before dereferencing it.
            if (document.Player is null || document.Tiles is null || document.Objects is null ||
                document.Inventory is null || document.Tiles.Any(t => t is null || t.GroundItems is null) ||
                document.Objects.Any(o => o is null) || document.Inventory.Any(i => i is null))
                return new Result<SimulationState>.Fail("INVALID_SAVE", "Save is missing required sections or contains null entries.");

            if (document.TimeMinutes < 0 || document.Player.Energy is < 0 or > 100 ||
                document.Player.Hunger is < 0 or > 100 || document.Player.Thirst is < 0 or > 100 ||
                document.Player.Health is < 0 or > 100 || document.Player.HungerProgressMinutes < 0 ||
                document.Player.ThirstProgressMinutes < 0 || document.Player.StarvationProgressMinutes < 0)
                return new Result<SimulationState>.Fail("INVALID_SAVE", "Player statistics or time are outside valid ranges.");

            if (document.Tiles.Any(t => string.IsNullOrWhiteSpace(t.TerrainId) ||
                    !definitions.Terrain.ContainsKey(t.TerrainId) ||
                    t.GroundItems.Any(i => i.Quantity <= 0 || string.IsNullOrWhiteSpace(i.ItemId) || !definitions.Items.ContainsKey(i.ItemId))))
                return new Result<SimulationState>.Fail("INVALID_SAVE", "Save contains an unknown terrain or ground item definition.");
            if (document.Inventory.Any(i => i.Quantity <= 0 || string.IsNullOrWhiteSpace(i.ItemId) || !definitions.Items.ContainsKey(i.ItemId)))
                return new Result<SimulationState>.Fail("INVALID_SAVE", "Save contains an unknown inventory item definition.");
            if (document.Objects.Any(o => o.Id == Guid.Empty || string.IsNullOrWhiteSpace(o.DefinitionId) ||
                    !definitions.Objects.TryGetValue(o.DefinitionId, out var def) || o.Durability <= 0 || o.Durability > def.MaxDurability))
                return new Result<SimulationState>.Fail("INVALID_SAVE", "Save contains an invalid object definition, ID, or durability.");
            if (document.Tiles.Select(t => t.Position).Distinct().Count() != document.Tiles.Count ||
                document.Objects.Select(o => o.Id).Distinct().Count() != document.Objects.Count)
                return new Result<SimulationState>.Fail("INVALID_SAVE", "Save contains duplicate tile positions or object IDs.");
            var tilePositions = document.Tiles.Select(t => t.Position).ToHashSet();
            if (document.Objects.Any(o => !tilePositions.Contains(o.Position)))
                return new Result<SimulationState>.Fail("INVALID_SAVE", "An object is located on a tile that does not exist.");
            if (!tilePositions.Contains(document.PlayerPosition))
                return new Result<SimulationState>.Fail("INVALID_SAVE", "Player position does not exist in the saved world.");

            var tiles = document.Tiles.ToImmutableDictionary(
                t => t.Position,
                t => new WorldTile(t.TerrainId, t.GroundItems.ToImmutableArray()));
            var objects = document.Objects.ToImmutableDictionary(
                o => o.Id,
                o => new WorldObject(o.Id, o.DefinitionId, o.Position, o.Durability));
            var world = new WorldState(tiles, objects, document.PlayerPosition);
            var survival = new GameState(document.Player, document.TimeMinutes, document.Inventory.ToImmutableArray());
            return new Result<SimulationState>.Success(new SimulationState(survival, world));
        }
        catch (FileNotFoundException ex) { return new Result<SimulationState>.Fail("SAVE_NOT_FOUND", ex.Message); }
        catch (DirectoryNotFoundException ex) { return new Result<SimulationState>.Fail("SAVE_NOT_FOUND", ex.Message); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException or NotSupportedException or OverflowException)
        { return new Result<SimulationState>.Fail("LOAD_FAILED", ex.Message); }
    }
}
