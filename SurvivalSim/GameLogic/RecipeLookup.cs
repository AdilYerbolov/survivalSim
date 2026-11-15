using SurvivalSim.Models;

namespace SurvivalSim.GameLogic;

public static class RecipeLookup
{
    public static Option<CraftRecipe> FindRecipe(IEnumerable<CraftRecipe> recipes, string recipeId)
    {
        CraftRecipe? recipe = recipes.FirstOrDefault(r => r.Id == recipeId);

        return recipe is null ? new Option<CraftRecipe>.None() : new Option<CraftRecipe>.Some(recipe);
    }
    public static Result<CraftRecipe> RequireRecipe(IEnumerable<CraftRecipe> recipes, string recipeId)
    {
        return FindRecipe(recipes, recipeId) switch
        {
            Option<CraftRecipe>.Some found => new Result<CraftRecipe>.Success(found.Value),
            Option<CraftRecipe>.None => new Result<CraftRecipe>.Fail("RECIPE NOT FOUND", $"Recipe '{recipeId}' does not exist"),
            _ => throw new InvalidOperationException("Unknown lookup result")
        };
    }
}