using SurvivalSim.Models;
using Game = SurvivalSim.GameLogic.Main;


var tests = new (string Name, Action Run)[]
{
    ("Normal workflow", Tests.NormalWorkflow),
    ("Empty dataset", Tests.EmptyDataset),
    ("Exact energy boundary", Tests.ExactEnergyBoundary),
    ("Insufficient energy", Tests.InsufficientEnergyPreservesState),
    ("Invalid duration", Tests.InvalidDuration),
    ("Missing input", Tests.MissingInput),
    ("Two survival policies", Tests.TwoPolicies),
    ("Pure stages and unchanged state", Tests.PureStagesAndOriginalUnchanged),
    ("Caller collection unchanged", Tests.BatchDoesNotModifyCallerCollection),
    ("Invalid energy cost", Tests.InvalidEnergyCost),
    ("Zero energy cost", Tests.ZeroEnergyCostAllowed)
};
int passed = 0;
foreach (var (name, test) in tests)
{
    try { test(); Console.WriteLine($"[PASS] {name}"); passed++; }
    catch (Exception ex) { Console.WriteLine($"[FAIL] {name}: {ex.Message}"); }
}
Console.WriteLine($"\n{passed}/{tests.Length} tests passed");
if (passed != tests.Length) Environment.ExitCode = 1;

static class Assert
{
    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"Expected {expected}, got {actual}");
    }
    public static void True(bool condition, string message = "Expected true")
    {
        if (!condition) throw new Exception(message);
    }
}

static class Tests
{
    private static GameState Start(int energy = 100) => new(new Player(energy, 100, 100), 480);
    private static ActionOutcome Run(GameState state, ActionRequest? action,
        Func<int,int>? hunger = null, Func<int,int>? thirst = null) =>
        Game.ExecuteAction(state, action, hunger ?? Game.normalHunger, thirst ?? Game.normalThirst);

    public static void NormalWorkflow()
    {
        var result = Run(Start(), new("Gather sticks", 40, 10));
        Assert.True(result.Success);
        Assert.Equal(new GameState(new Player(90, 98, 98), 520), result.State);
    }
    public static void EmptyDataset()
    {
        var state = Start();
        var result = Game.ExecuteBatch(state, Array.Empty<ActionRequest>(), Game.normalHunger, Game.normalThirst);
        Assert.Equal(0, result.Outcomes.Count);
        Assert.Equal(state, result.FinalState);
    }
    public static void ExactEnergyBoundary()
    {
        var result = Run(Start(10), new("Walk", 20, 10));
        Assert.True(result.Success);
        Assert.Equal(0, result.State.Player.Energy);
        Assert.Equal(510, result.State.TimeMinutes);
    }
    public static void InsufficientEnergyPreservesState()
    {
        var state = Start(9);
        var result = Run(state, new("Walk", 20, 10));
        Assert.True(!result.Success);
        Assert.Equal(state, result.State);
    }
    public static void InvalidDuration()
    {
        var state = Start();
        foreach (int duration in new[] { 0, -5 })
        {
            var result = Run(state, new("Walk", duration, 2));
            Assert.True(!result.Success);
            Assert.Equal(state, result.State);
        }
    }
    public static void MissingInput()
    {
        var state = Start();
        Assert.True(!Run(state, null).Success);
        Assert.True(!Run(state, new(null, 20, 2)).Success);
        Assert.Equal(state, Run(state, null).State);
    }
    public static void TwoPolicies()
    {
        var state = Start();
        var action = new ActionRequest("Walk", 40, 5);
        var normal = Run(state, action, Game.normalHunger, Game.normalThirst);
        var harsh = Run(state, action, Game.harshHunger, Game.harshThirst);
        Assert.Equal(98, normal.State.Player.Hunger);
        Assert.Equal(96, harsh.State.Player.Hunger);
        Assert.True(normal.State != harsh.State);
    }
    public static void PureStagesAndOriginalUnchanged()
    {
        var state = Start(20);
        var action = new GameAction("Walk", 20, 5);
        var prepared = Game.PrepareAction(state, action);
        Assert.Equal(30, prepared.DurationMinutes);
        Assert.Equal(20, action.DurationMinutes);
        var effects = Game.CalculateEffects(prepared, Game.normalHunger, Game.normalThirst);
        Assert.Equal(1, effects.HungerCost);
        var updated = Game.ApplyEffects(state, effects);
        Assert.Equal(510, updated.TimeMinutes);
        Assert.Equal(Start(20), state);
    }
    public static void BatchDoesNotModifyCallerCollection()
    {
        var requests = new List<ActionRequest> { new("Walk", 20, 5) };
        var snapshot = requests.ToArray();
        var result = Game.ExecuteBatch(Start(), requests, Game.normalHunger, Game.normalThirst);
        Assert.Equal(snapshot.Length, requests.Count);
        Assert.Equal(snapshot[0], requests[0]);
        Assert.Equal(1, result.Outcomes.Count);
    }
    public static void InvalidEnergyCost()
    {
        var state = Start();
        var result = Run(state, new("Bad", 20, -3));
        Assert.True(!result.Success);
        Assert.Equal(state, result.State);
    }
    public static void ZeroEnergyCostAllowed()
    {
        var result = Run(Start(0), new("Rest", 60, 0));
        Assert.True(result.Success);
        Assert.Equal(570, result.State.TimeMinutes);
    }
}
