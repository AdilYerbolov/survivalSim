using SurvivalSim.GameLogic;
using SurvivalSim.GameLogic.Native;
using SurvivalSim.GameLogic.World;
using SurvivalSim.Models;

public static class Stage5Tests
{
    public static void NativeCalculationReturnsExpectedCost()
    {
        var result = NativeGatheringCalculator.Calculate(10, 200, 150);
        Assert.True(result is Result<int>.Success);
        Assert.Equal(8, ((Result<int>.Success)result).Value);
    }

    public static void NativeCalculationRejectsInvalidInput()
    {
        var result = NativeGatheringCalculator.Calculate(0, 100, 100);
        Assert.True(result is Result<int>.Fail);
        Assert.Equal("INVALID_BASE_COST", ((Result<int>.Fail)result).Code);
    }

    public static void CurriedPoliciesUseDifferentConfigurations()
    {
        var definitionsResult = DefinitionLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Data", "Definitions"));
        if (definitionsResult is not Result<GameDefinitions>.Success loaded)
            throw new Exception("Could not load definitions.");

        var oak = loaded.Value.Objects["oak_tree"];
        var basicPolicy = GatheringCostPolicy.Create(
            new GatheringConfig(BaseEnergyCost: 10, ToolEfficiencyPercent: 100));
        var efficientPolicy = GatheringCostPolicy.Create(
            new GatheringConfig(BaseEnergyCost: 10, ToolEfficiencyPercent: 200));

        var basic = basicPolicy(oak);
        var efficient = efficientPolicy(oak);

        Assert.True(basic is Result<int>.Success);
        Assert.True(efficient is Result<int>.Success);
        Assert.Equal(11, ((Result<int>.Success)basic).Value); // ceil(10 * 110 / 100)
        Assert.Equal(6, ((Result<int>.Success)efficient).Value); // ceil(10 * 110 / 200)
        Assert.True(((Result<int>.Success)efficient).Value <
                    ((Result<int>.Success)basic).Value);
    }

    public static void YieldEnumerationIsDeferred()
    {
        int produced = 0;
        IEnumerable<int> Generate()
        {
            for (int i = 0; i < 5; i++)
            {
                produced++;
                yield return i;
            }
        }

        var sequence = Generate();
        Assert.Equal(0, produced); // Creating the enumerable executes nothing.

        var firstTwo = sequence.Take(2).ToArray();
        Assert.Equal(2, produced); // Only requested elements were produced.
        Assert.True(firstTwo.SequenceEqual(new[] { 0, 1 }));

        _ = sequence.ToArray(); // A second enumeration runs the iterator again.
        Assert.Equal(7, produced);
    }

    public static void NativeOverflowIsReported()
    {
        var result = NativeGatheringCalculator.Calculate(
            int.MaxValue, 1, int.MaxValue);
        Assert.True(result is Result<int>.Fail);
        Assert.Equal("NATIVE_CALCULATION_FAILED",
            ((Result<int>.Fail)result).Code);
    }
}
