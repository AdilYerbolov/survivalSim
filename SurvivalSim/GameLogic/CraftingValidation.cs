using SurvivalSim.Models;

namespace SurvivalSim.GameLogic;

public static class CraftingValidation
{
    public static Result<CraftRecipe> ValidateIngredients(GameState state, CraftRecipe recipe)
    {
        if (recipe.ResultQuantity <= 0 || recipe.Ingredients.Any(i => i.Quantity <= 0))
        {
            return new Result<CraftRecipe>.Fail("INVALID QUANTITY", "Recipe quantities must be positive");
        }
        if (string.IsNullOrEmpty(recipe.ResultItemId))
        {
            return new Result<CraftRecipe>.Fail("EMPTY RESULT ID", "Result id must not be empty");
        }
        if (recipe.DurationMinutes <= 0)
        {
            return new Result<CraftRecipe>.Fail("INVALID DURATION", "Duration must be positive");
        }
        if (string.IsNullOrEmpty(recipe.Id))
        {
            return new Result<CraftRecipe>.Fail("ID IS EMPTY", "Id must not be empty");
        }
        if (recipe.EnergyCost < 0 || recipe.EnergyCost > 100)
        {
            return new Result<CraftRecipe>.Fail("INVALID ENERGY COST", "Energy cost must be between 0 and 100");
        }
        foreach (var ingredient in recipe.Ingredients.GroupBy(i => i.ItemId))
        {
            int required = ingredient.Sum(i => i.Quantity);

            int available = state.Inventory
                .Where(i => i.ItemId == ingredient.Key)
                .Sum(i => i.Quantity);

            if (available < required)
            {
                return new Result<CraftRecipe>.Fail(
                    "MISSING_INGREDIENT",
                    $"Required {required} of '{ingredient.Key}', " +
                    $"but only {available} available."
                );
            }
        }
        return new Result<CraftRecipe>.Success(recipe);
    }
}
