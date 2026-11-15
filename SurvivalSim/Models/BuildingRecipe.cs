namespace SurvivalSim.Models;

public sealed record BuildingRecipe(
    string Id,
    string ObjectDefinitionId,
    IReadOnlyList<ItemStack> Ingredients,
    int DurationMinutes,
    int EnergyCost
);
