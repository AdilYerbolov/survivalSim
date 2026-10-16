using SurvivalSim.Models;

namespace SurvivalSim.GameLogic;

public static class Main
{
    public static readonly Func<int, int> normalHunger = minutes => minutes / 20;
    public static readonly Func<int, int> harshHunger = minutes => minutes / 10;
    public static readonly Func<int, int> normalThirst = minutes => minutes / 15;
    public static readonly Func<int, int> harshThirst = minutes => minutes / 8;

    public static GameAction PrepareAction(GameState state, GameAction action)
    {
        int extraMinutes = state.Player.Energy < 30 ? action.DurationMinutes / 2 : 0;
        return action with { DurationMinutes = checked(action.DurationMinutes + extraMinutes) };
    }

    public static ActionEffects CalculateEffects(GameAction action, Func<int, int> hungerPolicy, Func<int, int> thirstPolicy) =>
        new(action.DurationMinutes, action.EnergyCost,
            hungerPolicy(action.DurationMinutes), thirstPolicy(action.DurationMinutes));

    public static GameState ApplyEffects(GameState state, ActionEffects effects) =>
        state with
        {
            TimeMinutes = checked(state.TimeMinutes + effects.DurationMinutes),
            Player = state.Player with
            {
                Energy = Math.Clamp(state.Player.Energy - effects.EnergyCost, 0, 100),
                Hunger = Math.Clamp(state.Player.Hunger - effects.HungerCost, 0, 100),
                Thirst = Math.Clamp(state.Player.Thirst - effects.ThirstCost, 0, 100)
            }
        };

    public static bool HasEnoughEnergy(GameState state, GameAction action) =>
        state.Player.Energy >= action.EnergyCost;

    public static ActionOutcome ExecuteAction(GameState state, ActionRequest? request,
        Func<int, int> hungerPolicy, Func<int, int> thirstPolicy)
    {
        if (request is null) return new(false, state, "Missing action");
        if (string.IsNullOrWhiteSpace(request.Name)) return new(false, state, "Missing action name");
        if (request.DurationMinutes <= 0) return new(false, state, "Duration must be positive");
        if (request.EnergyCost < 0) return new(false, state, "Energy cost cannot be negative");
        if (request.EnergyCost > 100) return new(false, state, "Energy cost cannot exceed 100");
        if (hungerPolicy is null || thirstPolicy is null) return new(false, state, "Missing survival policy");

        try
        {
            GameAction action = new(request.Name, request.DurationMinutes, request.EnergyCost);
            GameAction prepared = PrepareAction(state, action);
            if (!HasEnoughEnergy(state, prepared)) return new(false, state, "Not enough energy");
            ActionEffects effects = CalculateEffects(prepared, hungerPolicy, thirstPolicy);
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
