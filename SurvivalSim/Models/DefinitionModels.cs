using System.Collections.Immutable;

namespace SurvivalSim.Models;

public sealed record ItemDefinition(string Id, string Name, char Symbol, ConsoleColor Color,
    decimal Weight, int MaxStack, IReadOnlyList<string> Tags);
public sealed record TerrainDefinition(string Id, string Name, char Symbol, ConsoleColor Color,
    bool Walkable, int MovementCost);
public sealed record DropDefinition(string ItemId, int Quantity);
public sealed record ObjectDefinition(string Id, string Name, char Symbol, ConsoleColor Color,
    bool BlocksMovement, int MaxDurability, string? RequiredToolTag,
    IReadOnlyList<DropDefinition> Drops);
public sealed record GameDefinitions(
    ImmutableDictionary<string, ItemDefinition> Items,
    ImmutableDictionary<string, TerrainDefinition> Terrain,
    ImmutableDictionary<string, ObjectDefinition> Objects);
