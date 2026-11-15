using SurvivalSim.Models;

namespace SurvivalSim.UI;

/// <summary>Compact status panel displayed above the world map.</summary>
public static class SurvivalHud
{
    public static void Render(GameState state)
    {
        var p = state.Player;
        Console.WriteLine("╔════════════════════════ SURVIVAL STATUS ════════════════════════╗");
        Console.WriteLine($"║ Health  {Bar(p.Health)} {p.Health,3}/100   Energy  {Bar(p.Energy)} {p.Energy,3}/100 ║");
        Console.WriteLine($"║ Hunger  {Bar(p.Hunger)} {p.Hunger,3}/100   Thirst  {Bar(p.Thirst)} {p.Thirst,3}/100 ║");
        Console.WriteLine("╚═════════════════════════════════════════════════════════════════╝");
    }

    private static string Bar(int value)
    {
        int filled = Math.Clamp(value, 0, 100) / 10;
        return "[" + new string('█', filled) + new string('·', 10 - filled) + "]";
    }
}
