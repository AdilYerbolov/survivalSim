using System.Collections.Immutable;

namespace SurvivalSim.Models 
{
    public sealed record Item(
        string Id,
        string Name
    );
    public sealed record ItemStack(
        string ItemId,
        int Quantity
    );
    public sealed record CraftRecipe(
        string Id,
        string ResultItemId,
        int ResultQuantity,
        IReadOnlyList<ItemStack> Ingredients,
        int DurationMinutes,
        int EnergyCost
    );
}
