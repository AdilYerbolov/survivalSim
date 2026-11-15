using System.Collections.Immutable;
using SurvivalSim.GameLogic;
using SurvivalSim.GameLogic.World;
using SurvivalSim.Models;
using SurvivalSim.Models.World;
using Game = SurvivalSim.GameLogic.Main;

public static class SurvivalImprovementsTests
{
    public static void HungerAccumulatesAcrossShortActions()
    {
        var state = new GameState(new Player(100, 100, 100), 480, ImmutableArray<ItemStack>.Empty);
        for (int i = 0; i < 18; i++)
        {
            var outcome = Game.ExecuteAction(state, new ActionRequest("Wait", 10, 0), Game.normalHunger, Game.normalThirst);
            Assert.True(outcome.Success);
            state = outcome.State;
        }
        Assert.Equal(99, state.Player.Hunger); // 180 minutes in total, even though each action is only 10 minutes.
        Assert.Equal(97, state.Player.Thirst); // 180 minutes / 60-minute thirst interval.
    }

    public static void BuildingRecipesLoadFromJson()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Data", "Definitions", "building_recipes.json");
        var result = BuildingRecipeLoader.Load(path);
        Assert.True(result is Result<IReadOnlyList<BuildingRecipe>>.Success);
        var recipes = ((Result<IReadOnlyList<BuildingRecipe>>.Success)result).Value;
        Assert.True(recipes.Count >= 2);
        Assert.True(recipes.Any(recipe => recipe.ObjectDefinitionId == "wooden_wall"));
    }

    public static void BuildingConsumesMaterialsAndPlacesObject()
    {
        string data = Path.Combine(AppContext.BaseDirectory, "Data", "Definitions");
        var definitionsResult = DefinitionLoader.Load(data);
        Assert.True(definitionsResult is Result<GameDefinitions>.Success);
        var definitions = ((Result<GameDefinitions>.Success)definitionsResult).Value;
        var worldResult = WorldDataLoader.Load(Path.Combine(data, "world.json"), definitions);
        Assert.True(worldResult is Result<WorldState>.Success);
        var world = ((Result<WorldState>.Success)worldResult).Value;
        var player = world.PlayerPosition;
        var target = player with { X = player.X + 1 };
        var tile = world.Tiles[target];
        world = world with { Tiles = world.Tiles.SetItem(target, tile with { GroundItems = ImmutableArray<ItemStack>.Empty }) };
        var state = new SimulationState(
            new GameState(new Player(100, 100, 100), 480, ImmutableArray.Create(new ItemStack("log", 2))), world);
        var recipe = new BuildingRecipe("test_wall", "wooden_wall",
            new[] { new ItemStack("log", 2) }, 30, 5);
        var result = BuildingService.Build(state, definitions, recipe, target);
        Assert.True(result is Result<SimulationState>.Success);
        var updated = ((Result<SimulationState>.Success)result).Value;
        Assert.True(updated.World.Objects.Values.Any(o => o.Position == target && o.DefinitionId == "wooden_wall"));
        Assert.Equal(0, updated.Survival.Inventory.Sum(i => i.Quantity));
        Assert.Equal(510, updated.Survival.TimeMinutes);
    }
}
