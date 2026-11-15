using System.Collections.Immutable;
using SurvivalSim.Models.World;

namespace SurvivalSim.GameLogic.World;

/// <summary>Lazy, read-only queries over world coordinates and objects.</summary>
public static class WorldExtensions
{
    /// <summary>Enumerates positions in a square viewport, row by row. Enumeration is deferred.</summary>
    public static IEnumerable<WorldPosition> GetVisiblePositions(
        this WorldPosition center, int radiusX, int radiusY)
    {
        if (radiusX < 0 || radiusY < 0)
            throw new ArgumentOutOfRangeException(nameof(radiusX), "Viewport radii cannot be negative.");

        for (var y = center.Y - radiusY; y <= center.Y + radiusY; y++)
        for (var x = center.X - radiusX; x <= center.X + radiusX; x++)
            yield return new WorldPosition(x, y, center.Z);
    }

    /// <summary>Enumerates objects on the same Z-level within Chebyshev distance.</summary>
    public static IEnumerable<WorldObject> GetNearbyObjects(
        this WorldState world, WorldPosition center, int radius)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));

        return world.Objects.Values.Where(obj =>
            obj.Position.Z == center.Z &&
            Math.Abs(obj.Position.X - center.X) <= radius &&
            Math.Abs(obj.Position.Y - center.Y) <= radius);
    }

    /// <summary>Returns the quantity of an item in a collection; zero when absent.</summary>
    public static int QuantityOf(this IEnumerable<global::SurvivalSim.Models.ItemStack> stacks, string itemId) =>
        stacks.Where(stack => stack.ItemId == itemId).Sum(stack => stack.Quantity);
}
