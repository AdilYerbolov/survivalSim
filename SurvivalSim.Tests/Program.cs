
using SurvivalSim.Models;
using Game = SurvivalSim.GameLogic.Main;
using System.Collections.Immutable;

var tests = new (string Name, Action Run)[]
{
    // Assignment 5
    ("Normal workflow", Tests.NormalWorkflow),
    ("Empty dataset", Tests.EmptyDataset),
    ("Exact energy boundary", Tests.ExactEnergyBoundary),
    ("Insufficient energy", Tests.InsufficientEnergyPreservesState),
    ("Invalid duration", Tests.InvalidDuration),
    ("Missing input", Tests.MissingInput),
    ("Two survival policies", Tests.TwoPolicies),
    ("Pure pipeline and unchanged state", Tests.PureStagesAndOriginalUnchanged),
    ("Caller collection unchanged", Tests.BatchDoesNotModifyCallerCollection),
    ("Invalid energy cost", Tests.InvalidEnergyCost),
    ("Zero energy cost", Tests.ZeroEnergyCostAllowed),

    // Assignment 6
    ("A6 Factory success", Assignment6Tests.FactorySuccess),
    ("A6 Invalid duration", Assignment6Tests.FactoryInvalidDuration),
    ("A6 Invalid energy", Assignment6Tests.FactoryInvalidEnergy),
    ("A6 Pattern matching", Assignment6Tests.PatternMatchingCases),
    ("A6 Normal operation", Assignment6Tests.NormalOperation),
    ("A6 Missing prerequisite", Assignment6Tests.MissingPrerequisite),
    ("A6 Repeated operation", Assignment6Tests.RepeatedOperation),
    ("A6 Exact energy boundary", Assignment6Tests.ExactEnergyBoundary),
    ("A6 State preservation", Assignment6Tests.FailedOperationPreservesState),

    // Assignment 7
("A7 Successful crafting", Assignment7Tests.SuccessfulCrafting),
("A7 Invalid quantity", Assignment7Tests.InvalidQuantity),
("A7 Missing ingredient", Assignment7Tests.MissingIngredient),
("A7 Insufficient energy", Assignment7Tests.InsufficientEnergy),
("A7 Recipe found", Assignment7Tests.RecipeFound),
("A7 Recipe missing", Assignment7Tests.RecipeMissing),
("A7 Map success", Assignment7Tests.MapSuccess),
("A7 Bind success", Assignment7Tests.BindSuccess),
("A7 Map failure propagation", Assignment7Tests.MapFailurePropagation),
("A7 Bind failure propagation", Assignment7Tests.BindFailurePropagation),
("A7 Missing JSON file", Assignment7Tests.MissingJsonFile),
("A7 Malformed JSON", Assignment7Tests.MalformedJson),
("A7 Valid JSON", Assignment7Tests.ValidJson),
("A7 Failure preserves state", Assignment7Tests.FailurePreservesState),
("A7 Duplicate ingredients", Assignment7Tests.DuplicateIngredients),
("A7 Missing recipe result", Assignment7Tests.MissingRecipeResult),
    ("Stage2 Move success", Stage2Tests.MoveSuccess),
    ("Stage2 Blocked tree", Stage2Tests.BlockedTree),
    ("Stage2 Blocked water", Stage2Tests.BlockedWater),
    ("Stage2 Stairs", Stage2Tests.Stairs),
    ("Stage2 Stairs rejected", Stage2Tests.NoStairs),
    ("Stage2 Invalid direction", Stage2Tests.InvalidDirection),
    ("Stage2 Low energy", Stage2Tests.LowEnergy),
    ("Stage3 Pickup and drop", Stage3Tests.PickupAndDrop),
    ("Stage3 Failed pickup", Stage3Tests.FailedPickupPreservesState),
    ("Stage3 Tool requirement", Stage3Tests.ToolRequired),
    ("Stage3 Tree drops", Stage3Tests.DestroyTreeSpawnsDrops),
    ("Stage3 Partial damage", Stage3Tests.PartialDamage),
    ("Stage3 Low energy", Stage3Tests.LowEnergyStrike),
    ("Stage3 Data-driven terrain", Stage3Tests.TerrainIsDataDriven),
    ("Stage4 Save/load round trip", Stage4Tests.SaveLoadRoundTrip),
    ("Stage4 Missing save", Stage4Tests.MissingSaveReturnsFailure),
    ("Stage4 Lazy visible positions", Stage4Tests.VisiblePositionsAreLazyAndCorrect),
    ("Stage4 Nearby objects query", Stage4Tests.NearbyObjectsUsesRadiusAndLevel),
    ("Stage4 Resource LINQ report", Stage4Tests.ResourceReportAggregatesAndSorts),
    ("Stage4 Terrain LINQ report", Stage4Tests.TerrainReportCountsTiles),

    // Stage 5: currying, lazy evaluation, and native C interop
    ("Stage5 Native calculation", Stage5Tests.NativeCalculationReturnsExpectedCost),
    ("Stage5 Native invalid input", Stage5Tests.NativeCalculationRejectsInvalidInput),
    ("Stage5 Curried gathering policies", Stage5Tests.CurriedPoliciesUseDifferentConfigurations),
    ("Stage5 Deferred yield execution", Stage5Tests.YieldEnumerationIsDeferred),
    ("Stage5 Native overflow handling", Stage5Tests.NativeOverflowIsReported),

    // Stage 6: persistence hardening and submission checks
    ("Stage6 Malformed save", Stage6Tests.MalformedSaveReturnsFailure),
    ("Stage6 Missing save sections", Stage6Tests.SaveWithMissingSectionsReturnsFailure),
    ("Stage6 Unknown item reference", Stage6Tests.UnknownItemReferenceIsRejected),
    ("Stage6 Incomplete state rejected", Stage6Tests.SaveRejectsIncompleteState),
    ("Stage6 Failed load preserves state", Stage6Tests.FailedLoadDoesNotCreateOrMutateState),

    // Survival UX and gameplay improvements
    ("Survival needs accumulate across short actions", SurvivalImprovementsTests.HungerAccumulatesAcrossShortActions),
    ("Building recipes load from JSON", SurvivalImprovementsTests.BuildingRecipesLoadFromJson),
    ("Building consumes materials and places an object", SurvivalImprovementsTests.BuildingConsumesMaterialsAndPlacesObject),

    // Assignment 8: reports, JSON input, lazy world queries, resource boundaries, native failure
    ("A8 Reports handle empty input and no matches", Stage8Tests.ReportsHandleEmptyWorldAndNoMatches),
    ("A8 Resource report grouping, totals, and stable ties", Stage8Tests.ResourceReportGroupsTotalsAndStableTies),
    ("A8 Terrain report grouping and stable ties", Stage8Tests.TerrainReportGroupsAndOrdersTies),
    ("A8 Reports reject invalid null input", Stage8Tests.ReportsRejectNullWorld),
    ("A8 Reports use JSON-loaded application world", Stage8Tests.WorldReportsUseJsonLoadedApplicationWorld),
    ("A8 World JSON rejects malformed and unknown references", Stage8Tests.WorldDataLoaderRejectsMalformedAndUnknownReferences),
    ("A8 Actual yield iterator is deferred and snapshot is reusable", Stage8Tests.ActualVisiblePositionIteratorIsDeferredAndSnapshotIsReusable),
    ("A8 Native adapter maps controlled load failure", Stage8Tests.NativeAdapterFailureBecomesResultFailure),
    ("A8 Convenience file reads release handles", Stage8Tests.ConvenienceFileReadsReleaseHandlesAfterSuccessAndFailure),
    ("A8 Complete workflow integrates native strike and reports", Stage8Tests.NormalWorkflowIntegratesNativeStrikeAndReports)
};

