using System.Text.Json;
using SurvivalSim.Models;

namespace SurvivalSim.GameLogic;

public static class BuildingRecipeLoader
{
    public static Result<IReadOnlyList<BuildingRecipe>> Load(string path)
    {
        try
        {
            var recipes = JsonSerializer.Deserialize<List<BuildingRecipe>>(File.ReadAllText(path),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (recipes is null || recipes.Count == 0)
                return new Result<IReadOnlyList<BuildingRecipe>>.Fail("EMPTY_BUILDING_RECIPES", "No building recipes were found.");
            if (recipes.Any(r => string.IsNullOrWhiteSpace(r.Id) || string.IsNullOrWhiteSpace(r.ObjectDefinitionId) ||
                r.DurationMinutes <= 0 || r.EnergyCost is < 0 or > 100 || r.Ingredients is null ||
                r.Ingredients.Count == 0 || r.Ingredients.Any(i => string.IsNullOrWhiteSpace(i.ItemId) || i.Quantity <= 0)))
                return new Result<IReadOnlyList<BuildingRecipe>>.Fail("INVALID_BUILDING_RECIPE", "A building recipe contains invalid data.");
            if (recipes.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count() != recipes.Count)
                return new Result<IReadOnlyList<BuildingRecipe>>.Fail("DUPLICATE_BUILDING_RECIPE", "Building recipe IDs must be unique.");
            return new Result<IReadOnlyList<BuildingRecipe>>.Success(recipes);
        }
        catch (FileNotFoundException ex) { return new Result<IReadOnlyList<BuildingRecipe>>.Fail("FILE_NOT_FOUND", ex.Message); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { return new Result<IReadOnlyList<BuildingRecipe>>.Fail("BUILDING_RECIPE_LOAD_FAILED", ex.Message); }
    }
}
