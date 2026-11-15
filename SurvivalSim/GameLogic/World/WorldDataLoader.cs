using System.Collections.Immutable;
using System.Text.Json;
using SurvivalSim.Models;
using SurvivalSim.Models.World;

namespace SurvivalSim.GameLogic.World;

/// <summary>Loads the playable world template from JSON and validates all definition references.</summary>
public static class WorldDataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static Result<WorldState> Load(string path, GameDefinitions definitions)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Fail("INVALID_PATH", "World data path cannot be empty.");
        if (definitions is null)
            return Fail("INVALID_DEFINITIONS", "Game definitions are required.");

        try
        {
            var document = JsonSerializer.Deserialize<WorldDocument>(File.ReadAllText(path), JsonOptions);
            if (document?.PlayerPosition is null || document.Tiles is null || document.Objects is null)
                return Fail("INVALID_WORLD_DATA", "World JSON is missing required sections.");

            var tiles = ImmutableDictionary.CreateBuilder<WorldPosition, WorldTile>();
            foreach (var tile in document.Tiles)
            {
                if (tile is null || tile.GroundItems is null || string.IsNullOrWhiteSpace(tile.TerrainId) ||
                    !definitions.Terrain.ContainsKey(tile.TerrainId))
                    return Fail("INVALID_WORLD_TILE", "A world tile is null or references unknown terrain.");

                var terrainId = tile.TerrainId;
                var position = new WorldPosition(tile.X, tile.Y, tile.Z);
                var groundItems = ImmutableArray.CreateBuilder<ItemStack>();
                foreach (var item in tile.GroundItems)
                {
                    if (item is null || string.IsNullOrWhiteSpace(item.ItemId) || item.Quantity <= 0 ||
                        !definitions.Items.ContainsKey(item.ItemId))
                        return Fail("INVALID_GROUND_ITEM", "A ground item is invalid or references an unknown item.");
                    groundItems.Add(new ItemStack(item.ItemId, item.Quantity));
                }

                if (tiles.ContainsKey(position))
                    return Fail("DUPLICATE_WORLD_TILE", $"World tile {position} appears more than once.");
                tiles.Add(position, new WorldTile(terrainId, groundItems.ToImmutable()));
            }

            var objects = ImmutableDictionary.CreateBuilder<Guid, WorldObject>();
            foreach (var item in document.Objects)
            {
                if (item is null || !Guid.TryParse(item.Id, out var id) ||
                    string.IsNullOrWhiteSpace(item.DefinitionId) ||
                    !definitions.Objects.TryGetValue(item.DefinitionId, out var definition) ||
                    item.Durability <= 0 || item.Durability > definition.MaxDurability)
                    return Fail("INVALID_WORLD_OBJECT", "A world object has invalid identity, definition, or durability.");

                var position = new WorldPosition(item.X, item.Y, item.Z);
                if (!tiles.ContainsKey(position))
                    return Fail("OBJECT_WITHOUT_TILE", $"Object {id} is not placed on an existing tile.");
                if (objects.ContainsKey(id))
                    return Fail("DUPLICATE_WORLD_OBJECT", $"Object ID {id} appears more than once.");
                objects.Add(id, new WorldObject(id, item.DefinitionId, position, item.Durability));
            }

            var playerPosition = new WorldPosition(
                document.PlayerPosition.X, document.PlayerPosition.Y, document.PlayerPosition.Z);
            if (!tiles.ContainsKey(playerPosition))
                return Fail("PLAYER_WITHOUT_TILE", "The player must start on an existing tile.");

            return new Result<WorldState>.Success(
                new WorldState(tiles.ToImmutable(), objects.ToImmutable(), playerPosition));
        }
        catch (FileNotFoundException ex)
        {
            return Fail("WORLD_FILE_NOT_FOUND", ex.Message);
        }
        catch (DirectoryNotFoundException ex)
        {
            return Fail("WORLD_FILE_NOT_FOUND", ex.Message);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or
            ArgumentException or NotSupportedException)
        {
            return Fail("WORLD_DATA_LOAD_ERROR", ex.Message);
        }
    }

    private static Result<WorldState> Fail(string code, string message) =>
        new Result<WorldState>.Fail(code, message);

    public sealed class WorldDocument
    {
        public WorldDocument() { }
        public PositionData? PlayerPosition { get; set; }
        public TileData[]? Tiles { get; set; }
        public ObjectData[]? Objects { get; set; }
    }

    public sealed class PositionData { public PositionData() { } public int X { get; set; } public int Y { get; set; } public int Z { get; set; } }
    public sealed class TileData
    {
        public TileData() { }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public string? TerrainId { get; set; }
        public GroundItemData[]? GroundItems { get; set; }
    }
    public sealed class GroundItemData { public GroundItemData() { } public string ItemId { get; set; } = string.Empty; public int Quantity { get; set; } }
    public sealed class ObjectData
    {
        public ObjectData() { }
        public string Id { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int Durability { get; set; }
    }
}