int passed = 0;

foreach (var (name, test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"[PASS] {name}");
        passed++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[FAIL] {name}: {ex.Message}");
    }
}

Console.WriteLine($"\n{passed}/{tests.Length} tests passed");

if (passed != tests.Length)
    Environment.ExitCode = 1;


// ================= ASSERTIONS =================

public static class Assert
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception(
                $"Expected {expected}, got {actual}");
    }

    public static void True(
        bool condition,
        string message = "Expected true")
    {
        if (!condition)
            throw new Exception(message);
    }
}


// ================= ASSIGNMENT 5 TESTS =================

public static class Tests
{
    private static GameState Start(int energy = 100) =>
        new(
            new Player(energy, 100, 100),
            480,
            ImmutableArray<ItemStack>.Empty
        );

    private static ActionOutcome Run(
        GameState state,
        ActionRequest? action,
        Func<int, int>? hunger = null,
        Func<int, int>? thirst = null)
    {
        return Game.ExecuteAction(
            state,
            action,
            hunger ?? Game.normalHunger,
            thirst ?? Game.normalThirst
        );
    }

    public static void NormalWorkflow()
    {
        var result = Run(
            Start(),
            new ActionRequest("Gather sticks", 40, 10)
        );

        Assert.True(result.Success);

        Assert.Equal(90, result.State.Player.Energy);
        Assert.Equal(100, result.State.Player.Hunger);
        Assert.Equal(100, result.State.Player.Thirst);
        Assert.Equal(520, result.State.TimeMinutes);
    }

