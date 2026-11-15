using System.Collections.Immutable;
using SurvivalSim.Models;
using SurvivalSim.Models.World;
using Game = SurvivalSim.GameLogic.Main;

namespace SurvivalSim.GameLogic.World;

/// <summary>Creates real world objects atomically at an adjacent tile.</summary>
public static class BuildingService
{
    public static Result<SimulationState> Build(SimulationState state, GameDefinitions definitions,
        BuildingRecipe recipe, WorldPosition target)
    {
        if (recipe is null) return Fail("INVALID_RECIPE", "A building recipe is required.");
        if (!definitions.Objects.TryGetValue(recipe.ObjectDefinitionId, out var objectDefinition))
            return Fail("UNKNOWN_OBJECT", $"Unknown building: {recipe.ObjectDefinitionId}");
        var player = state.World.PlayerPosition;
        if (target.Z != player.Z || Math.Abs((long)target.X - player.X) + Math.Abs((long)target.Y - player.Y) != 1)
            return Fail("NOT_ADJACENT", "Choose one of the four tiles next to your character.");
        if (!state.World.Tiles.TryGetValue(target, out var tile)) return Fail("OUT_OF_WORLD", "That tile is outside the world.");
        if (state.World.Objects.Values.Any(o => o.Position == target)) return Fail("TILE_OCCUPIED", "There is already an object on that tile.");
        if (!definitions.Terrain.TryGetValue(tile.TerrainId, out var terrain) || !terrain.Walkable)
            return Fail("BLOCKED_TERRAIN", "You cannot build on this terrain.");
        if (recipe.DurationMinutes <= 0 || recipe.EnergyCost is < 0 or > 100 || recipe.Ingredients is null ||
            recipe.Ingredients.Any(i => i.Quantity <= 0 || !definitions.Items.ContainsKey(i.ItemId)))
            return Fail("INVALID_RECIPE", "The building recipe is invalid.");
        foreach (var ingredient in recipe.Ingredients.GroupBy(i => i.ItemId))
        {
            long required = ingredient.Sum(i => (long)i.Quantity);
            long available = state.Survival.Inventory.Where(i => i.ItemId == ingredient.Key).Sum(i => (long)i.Quantity);
            if (available < required) return Fail("MISSING_INGREDIENTS", $"Need {required} x {ingredient.Key}.");
        }
        var outcome = Game.ExecuteAction(state.Survival,
            new ActionRequest("Build " + objectDefinition.Name, recipe.DurationMinutes, recipe.EnergyCost),
            Game.normalHunger, Game.normalThirst);
        if (!outcome.Success) return Fail("ACTION_FAILED", outcome.Message);

        var inventory = outcome.State.Inventory.GroupBy(i => i.ItemId)
            .ToDictionary(group => group.Key, group => group.Sum(i => i.Quantity));
        foreach (var ingredient in recipe.Ingredients) inventory[ingredient.ItemId] -= ingredient.Quantity;
        var updatedInventory = inventory.Where(p => p.Value > 0)
            .Select(p => new ItemStack(p.Key, p.Value)).ToImmutableArray();
        var built = new WorldObject(Guid.NewGuid(), recipe.ObjectDefinitionId, target, objectDefinition.MaxDurability);
        return new Result<SimulationState>.Success(state with
        {
            Survival = outcome.State with { Inventory = updatedInventory },
            World = state.World with { Objects = state.World.Objects.Add(built.Id, built) }
        });
    }

    private static Result<SimulationState> Fail(string code, string message) => new Result<SimulationState>.Fail(code, message);
}
