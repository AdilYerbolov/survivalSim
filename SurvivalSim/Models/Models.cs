using System.Collections.Immutable;
using System.Net.Cache;

namespace SurvivalSim.Models;

public sealed record Player(
    int Energy,
    int Hunger,
    int Thirst,
    int Health = 100,
    int HungerProgressMinutes = 0,
    int ThirstProgressMinutes = 0,
    int StarvationProgressMinutes = 0
    );
public sealed record GameState(
    Player Player, 
    int TimeMinutes,
    IReadOnlyList<ItemStack> Inventory
    );
public sealed record GameAction
{
    public string Name { get; }
    public int DurationMinutes { get; }
    public int EnergyCost { get; }

    private GameAction(string name, int durationMinutes, int energyCost)
    {
        Name = name;
        DurationMinutes = durationMinutes;
        EnergyCost = energyCost;
    }
    public static ActionValidation Create(ActionRequest? request)
    {
        if ( request == null )
        {
            return new ActionValidation.Invalid("Missing action");
        }
        if (string.IsNullOrEmpty(request.Name))
        {
            return new ActionValidation.Invalid("Missing action name");
        }
        if (request.DurationMinutes <= 0)
        {
            return new ActionValidation.Invalid("Duration must be positive");
        }
        if (request.EnergyCost < 0 || request.EnergyCost > 100)
        {
            return new ActionValidation.Invalid("Invalid energy cost");
        }

        return new ActionValidation.Valid(new GameAction(request.Name, request.DurationMinutes, request.EnergyCost));
    }
    public (bool Success, GameAction action, string? reason) WithDuration(int duration)
    {
        if (duration <= 0)
        {
            return (false, this, "Invalid Duration");
        }
        return (true, new GameAction(Name, duration, EnergyCost), null);
    }
}
public sealed record ActionEffects(
    int DurationMinutes,
    int EnergyCost,
    int HungerCost,
    int ThirstCost,
    int HungerProgressMinutes = 0,
    int ThirstProgressMinutes = 0,
    int HealthCost = 0,
    int StarvationProgressMinutes = 0
    );
public sealed record ActionOutcome(
    bool Success,
    GameState State,
    string Message
    );
public sealed record ActionRequest(
    string? Name,
    int DurationMinutes,
    int EnergyCost
    );
public sealed record BatchResult(
    IReadOnlyList<ActionOutcome> Outcomes,
    GameState FinalState
    );
public abstract record ActionValidation
{
    private ActionValidation() { }

    public sealed record Valid(GameAction Action) : ActionValidation;
    public sealed record Invalid(string Reason) : ActionValidation;
}
public abstract record Result<T>
{
    private Result(){}

    public sealed record Success(T Value) : Result<T>;
    public sealed record Fail(string Code, string Message) : Result<T>;
}
public abstract record Option<T>
{
    private Option() {}

    public sealed record Some(T Value) : Option<T>;
    public sealed record None : Option<T>;
}
