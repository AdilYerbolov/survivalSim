using SurvivalSim.GameLogic.Native;
using SurvivalSim.Models;

namespace SurvivalSim.GameLogic.World;

/// <summary>
/// Curried policy: configuration -> object definition -> calculated energy cost.
/// The returned function closes over the selected configuration.
/// </summary>
public static class GatheringCostPolicy
{
    public static Func<ObjectDefinition, Result<int>> Create(GatheringConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return target =>
        {
            ArgumentNullException.ThrowIfNull(target);
            // Tougher objects increase difficulty. Keep the factor in a safe,
            // predictable range while preserving the definition-driven input.
            int difficultyPercent = Math.Clamp(
                100 + target.MaxDurability / 10, 100, 300);

            return NativeGatheringCalculator.Calculate(
                config.BaseEnergyCost,
                config.ToolEfficiencyPercent,
                difficultyPercent);
        };
    }
}
