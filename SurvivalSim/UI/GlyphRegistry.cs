using SurvivalSim.Models;
using SurvivalSim.Models.World;

namespace SurvivalSim.UI;

public static class GlyphRegistry
{
    public static readonly MapGlyph Player = new('@', ConsoleColor.White);

    public static MapGlyph GetTerrain(string id, GameDefinitions definitions) =>
        definitions.Terrain.TryGetValue(id, out var definition)
            ? new MapGlyph(definition.Symbol, definition.Color)
            : new MapGlyph('?', ConsoleColor.Magenta);

    public static MapGlyph GetObject(string id, GameDefinitions definitions) =>
        definitions.Objects.TryGetValue(id, out var definition)
            ? new MapGlyph(definition.Symbol, definition.Color)
            : new MapGlyph('?', ConsoleColor.Magenta);

    public static MapGlyph GetItem(string id, GameDefinitions definitions) =>
        definitions.Items.TryGetValue(id, out var definition)
            ? new MapGlyph(definition.Symbol, definition.Color)
            : new MapGlyph('*', ConsoleColor.Magenta);
}
