namespace SurvivalSim.Models.World;

public sealed record WorldObject(
    Guid Id,
    string DefinitionId,
    WorldPosition Position,
    int Durability
);
