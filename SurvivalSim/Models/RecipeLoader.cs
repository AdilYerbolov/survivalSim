using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using SurvivalSim.Models;


namespace SurvivalSim.GameLogic;

public static class RecipeLoader
{
    public static Result<ImmutableArray<CraftRecipe>> Load(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            
            var recipes = JsonSerializer.Deserialize<CraftRecipe[]>(json, new JsonSerializerOptions{PropertyNameCaseInsensitive = true});
            

            if (recipes is null || recipes.Length == 0)
            {
                return new Result<ImmutableArray<CraftRecipe>>.Fail("INVALID_RECIPE_DATA",
                $"Recipe file '{path}' contains no recipes");
            }
            if (recipes.Any(r =>
            r is null ||
            string.IsNullOrWhiteSpace(r.Id) ||
            string.IsNullOrWhiteSpace(r.ResultItemId) ||
            r.ResultQuantity <= 0 ||
            r.DurationMinutes <= 0 ||
            r.EnergyCost < 0 ||
            r.EnergyCost > 100 ||
            r.Ingredients is null ||
            r.Ingredients.Any(i =>
                i is null ||
                string.IsNullOrWhiteSpace(i.ItemId) ||
                i.Quantity <= 0)))
            {
                return new Result<ImmutableArray<CraftRecipe>>.Fail("INVALID RECIPE DATA", $"Recipe file '{path}' contains invalid recipes");
            }
            if (recipes.Select(r => r.Id).Distinct().Count() != recipes.Length)
            {
                return new Result<ImmutableArray<CraftRecipe>>.Fail("DUPLICATE RECIPE", $"Recipe file '{path}' contains multiple Ids");
            }

            return new Result<ImmutableArray<CraftRecipe>>.Success(recipes.ToImmutableArray());
        }
        catch (FileNotFoundException)
        {
            return new Result<ImmutableArray<CraftRecipe>>.Fail("FILE NOT FOUND", $"Recipe file '{path}' was not found");
        }
        catch (DirectoryNotFoundException)
        {
            return new Result<ImmutableArray<CraftRecipe>>.Fail("FILE NOT FOUND", $"Recipe file '{path}' was not found");
        }
        catch (JsonException ex)
        {
            return new Result<ImmutableArray<CraftRecipe>>.Fail("INVALID JSON", $"Cannot parse '{path}': {ex.Message}");
        }
        catch (IOException ex)
        {
            return new Result<ImmutableArray<CraftRecipe>>.Fail("FILE IO ERROR", $"Cannot read '{path}': {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return new Result<ImmutableArray<CraftRecipe>>.Fail("FILE_ACCESS_DENIED", $"Cannot access '{path}': {ex.Message}");
        }
    }
}
