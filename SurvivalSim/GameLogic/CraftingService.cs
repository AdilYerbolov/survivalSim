using SurvivalSim.Models;
using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;
using Game = SurvivalSim.GameLogic.Main;

namespace SurvivalSim.GameLogic
{
    public static class CraftingService
    {
        public static Result<CraftRecipe> CheckCrafting(GameState state, IEnumerable<CraftRecipe> recipes, string recipeId)
        {
            return RecipeLookup.RequireRecipe(recipes, recipeId).Bind(recipe => CraftingValidation.ValidateIngredients(state, recipe));
        }
        public static Result<GameState> CraftWithResult(GameState state, IEnumerable<CraftRecipe> recipes, string recipeId, Func<int, int> hungerPolicy, Func<int, int> thirstPolicy)
        {
            return CheckCrafting(state, recipes, recipeId).Bind(recipe => ExecuteCrafting(state, recipe, hungerPolicy, thirstPolicy)); 
        }
        private static Result<GameState> ExecuteCrafting(GameState state, CraftRecipe recipe, Func<int, int> hungerPolicy, Func<int, int> thirstPolicy)
        {
            var request = new ActionRequest($"Craft {recipe.ResultItemId}", recipe.DurationMinutes, recipe.EnergyCost);
            ActionOutcome outcome = Game.ExecuteAction(state, request, hungerPolicy, thirstPolicy);

            if (!outcome.Success)
            {
                string code = outcome.Message == "Not enough energy" ? "NOT ENOUGH ENERGY" : "ACTION FAILED";

                return new Result<GameState>.Fail(code, outcome.Message);
            }

            var inventory = UpdateInventory(outcome.State.Inventory.ToImmutableArray(), recipe);
            GameState updated = outcome.State with
            {
                Inventory = inventory
            };

            return  new Result<GameState>.Success(updated);
        }
        private static ImmutableArray<ItemStack> UpdateInventory(ImmutableArray<ItemStack> inventory, CraftRecipe recipe)
        {
            var quantities = inventory.GroupBy(item => item.ItemId)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
            foreach (var ingredient in recipe.Ingredients)
            {
                quantities[ingredient.ItemId] -= ingredient.Quantity;
            }

            quantities[recipe.ResultItemId] = quantities.GetValueOrDefault(recipe.ResultItemId) + recipe.ResultQuantity;

            return quantities.Where(entry => entry.Value > 0).Select(entry => new ItemStack(entry.Key, entry.Value)).ToImmutableArray();
        }
        public static Result<string> CraftSummary(GameState state, IEnumerable<CraftRecipe> recipes,
        string recipeId, Func<int, int> hungerPolicy, Func<int, int> thirstPolicy)
        {
            return CraftWithResult(state, recipes, recipeId, hungerPolicy, thirstPolicy)
                .Map(updated => $"Crafted: {recipeId}. " +
                $"Time: {updated.TimeMinutes}, " +
                $"Energy: {updated.Player.Energy}");
        }
    }
}
