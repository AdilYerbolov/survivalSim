using System.Collections.Immutable;
using SurvivalSim.Models;
using SurvivalSim.Models.World;

namespace SurvivalSim.GameLogic.World;

public static class WorldGenerator
{
    public static WorldState CreateTestWorld()
    {
        var tiles = ImmutableDictionary.CreateBuilder<WorldPosition, WorldTile>();
        for (int z = -1; z <= 0; z++)
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 24; x++)
        {
            var terrain = z == -1
                ? (x is 0 or 23 || y is 0 or 15 ? "deep_water" : "stone")
                : "grass";
            tiles[new WorldPosition(x, y, z)] =
                new WorldTile(terrain, ImmutableArray<ItemStack>.Empty);
        }

        tiles[new WorldPosition(9, 4, 0)] = new("deep_water", ImmutableArray<ItemStack>.Empty);
        tiles[new WorldPosition(9, 5, 0)] = new("deep_water", ImmutableArray<ItemStack>.Empty);
        tiles[new WorldPosition(4, 5, 0)] = new("sand", ImmutableArray<ItemStack>.Empty);
        tiles[new WorldPosition(6, 6, 0)] = new("grass",
            ImmutableArray.Create(new ItemStack("loose_stone", 3), new ItemStack("stick", 2)));

        var objects = ImmutableDictionary.CreateBuilder<Guid, WorldObject>();
        void Add(string id, int x, int y, int z, int durability)
        {
            var obj = new WorldObject(Guid.NewGuid(), id, new WorldPosition(x, y, z), durability);
            objects.Add(obj.Id, obj);
        }
        Add("oak_tree", 7, 4, 0, 100);
        Add("oak_tree", 8, 4, 0, 100);
        Add("boulder", 3, 8, 0, 200);
        Add("stone_furnace", 13, 9, 0, 250);
        Add("wooden_wall", 12, 9, 0, 80);
        Add("stairs_down", 5, 5, 0, 100);
        Add("stairs_up", 5, 5, -1, 100);
        Add("boulder", 8, 7, -1, 200);
        return new WorldState(tiles.ToImmutable(), objects.ToImmutable(), new WorldPosition(4, 4, 0));
    }
}
