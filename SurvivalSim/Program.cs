using System.Text.Json;
using SurvivalSim.Models;
using Game = SurvivalSim.GameLogic.Main;

var path = Path.Combine(AppContext.BaseDirectory, "Data", "actions.json");
ActionRequest[] actions;
try
{
    actions = JsonSerializer.Deserialize<ActionRequest[]>(File.ReadAllText(path))
        ?? throw new JsonException("Dataset must not be null");
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
{
    Console.Error.WriteLine($"Cannot load sample data: {ex.Message}");
    return;
}

var initial = new GameState(new Player(100, 100, 100), 480);
foreach (var (label, hunger, thirst) in new[]
{
    ("Normal", Game.normalHunger, Game.normalThirst),
    ("Harsh", Game.harshHunger, Game.harshThirst)
})
{
    var result = Game.ExecuteBatch(initial, actions, hunger, thirst);
    Console.WriteLine($"\n{label} survival policy:");
    foreach (var outcome in result.Outcomes)
        Console.WriteLine($"{(outcome.Success ? "OK" : "REJECTED")}: {outcome.Message}");
    Console.WriteLine($"Final state: {result.FinalState}");
}
