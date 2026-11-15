using System.Collections.Immutable;
using SurvivalSim.Models;
using SurvivalSim.Models.World;
using Game = SurvivalSim.GameLogic.Main;

namespace SurvivalSim.GameLogic.World;

/// <summary>Pure, atomic transitions for items and adjacent object interactions.</summary>
public static class InteractionService
{
    public static Result<string> Inspect(SimulationState state, GameDefinitions definitions, WorldPosition position)
    {
        if (!state.World.Tiles.TryGetValue(position, out var tile))
            return new Result<string>.Fail("NO_TILE", "That position is outside the world.");
        var objects = state.World.Objects.Values.Where(o => o.Position == position)
            .OrderBy(o => o.Id).Select(o =>
                definitions.Objects.TryGetValue(o.DefinitionId, out var d)
                    ? $"{d.Name} ({o.Durability}/{d.MaxDurability} HP)" : $"Unknown: {o.DefinitionId}");
        var items = tile.GroundItems.Select(i => $"{i.ItemId} x{i.Quantity}");
        return new Result<string>.Success($"{position}: {tile.TerrainId}; " +
            string.Join("; ", objects.Concat(items).DefaultIfEmpty("nothing here")));
    }

    public static Result<SimulationState> Pickup(SimulationState state, GameDefinitions definitions, string itemId, int quantity)
    {
        if (quantity <= 0) return Fail("INVALID_QUANTITY", "Quantity must be positive.");
        if (!definitions.Items.ContainsKey(itemId)) return Fail("UNKNOWN_ITEM", itemId);
        var pos = state.World.PlayerPosition;
        if (!state.World.Tiles.TryGetValue(pos, out var tile)) return Fail("NO_TILE", "No tile here.");
        var available = tile.GroundItems.Where(i => i.ItemId == itemId).Sum(i => (long)i.Quantity);
        if (available < quantity) return Fail("NOT_ENOUGH_ITEMS", "Not enough items on the ground.");
        var updatedGround = ChangeQuantity(tile.GroundItems, itemId, -quantity);
        var updatedInventory = ChangeQuantity(state.Survival.Inventory, itemId, quantity);
        return new Result<SimulationState>.Success(state with
        {
            Survival = state.Survival with { Inventory = updatedInventory },
            World = state.World with { Tiles = state.World.Tiles.SetItem(pos, tile with { GroundItems = updatedGround }) }
        });
    }

    public static Result<SimulationState> Drop(SimulationState state, GameDefinitions definitions, string itemId, int quantity)
    {
        if (quantity <= 0) return Fail("INVALID_QUANTITY", "Quantity must be positive.");
        if (!definitions.Items.ContainsKey(itemId)) return Fail("UNKNOWN_ITEM", itemId);
        var available = state.Survival.Inventory.Where(i => i.ItemId == itemId).Sum(i => (long)i.Quantity);
        if (available < quantity) return Fail("NOT_ENOUGH_ITEMS", "Not enough items in inventory.");
        var pos = state.World.PlayerPosition;
        if (!state.World.Tiles.TryGetValue(pos, out var tile)) return Fail("NO_TILE", "No tile here.");
        var ground = ChangeQuantity(tile.GroundItems, itemId, quantity);
        var inventory = ChangeQuantity(state.Survival.Inventory, itemId, -quantity);
        return new Result<SimulationState>.Success(state with
        {
            Survival = state.Survival with { Inventory = inventory },
            World = state.World with { Tiles = state.World.Tiles.SetItem(pos, tile with { GroundItems = ground }) }
        });
    }

    public static Result<SimulationState> Strike(SimulationState state, GameDefinitions definitions,
        WorldPosition target, int damage = 25)
    {
        var p = state.World.PlayerPosition;
        if (target.Z != p.Z || Math.Abs((long)target.X - p.X) + Math.Abs((long)target.Y - p.Y) != 1)
            return Fail("NOT_ADJACENT", "Target must be one cardinal tile away.");
        if (damage <= 0) return Fail("INVALID_DAMAGE", "Damage must be positive.");
        var obj = state.World.Objects.Values.Where(o => o.Position == target).OrderBy(o => o.Id).FirstOrDefault();
        if (obj is null) return Fail("NO_OBJECT", "Nothing to damage there.");
        if (!definitions.Objects.TryGetValue(obj.DefinitionId, out var def)) return Fail("UNKNOWN_OBJECT", obj.DefinitionId);
        if (def.RequiredToolTag is { } tag && !state.Survival.Inventory.Any(stack =>
            stack.Quantity > 0 && definitions.Items.TryGetValue(stack.ItemId, out var item) && item.Tags.Contains(tag)))
            return Fail("TOOL_REQUIRED", $"Requires tool with tag '{tag}'.");
        if (!state.World.Tiles.TryGetValue(target, out var tile)) return Fail("NO_TILE", "No target tile.");
        // Validate all drops before consuming energy or changing the world.
        if (def.Drops.Any(d => d.Quantity <= 0 || !definitions.Items.ContainsKey(d.ItemId)))
            return Fail("INVALID_DROPS", "Invalid destruction drop table.");
        // Tool quality is supplied as configuration; the same object definition
        // can therefore be evaluated under different gathering policies.
        int toolEfficiency = def.RequiredToolTag is null ? 100 : 150;
        var costPolicy = GatheringCostPolicy.Create(
            new GatheringConfig(BaseEnergyCost: 2, ToolEfficiencyPercent: toolEfficiency));
        var costResult = costPolicy(def);
        if (costResult is Result<int>.Fail costFailure)
            return Fail(costFailure.Code, costFailure.Message);
        int energyCost = ((Result<int>.Success)costResult).Value;

        var outcome = Game.ExecuteAction(state.Survival,
            new ActionRequest("Strike " + def.Name, 10, energyCost),
            Game.normalHunger, Game.normalThirst);
        if (!outcome.Success) return Fail("ACTION_FAILED", outcome.Message);
        int remaining = Math.Max(0, obj.Durability - damage);
        var objects = remaining == 0
            ? state.World.Objects.Remove(obj.Id)
            : state.World.Objects.SetItem(obj.Id, obj with { Durability = remaining });
        var tiles = state.World.Tiles;
        if (remaining == 0)
        {
            var drops = tile.GroundItems;
            foreach (var drop in def.Drops) drops = ChangeQuantity(drops, drop.ItemId, drop.Quantity);
            tiles = tiles.SetItem(target, tile with { GroundItems = drops });
        }
        return new Result<SimulationState>.Success(state with
        {
            Survival = outcome.State,
            World = state.World with { Objects = objects, Tiles = tiles }
        });
    }

    private static ImmutableArray<ItemStack> ChangeQuantity(IEnumerable<ItemStack> source, string id, int delta)
    {
        // Aggregate duplicate stacks, preserve first-seen order, and append a new item if needed.
        var result = new List<ItemStack>();
        bool found = false;
        foreach (var group in source.GroupBy(i => i.ItemId))
        {
            long amount = group.Sum(i => (long)i.Quantity) + (group.Key == id ? delta : 0);
            if (group.Key == id) found = true;
            if (amount > 0) result.Add(new ItemStack(group.Key, checked((int)amount)));
        }
        if (!found && delta > 0) result.Add(new ItemStack(id, delta));
        return result.ToImmutableArray();
    }

    private static Result<SimulationState> Fail(string code, string message) =>
        new Result<SimulationState>.Fail(code, message);
}