    public static void EmptyDataset()
    {
        var state = Start();

        var result = Game.ExecuteBatch(
            state,
            Array.Empty<ActionRequest>(),
            Game.normalHunger,
            Game.normalThirst
        );

        Assert.Equal(0, result.Outcomes.Count);
        Assert.Equal(state, result.FinalState);
    }

    public static void ExactEnergyBoundary()
    {
        var result = Run(
            Start(10),
            new ActionRequest("Walk", 20, 10)
        );

        Assert.True(result.Success);
        Assert.Equal(0, result.State.Player.Energy);
        Assert.Equal(510, result.State.TimeMinutes);
    }

    public static void InsufficientEnergyPreservesState()
    {
        var state = Start(9);

        var result = Run(
            state,
            new ActionRequest("Walk", 20, 10)
        );

        Assert.True(!result.Success);
        Assert.Equal(state, result.State);
    }

    public static void InvalidDuration()
    {
        var state = Start();

        foreach (int duration in new[] { 0, -5 })
        {
            var result = Run(
                state,
                new ActionRequest("Walk", duration, 2)
            );

            Assert.True(!result.Success);
            Assert.Equal(state, result.State);
        }
    }

    public static void MissingInput()
    {
        var state = Start();

        Assert.True(!Run(state, null).Success);

        Assert.True(!Run(
            state,
            new ActionRequest(null, 20, 2)
        ).Success);

        Assert.Equal(state, Run(state, null).State);
    }

    public static void TwoPolicies()
    {
        var state = Start();
        var action = new ActionRequest("Walk", 40, 5);

        var normal = Run(
            state,
            action,
            Game.normalHunger,
            Game.normalThirst
        );

        var harsh = Run(
            state,
            action,
            Game.harshHunger,
            Game.harshThirst
        );

        Assert.True(normal.Success);
        Assert.True(harsh.Success);

        Assert.Equal(100, normal.State.Player.Hunger);
        Assert.Equal(99, harsh.State.Player.Hunger);

        Assert.True(normal.State != harsh.State);
    }

    public static void PureStagesAndOriginalUnchanged()
    {
        var state = Start(20);
        var action = new ActionRequest("Walk", 20, 5);

        var first = Run(state, action);
        var second = Run(state, action);

        Assert.True(first.Success);
        Assert.True(second.Success);

        // The same input produces the same result.
        Assert.Equal(first.State, second.State);

        // The original state remains unchanged.
        Assert.Equal(20, state.Player.Energy);
        Assert.Equal(480, state.TimeMinutes);

        // The fatigue adjustment is applied internally.
        Assert.Equal(510, first.State.TimeMinutes);
        Assert.Equal(15, first.State.Player.Energy);

        // Original action request is unchanged.
        Assert.Equal(20, action.DurationMinutes);
        Assert.Equal(5, action.EnergyCost);
    }

    public static void BatchDoesNotModifyCallerCollection()
    {
        var requests = new List<ActionRequest>
        {
            new("Walk", 20, 5)
        };

        var snapshot = requests.ToArray();

        var result = Game.ExecuteBatch(
            Start(),
            requests,
            Game.normalHunger,
            Game.normalThirst
        );

        Assert.Equal(snapshot.Length, requests.Count);
        Assert.Equal(snapshot[0], requests[0]);
        Assert.Equal(1, result.Outcomes.Count);
    }

    public static void InvalidEnergyCost()
    {
        var state = Start();

        var result = Run(
            state,
            new ActionRequest("Bad", 20, -3)
        );

        Assert.True(!result.Success);
        Assert.Equal(state, result.State);
    }

    public static void ZeroEnergyCostAllowed()
    {
        var result = Run(
            Start(0),
            new ActionRequest("Rest", 60, 0)
        );

        Assert.True(result.Success);

        // Fatigue applies because energy is below 30.
        Assert.Equal(570, result.State.TimeMinutes);
        Assert.Equal(0, result.State.Player.Energy);
    }
}
