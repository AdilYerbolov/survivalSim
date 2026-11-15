using SurvivalSim.Models;
using SurvivalSim.Models.World;

namespace SurvivalSim.UI;

public static  class TileGlyphResolver
{
    public static MapGlyph Resolve(WorldState world, WorldPosition position, GameDefinitions definitions, IReadOnlyDictionary<WorldPosition, WorldObject>? objectIndex = null)
    {
        if (position == world.PlayerPosition)
        {
            return GlyphRegistry.Player;
        }

        var worldObject = objectIndex is null
            ? world.Objects.Values.Where(o => o.Position == position).OrderBy(o => o.Id).FirstOrDefault()
            : objectIndex.GetValueOrDefault(position);

        if (worldObject is not null)
        {
            return GlyphRegistry.GetObject(worldObject.DefinitionId, definitions);
        }
        if (!world.Tiles.TryGetValue(position, out var tile))
        {
            return new MapGlyph(' ', ConsoleColor.Black);
        }
        if (!tile.GroundItems.IsDefaultOrEmpty)
        {
            var firstItem = tile.GroundItems[0];

            return GlyphRegistry.GetItem(firstItem.ItemId, definitions);
        }

        return GlyphRegistry.GetTerrain(tile.TerrainId, definitions);
    }
}
