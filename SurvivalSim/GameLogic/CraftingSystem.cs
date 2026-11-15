using System.Collections.Immutable;
using SurvivalSim.Models;

namespace SurvivalSim.GameLogic;

public static class Crafting
{
    public static bool HasIngridients(IReadOnlyList<ItemStack> inventory, CraftRecipe recipe)
    {
        return recipe.Ingredients.All(required => inventory
        .Where(item => item.ItemId == required.ItemId)
        .Sum(item => item.Quantity) >= required.Quantity);
    }
    public static ImmutableArray<ItemStack> CraftInventory(ImmutableArray<ItemStack> inventory, CraftRecipe recipe)
    {
        var quantities = inventory.ToDictionary(item => item.ItemId, item => item.Quantity);
        foreach (var ingredient in recipe.Ingredients)
        {
            quantities[ingredient.ItemId] -= ingredient.Quantity;   
        }

        quantities[recipe.ResultItemId] = quantities.GetValueOrDefault(recipe.ResultItemId) + recipe.ResultQuantity;

        return quantities.Where(entry => entry.Value > 0).Select(entry => new ItemStack(entry.Key, entry.Value)).ToImmutableArray();
    }
    public static ActionOutcome Craft(GameState state, CraftRecipe recipe, Func<int, int> hungerPolicy, Func<int, int> thirstPolicy)
    {
        if (!HasIngridients(state.Inventory, recipe))
        {
            return new ActionOutcome(false, state, "There is not enough ingredients");
        }
        var action = new ActionRequest($"Craft {recipe.ResultItemId}", recipe.DurationMinutes, recipe.EnergyCost);
        ActionOutcome outcome = Main.ExecuteAction(state, action, hungerPolicy, thirstPolicy);
        if (!outcome.Success)
        {
            return outcome;
        }
        var updatedInventory = CraftInventory(state.Inventory.ToImmutableArray(), recipe);
        GameState updated = outcome.State with
        {
            Inventory = updatedInventory
        };

        return new ActionOutcome(true, updated, $"Crafted {recipe.ResultItemId}");
    }
}
