using System.Collections.Immutable;

namespace SurvivalSim.Models.World;

public sealed record WorldState(
    ImmutableDictionary<WorldPosition, WorldTile> Tiles,
    ImmutableDictionary<Guid, WorldObject> Objects,
    WorldPosition PlayerPosition
);

public sealed record SimulationState(
    GameState Survival,
    WorldState World
);
