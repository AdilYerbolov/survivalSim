using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;
using SurvivalSim.Models;

namespace SurvivalSim.GameLogic;

public static class Main
{
    // Survival rates are per elapsed in-game time, not per action. Cumulative
    // elapsed minutes are retained in Player so short actions add up correctly.
    public static readonly Func<int, int> normalHunger = minutes => minutes / 180; // 1 point / 3 hours
    public static readonly Func<int, int> harshHunger = minutes => minutes / 30;   // accelerated test/survival mode
    public static readonly Func<int, int> normalThirst = minutes => minutes / 60;  // 1 point / hour
    public static readonly Func<int, int> harshThirst = minutes => minutes / 20;   // accelerated test/survival mode

    private static GameAction PrepareAction(GameState state, GameAction action)
    {
        int extraMinutes = state.Player.Energy < 30 ? action.DurationMinutes / 2 : 0;
        int newDuration = checked(action.DurationMinutes + extraMinutes);
        (bool Success, GameAction newAction, string? reason) = action.WithDuration(newDuration);
        return newAction;
    }

    private static ActionEffects CalculateEffects(GameState state, GameAction action,
        Func<int, int> hungerPolicy, Func<int, int> thirstPolicy)
    {
        int hungerElapsed = checked(state.Player.HungerProgressMinutes + action.DurationMinutes);
        int thirstElapsed = checked(state.Player.ThirstProgressMinutes + action.DurationMinutes);
        int hungerCost = hungerPolicy(hungerElapsed) - hungerPolicy(state.Player.HungerProgressMinutes);
        int thirstCost = thirstPolicy(thirstElapsed) - thirstPolicy(state.Player.ThirstProgressMinutes);
        int hungerAfter = Math.Max(0, state.Player.Hunger - hungerCost);
        int thirstAfter = Math.Max(0, state.Player.Thirst - thirstCost);
        int starvationElapsed = hungerAfter == 0 || thirstAfter == 0
            ? checked(state.Player.StarvationProgressMinutes + action.DurationMinutes)
            : 0;
        int healthCost = starvationElapsed / 60 - state.Player.StarvationProgressMinutes / 60;
        return new ActionEffects(action.DurationMinutes, action.EnergyCost,
            hungerCost, thirstCost, hungerElapsed, thirstElapsed, healthCost, starvationElapsed);
    }

    private static GameState ApplyEffects(GameState state, ActionEffects effects) =>
        state with
        {
            TimeMinutes = checked(state.TimeMinutes + effects.DurationMinutes),
            Player = state.Player with
            {
                Energy = Math.Clamp(state.Player.Energy - effects.EnergyCost, 0, 100),
                Hunger = Math.Clamp(state.Player.Hunger - effects.HungerCost, 0, 100),
                Thirst = Math.Clamp(state.Player.Thirst - effects.ThirstCost, 0, 100),
                Health = Math.Clamp(state.Player.Health - effects.HealthCost, 0, 100),
                HungerProgressMinutes = effects.HungerProgressMinutes,
                ThirstProgressMinutes = effects.ThirstProgressMinutes,
                StarvationProgressMinutes = effects.StarvationProgressMinutes
            }
        };

    public static bool HasEnoughEnergy(GameState state, GameAction action) =>
        state.Player.Energy >= action.EnergyCost;

    public static ActionOutcome ExecuteAction(GameState state, ActionRequest? request, Func<int, int> hungerPolicy, Func<int, int> thirstPolicy)
    {
        ActionValidation validation = GameAction.Create(request);

        return validation switch
        {
            ActionValidation.Valid valid => ExecuteValidatedAction(state, valid.Action, hungerPolicy, thirstPolicy),
            ActionValidation.Invalid invalid => new ActionOutcome(false, state, invalid.Reason),
            _ => new ActionOutcome(false, state, "Unknown validation outcome")
        };
    }

    private static ActionOutcome ExecuteValidatedAction(GameState state, GameAction action,
        Func<int, int> hungerPolicy, Func<int, int> thirstPolicy)
    {
        if (hungerPolicy is null || thirstPolicy is null) return new(false, state, "Missing survival policy");

        try
        {
            GameAction prepared = PrepareAction(state, action);
            if (!HasEnoughEnergy(state, prepared)) return new(false, state, "Not enough energy");
            ActionEffects effects = CalculateEffects(state, prepared, hungerPolicy, thirstPolicy);
            if (effects.HungerCost < 0 || effects.ThirstCost < 0)
                return new(false, state, "Survival policies must not produce negative costs");
            GameState updated = ApplyEffects(state, effects);
            return new(true, updated, $"Completed {action.Name}");
        }
        catch (OverflowException)
        {
            return new(false, state, "Action duration or game time overflowed");
        }
    }

    public static BatchResult ExecuteBatch(GameState initial, IReadOnlyList<ActionRequest>? requests,
        Func<int, int> hungerPolicy, Func<int, int> thirstPolicy)
    {
        if (requests is null) throw new ArgumentNullException(nameof(requests));
        var results = new List<ActionOutcome>();
        GameState current = initial;
        foreach (ActionRequest request in requests)
        {
            var outcome = ExecuteAction(current, request, hungerPolicy, thirstPolicy);
            results.Add(outcome);
            current = outcome.State;
        }
        return new BatchResult(results.AsReadOnly(), current);
    }
}
