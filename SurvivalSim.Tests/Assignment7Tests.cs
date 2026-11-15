
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using SurvivalSim.Models;
using SurvivalSim.GameLogic;
using Game = SurvivalSim.GameLogic.Main;

public static class Assignment7Tests
{
    private static GameState Start(
        int energy = 100,
        params ItemStack[] items)
    {
        return new GameState(
            new Player(energy, 100, 100),
            480,
            items.ToImmutableArray()
        );
    }

    private static CraftRecipe StoneBlade() => new(
        "stone_blade_recipe",
        "stone_blade",
        1,
        new List<ItemStack>
        {
            new("loose_stone", 2)
        },
        15,
        5
    );

    private static Result<GameState> Craft(
        GameState state,
        CraftRecipe? recipe = null)
    {
        var recipes = new[] { recipe ?? StoneBlade() };

        return CraftingService.CraftWithResult(
            state,
            recipes,
            recipes[0].Id,
            Game.normalHunger,
            Game.normalThirst
        );
    }

    private static int Quantity(GameState state, string id) =>
        state.Inventory
            .Where(i => i.ItemId == id)
            .Sum(i => i.Quantity);

    private static void AssertFailure<T>(
        Result<T> result,
        string expectedCode)
    {
        Assert.True(result is Result<T>.Fail);

        if (result is Result<T>.Fail failure)
            Assert.Equal(expectedCode, failure.Code);
    }

    // 1. Successful crafting
    public static void SuccessfulCrafting()
    {
        var state = Start(100, new ItemStack("loose_stone", 5));

        var result = Craft(state);

        Assert.True(result is Result<GameState>.Success);

        var updated = ((Result<GameState>.Success)result).Value;

        Assert.Equal(3, Quantity(updated, "loose_stone"));
        Assert.Equal(1, Quantity(updated, "stone_blade"));
        Assert.Equal(95, updated.Player.Energy);
        Assert.Equal(495, updated.TimeMinutes);
    }

    // 2. Invalid quantity
    public static void InvalidQuantity()
    {
        var recipe = StoneBlade() with
        {
            ResultQuantity = 0
        };

        var result = Craft(
            Start(100, new ItemStack("loose_stone", 5)),
            recipe
        );

        AssertFailure(result, "INVALID QUANTITY");
    }

    // 3. Missing ingredient
    public static void MissingIngredient()
    {
        var state = Start(100, new ItemStack("loose_stone", 1));

        var result = Craft(state);

        AssertFailure(result, "MISSING_INGREDIENT");
    }

    // 4. Insufficient energy
    public static void InsufficientEnergy()
    {
        var state = Start(4, new ItemStack("loose_stone", 5));

        var result = Craft(state);

        AssertFailure(result, "NOT ENOUGH ENERGY");
    }

    // 5. Existing recipe lookup
    public static void RecipeFound()
    {
        var recipe = StoneBlade();

        var result = RecipeLookup.FindRecipe(
            new[] { recipe },
            recipe.Id
        );

        Assert.True(result is Option<CraftRecipe>.Some);
    }

    // 6. Missing recipe lookup
    public static void RecipeMissing()
    {
        var result = RecipeLookup.FindRecipe(
            new[] { StoneBlade() },
            "nonexistent_recipe"
        );

        Assert.True(result is Option<CraftRecipe>.None);
    }

    // 7. Map transforms successful values
    public static void MapSuccess()
    {
        Result<int> input = new Result<int>.Success(10);

        var result = input.Map(x => x * 2);

        Assert.True(result is Result<int>.Success);

        var value = ((Result<int>.Success)result).Value;
        Assert.Equal(20, value);
    }

    // 8. Bind continues successful computations
    public static void BindSuccess()
    {
        Result<int> input = new Result<int>.Success(10);

        var result = input.Bind(x =>
            (Result<int>)new Result<int>.Success(x + 5)
        );

        Assert.True(result is Result<int>.Success);

        var value = ((Result<int>.Success)result).Value;
        Assert.Equal(15, value);
    }

    // 9. Map must not execute after failure
    public static void MapFailurePropagation()
    {
        Result<int> input = new Result<int>.Fail(
            "TEST_ERROR",
            "Original failure"
        );

        bool called = false;

        var result = input.Map(x =>
        {
            called = true;
            return x * 2;
        });

        Assert.True(!called);
        AssertFailure(result, "TEST_ERROR");
    }

    // 10. Bind must not execute after failure
    public static void BindFailurePropagation()
    {
        Result<int> input = new Result<int>.Fail(
            "TEST_ERROR",
            "Original failure"
        );

        bool called = false;

        var result = input.Bind(x =>
        {
            called = true;
            return (Result<int>)new Result<int>.Success(x + 5);
        });

        Assert.True(!called);
        AssertFailure(result, "TEST_ERROR");
    }

    // 11. Missing JSON file
    public static void MissingJsonFile()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"),
            "recipes.json"
        );

        var result = RecipeLoader.Load(path);

        AssertFailure(result, "FILE NOT FOUND");
    }

    // 12. Malformed JSON
    public static void MalformedJson()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, "{ invalid json");

            var result = RecipeLoader.Load(path);

            AssertFailure(result, "INVALID JSON");
        }
        finally
        {
            File.Delete(path);
        }
    }

    // 13. Valid JSON loading
    public static void ValidJson()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, """
            [
              {
                "id": "stone_blade_recipe",
                "resultItemId": "stone_blade",
                "resultQuantity": 1,
                "ingredients": [
                  { "itemId": "loose_stone", "quantity": 2 }
                ],
                "durationMinutes": 15,
                "energyCost": 5
              }
            ]
            """);

            var result = RecipeLoader.Load(path);

            Assert.True(result is
                Result<ImmutableArray<CraftRecipe>>.Success);

            var recipes =
                ((Result<ImmutableArray<CraftRecipe>>.Success)result).Value;

            Assert.Equal(1, recipes.Length);
            Assert.Equal("stone_blade_recipe", recipes[0].Id);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // 14. Failed crafting must preserve state
    public static void FailurePreservesState()
    {
        var state = Start(
            100,
            new ItemStack("loose_stone", 1)
        );

        var result = Craft(state);

        Assert.True(result is Result<GameState>.Fail);

        Assert.Equal(1, Quantity(state, "loose_stone"));
        Assert.Equal(0, Quantity(state, "stone_blade"));
        Assert.Equal(100, state.Player.Energy);
        Assert.Equal(480, state.TimeMinutes);
    }

    // 15. Duplicate ingredients must be combined
    public static void DuplicateIngredients()
    {
        var recipe = StoneBlade() with
        {
            Ingredients = new List<ItemStack>
            {
                new("loose_stone", 2),
                new("loose_stone", 2)
            }
        };

        var result = Craft(
            Start(100, new ItemStack("loose_stone", 3)),
            recipe
        );

        AssertFailure(result, "MISSING_INGREDIENT");
    }

    // 16. Missing recipe returns explicit failure
    public static void MissingRecipeResult()
    {
        var result = CraftingService.CraftWithResult(
            Start(100, new ItemStack("loose_stone", 5)),
            new[] { StoneBlade() },
            "unknown_recipe",
            Game.normalHunger,
            Game.normalThirst
        );

        AssertFailure(result, "RECIPE NOT FOUND");
    }
}
