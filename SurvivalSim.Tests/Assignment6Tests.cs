using System.Collections.Immutable;
using SurvivalSim.Models;
using Game = SurvivalSim.GameLogic.Main;

public static class Assignment6Tests
{
    private static GameState Start(int energy = 100) =>
    new(
        new Player(energy, 100, 100),
        480,
        ImmutableArray<ItemStack>.Empty
    );

    private static ActionOutcome Run(
        GameState state,
        ActionRequest? request)
    {
        return Game.ExecuteAction(
            state,
            request,
            Game.normalHunger,
            Game.normalThirst
        );
    }

    public static void FactorySuccess()
    {
        var result = GameAction.Create(
            new ActionRequest("Walk", 20, 5));

        Assert.True(result is ActionValidation.Valid);

        if (result is ActionValidation.Valid valid)
        {
            Assert.Equal("Walk", valid.Action.Name);
            Assert.Equal(20, valid.Action.DurationMinutes);
            Assert.Equal(5, valid.Action.EnergyCost);
        }
    }

    public static void FactoryInvalidDuration()
    {
        var result = GameAction.Create(
            new ActionRequest("Walk", 0, 5));

        Assert.True(result is ActionValidation.Invalid);
    }

    public static void FactoryInvalidEnergy()
    {
        var result = GameAction.Create(
            new ActionRequest("Walk", 20, -5));

        Assert.True(result is ActionValidation.Invalid);
    }

    public static void PatternMatchingCases()
    {
        var valid = GameAction.Create(
            new ActionRequest("Walk", 20, 5));

        var invalid = GameAction.Create(
            new ActionRequest("Walk", -20, 5));

        string Describe(ActionValidation validation) =>
            validation switch
            {
                ActionValidation.Valid => "Valid",
                ActionValidation.Invalid => "Invalid",
                _ => throw new InvalidOperationException()
            };

        Assert.Equal("Valid", Describe(valid));
        Assert.Equal("Invalid", Describe(invalid));
    }

    public static void NormalOperation()
    {
        var result = Run(
            Start(),
            new ActionRequest("Walk", 20, 5));

        Assert.True(result.Success);
        Assert.Equal(95, result.State.Player.Energy);
        Assert.Equal(500, result.State.TimeMinutes);
    }

    public static void MissingPrerequisite()
    {
        var state = Start(4);

        var result = Run(
            state,
            new ActionRequest("Walk", 20, 5));

        Assert.True(!result.Success);
        Assert.Equal(state, result.State);
    }

    public static void RepeatedOperation()
    {
        var state = Start(10);
        var action = new ActionRequest("Walk", 20, 5);

        var first = Run(state, action);
        Assert.True(first.Success);

        var second = Run(first.State, action);
        Assert.True(second.Success);
        Assert.Equal(0, second.State.Player.Energy);

        var third = Run(second.State, action);
        Assert.True(!third.Success);
        Assert.Equal(second.State, third.State);
    }

    public static void ExactEnergyBoundary()
    {
        var result = Run(
            Start(5),
            new ActionRequest("Walk", 20, 5));

        Assert.True(result.Success);
        Assert.Equal(0, result.State.Player.Energy);

        // Fatigue: 20 + 10 = 30 minutes
        Assert.Equal(510, result.State.TimeMinutes);
    }

    public static void FailedOperationPreservesState()
    {
        var state = Start(100);

        var result = Run(
            state,
            new ActionRequest("Walk", -10, 5));

        Assert.True(!result.Success);
        Assert.Equal(state, result.State);
    }
}
