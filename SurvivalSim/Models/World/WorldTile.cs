using System.Collections.Immutable;
namespace SurvivalSim.Models.World;
public sealed record WorldTile(string TerrainId, ImmutableArray<ItemStack> GroundItems);
