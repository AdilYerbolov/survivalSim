using System.Runtime.InteropServices;
using SurvivalSim.Models;

namespace SurvivalSim.GameLogic.Native;

/// <summary>Safe managed boundary around the native C gathering-cost function.</summary>
public static class NativeGatheringCalculator
{
    private const string LibraryName = "survivalsim_native";

    [DllImport(LibraryName, EntryPoint = "calculate_gathering_cost",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern int CalculateGatheringCostNative(
        int baseCost, int toolEfficiencyPercent, int difficultyPercent);

    public static Result<int> Calculate(
        int baseCost, int toolEfficiencyPercent, int difficultyPercent) =>
        CalculateCore(baseCost, toolEfficiencyPercent, difficultyPercent, CalculateGatheringCostNative);

    // Internal injection seam lets tests verify native-loading failures deterministically
    // without depending on whether the operating system has already loaded the DLL.
    internal static Result<int> CalculateWithInvoker(
        int baseCost,
        int toolEfficiencyPercent,
        int difficultyPercent,
        Func<int, int, int, int> invoke) =>
        CalculateCore(baseCost, toolEfficiencyPercent, difficultyPercent, invoke);

    private static Result<int> CalculateCore(
        int baseCost,
        int toolEfficiencyPercent,
        int difficultyPercent,
        Func<int, int, int, int> invoke)
    {
        if (baseCost <= 0)
            return Failure("INVALID_BASE_COST", "Base cost must be positive.");
        if (toolEfficiencyPercent <= 0)
            return Failure("INVALID_TOOL_EFFICIENCY", "Tool efficiency must be positive.");
        if (difficultyPercent <= 0)
            return Failure("INVALID_DIFFICULTY", "Difficulty must be positive.");

        try
        {
            int result = invoke(baseCost, toolEfficiencyPercent, difficultyPercent);
            return result > 0
                ? new Result<int>.Success(result)
                : Failure("NATIVE_CALCULATION_FAILED",
                    $"Native calculator returned error code {result}.");
        }
        catch (DllNotFoundException ex)
        {
            return Failure("NATIVE_LIBRARY_UNAVAILABLE",
                $"Could not load {LibraryName} or one of its dependencies. " +
                $"On Windows, build it with Native/build-native.ps1 (64-bit MinGW-w64); " +
                $"the script statically links the GCC runtime. {ex.Message}");
        }
        catch (EntryPointNotFoundException ex)
        {
            return Failure("NATIVE_ENTRY_POINT_MISSING", ex.Message);
        }
        catch (BadImageFormatException ex)
        {
            return Failure("NATIVE_LIBRARY_INVALID",
                $"The native library has the wrong architecture or format. {ex.Message}");
        }
    }

    private static Result<int> Failure(string code, string message) =>
        new Result<int>.Fail(code, message);
}
