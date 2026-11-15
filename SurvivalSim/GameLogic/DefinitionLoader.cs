using System.Collections.Immutable;
using System.Text.Json;
using SurvivalSim.Models;

namespace SurvivalSim.GameLogic;

public static class DefinitionLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public static Result<GameDefinitions> Load(string directory)
    {
        try
        {
            var items = Read<ItemDefinition>(Path.Combine(directory, "items.json"));
            var terrain = Read<TerrainDefinition>(Path.Combine(directory, "terrain.json"));
            var objects = Read<ObjectDefinition>(Path.Combine(directory, "objects.json"));
            if (items.Length == 0 || terrain.Length == 0 || objects.Length == 0)
                return Fail("EMPTY_DEFINITIONS", "Definition files must not be empty.");
            if (items.Any(i => i is null || !ValidId(i.Id) || string.IsNullOrWhiteSpace(i.Name) ||
                i.Symbol == '\0' || i.Weight < 0 || i.MaxStack <= 0 || i.Tags is null) ||
                terrain.Any(t => t is null || !ValidId(t.Id) || string.IsNullOrWhiteSpace(t.Name) ||
                t.Symbol == '\0' || t.MovementCost < 0 || (t.Walkable && t.MovementCost == 0)) ||
                objects.Any(o => o is null || !ValidId(o.Id) || string.IsNullOrWhiteSpace(o.Name) ||
                o.Symbol == '\0' || o.MaxDurability <= 0 || o.Drops is null ||
                o.Drops.Any(d => d is null || !ValidId(d.ItemId) || d.Quantity <= 0)))
                return Fail("INVALID_DEFINITION", "One or more definitions contain invalid fields.");
            if (Duplicate(items.Select(i => i.Id)) || Duplicate(terrain.Select(t => t.Id)) ||
                Duplicate(objects.Select(o => o.Id)))
                return Fail("DUPLICATE_ID", "Duplicate definition IDs found.");
            var itemMap = items.ToImmutableDictionary(i => i.Id, StringComparer.Ordinal);
            var terrainMap = terrain.ToImmutableDictionary(t => t.Id, StringComparer.Ordinal);
            var objectMap = objects.ToImmutableDictionary(o => o.Id, StringComparer.Ordinal);
            if (objects.Any(o => o.Drops.Any(d => !itemMap.ContainsKey(d.ItemId))))
                return Fail("UNKNOWN_DROP", "An object drops an item that is not defined.");
            if (objects.Any(o => o.RequiredToolTag is not null &&
                !items.Any(i => i.Tags.Contains(o.RequiredToolTag, StringComparer.Ordinal))))
                return Fail("UNKNOWN_TOOL_TAG", "An object requires a tool tag not defined by any item.");
            return new Result<GameDefinitions>.Success(new GameDefinitions(itemMap, terrainMap, objectMap));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or
            ArgumentException or NotSupportedException)
        {
            return Fail("DEFINITION_LOAD_ERROR", ex.Message);
        }
    }

    private static T[] Read<T>(string path) =>
        JsonSerializer.Deserialize<T[]>(File.ReadAllText(path), Options)
        ?? throw new JsonException($"Null definition list in {path}");
    private static bool ValidId(string? id) => !string.IsNullOrWhiteSpace(id);
    private static bool Duplicate(IEnumerable<string> ids) =>
        ids.Distinct(StringComparer.Ordinal).Count() != ids.Count();
    private static Result<GameDefinitions> Fail(string code, string message) =>
        new Result<GameDefinitions>.Fail(code, message);
}
