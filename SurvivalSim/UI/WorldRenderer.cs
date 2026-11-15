using SurvivalSim.GameLogic.World;
using SurvivalSim.Models;
using SurvivalSim.Models.World;

namespace SurvivalSim.UI;

public static class WorldRenderer
{
    public static void Render(WorldState world, GameDefinitions definitions, int radiusX = 10, int radiusY = 10)
    {
        var objectIndex = world.Objects.Values
            .GroupBy(obj => obj.Position)
            .ToDictionary(group => group.Key, group => group.OrderBy(obj => obj.Id).First());

        var visiblePositions = world.PlayerPosition.GetVisiblePositions(radiusX, radiusY).ToArray();
        var width = radiusX * 2 + 1;
        for (var i = 0; i < visiblePositions.Length; i++)
        {
            var position = visiblePositions[i];
            var glyph = TileGlyphResolver.Resolve(world, position, definitions, objectIndex);
            Console.ForegroundColor = glyph.Color;
            Console.Write(glyph.Symbol);
            if ((i + 1) % width == 0)
            {
                Console.ResetColor();
                Console.WriteLine();
            }
        }
        Console.ResetColor();
    }
}
