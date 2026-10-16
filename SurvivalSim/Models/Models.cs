namespace SurvivalSim.Models;

public sealed record Player(int Energy, int Hunger, int Thirst);
public sealed record GameState(Player Player, int TimeMinutes);
public sealed record GameAction(string Name, int DurationMinutes, int EnergyCost);
public sealed record ActionEffects(int DurationMinutes, int EnergyCost, int HungerCost, int ThirstCost);
public sealed record ActionOutcome(bool Success, GameState State, string Message);
public sealed record ActionRequest(string? Name, int DurationMinutes, int EnergyCost);
public sealed record BatchResult(IReadOnlyList<ActionOutcome> Outcomes, GameState FinalState);
