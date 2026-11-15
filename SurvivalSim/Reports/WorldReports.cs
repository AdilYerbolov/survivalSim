using SurvivalSim.GameLogic.World;
using SurvivalSim.Models;
using SurvivalSim.Models.World;

namespace SurvivalSim.Reports;

public sealed record ResourceReportRow(string ItemId, int TotalQuantity, int TileCount);
public sealed record TerrainReportRow(string TerrainId, int TileCount, int ObjectCount);

/// <summary>Pure LINQ projections over the in-memory world; file output is handled separately.</summary>
public static class WorldReports
{
    /// <summary>Groups all ground items by ID and sorts by quantity, then ID for stable ties.</summary>
    public static IReadOnlyList<ResourceReportRow> BuildResourceDistribution(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return world.Tiles.Values
            .SelectMany(tile => tile.GroundItems)
            .Where(stack => stack.Quantity > 0)
            .Select(stack => new { stack.ItemId, stack.Quantity })
            .GroupBy(stack => stack.ItemId)
            .Select(group => new ResourceReportRow(
                group.Key,
                group.Sum(stack => stack.Quantity),
                world.Tiles.Values.Count(tile => tile.GroundItems.Any(stack => stack.ItemId == group.Key))))
            .OrderByDescending(row => row.TotalQuantity)
            .ThenBy(row => row.ItemId, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>Counts tiles by terrain and objects standing on those terrain types.</summary>
    public static IReadOnlyList<TerrainReportRow> BuildTerrainAndObjectStatistics(WorldState world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return world.Tiles
            .GroupBy(pair => pair.Value.TerrainId)
            .Select(group => new TerrainReportRow(
                group.Key,
                group.Count(),
                world.Objects.Values.Count(obj =>
                    world.Tiles.TryGetValue(obj.Position, out var tile) && tile.TerrainId == group.Key)))
            .OrderByDescending(row => row.TileCount)
            .ThenBy(row => row.TerrainId, StringComparer.Ordinal)
            .ToArray();
    }

    public static string FormatResourceReport(IEnumerable<ResourceReportRow> rows) =>
        "WORLD RESOURCE DISTRIBUTION\n" + string.Join("\n", rows.Select(row =>
            $"{row.ItemId,-20} quantity={row.TotalQuantity,4} tiles={row.TileCount,4}"));

    public static string FormatTerrainReport(IEnumerable<TerrainReportRow> rows) =>
        "TERRAIN AND OBJECT STATISTICS\n" + string.Join("\n", rows.Select(row =>
            $"{row.TerrainId,-20} tiles={row.TileCount,4} objects={row.ObjectCount,4}"));
}
