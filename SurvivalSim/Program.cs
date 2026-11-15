using System.Collections.Immutable;
using System.Text.Json;
using SurvivalSim.Models;
using SurvivalSim.Models.World;
using SurvivalSim.GameLogic;
using SurvivalSim.GameLogic.World;
using SurvivalSim.UI;
using SurvivalSim.Reports;
using Game = SurvivalSim.GameLogic.Main;

var definitionsPath = Path.Combine(AppContext.BaseDirectory, "Data", "Definitions");
var definitionsResult = DefinitionLoader.Load(definitionsPath);
if (definitionsResult is not Result<GameDefinitions>.Success definitionsSuccess)
{
    if (definitionsResult is Result<GameDefinitions>.Fail failure)
        Console.Error.WriteLine($"[{failure.Code}] {failure.Message}");
    return;
}
var definitions = definitionsSuccess.Value;

var recipePath = Path.Combine(definitionsPath, "recipes.json");
var recipesResult = RecipeLoader.Load(recipePath);
if (recipesResult is not Result<ImmutableArray<CraftRecipe>>.Success recipesSuccess)
{
    if (recipesResult is Result<ImmutableArray<CraftRecipe>>.Fail failure)
        Console.Error.WriteLine($"[{failure.Code}] {failure.Message}");
    return;
}
Console.WriteLine($"Loaded {recipesSuccess.Value.Length} crafting recipes.");
var buildingRecipesPath = Path.Combine(definitionsPath, "building_recipes.json");
var buildingRecipesResult = BuildingRecipeLoader.Load(buildingRecipesPath);
if (buildingRecipesResult is not Result<IReadOnlyList<BuildingRecipe>>.Success buildingRecipesSuccess)
{
    if (buildingRecipesResult is Result<IReadOnlyList<BuildingRecipe>>.Fail failure)
        Console.Error.WriteLine($"[{failure.Code}] {failure.Message}");
    return;
}
var buildingRecipes = buildingRecipesSuccess.Value;

Console.WriteLine("1 - Explore world (WASD, stairs)\n2 - Previous survival batch demonstration");
var selection = Console.ReadKey(intercept: true).KeyChar;
Console.WriteLine();
if (selection == '2')
{
    RunLegacyDemo();
    return;
}

var worldDataPath = Path.Combine(definitionsPath, "world.json");
var worldResult = WorldDataLoader.Load(worldDataPath, definitions);
if (worldResult is not Result<WorldState>.Success worldSuccess)
{
    if (worldResult is Result<WorldState>.Fail failure)
        Console.Error.WriteLine($"[{failure.Code}] {failure.Message}");
    return;
}

var state = new SimulationState(
    new GameState(new Player(100, 100, 100), 480,
        ImmutableArray.Create(new ItemStack("stone_axe", 1), new ItemStack("stone_pickaxe", 1))),
    worldSuccess.Value);
string status = "Explore with WASD. F5: save | F6: load | F7: reports | Q: quit.";
var savePath = Path.Combine(AppContext.BaseDirectory, "Data", "Saves", "world-save.json");
var reportsPath = Path.Combine(AppContext.BaseDirectory, "Data", "Reports");
Console.CursorVisible = false;
try
{
    while (true)
    {
        Console.Clear();
        SurvivalHud.Render(state.Survival);
        WorldRenderer.Render(state.World, definitions, radiusX: 10, radiusY: 6);
        Console.ResetColor();
        Console.WriteLine($"Time: {FormatTime(state.Survival.TimeMinutes)} | Position: ({state.World.PlayerPosition.X}, {state.World.PlayerPosition.Y}, {state.World.PlayerPosition.Z})");
        Console.WriteLine("WASD: move | >/<: stairs | G: pickup menu | P: drop | X: gather/strike | I: inspect | N: nearby | V: inventory | C: craft | B: build | F5: save | F6: load | F7: reports | Q: quit");
        Console.WriteLine(status);
        var key = Console.ReadKey(intercept: true).Key;
        if (key == ConsoleKey.Q) break;
        if (key == ConsoleKey.F5)
        {
            var saved = WorldSaveService.Save(state, savePath);
            status = saved is Result<string>.Success ok
                ? $"World saved: {ok.Value}"
                : $"Save failed: {((Result<string>.Fail)saved).Message}";
            continue;
        }
        if (key == ConsoleKey.F6)
        {
            var loaded = WorldSaveService.Load(savePath, definitions);
            if (loaded is Result<SimulationState>.Success ok)
            {
                state = ok.Value;
                status = "World loaded successfully.";
            }
            else status = $"Load failed: {((Result<SimulationState>.Fail)loaded).Message}";
            continue;
        }
        if (key == ConsoleKey.F7)
        {
            try
            {
                Directory.CreateDirectory(reportsPath);
                var resourceText = WorldReports.FormatResourceReport(WorldReports.BuildResourceDistribution(state.World));
                var terrainText = WorldReports.FormatTerrainReport(WorldReports.BuildTerrainAndObjectStatistics(state.World));
                var resourceFile = Path.Combine(reportsPath, "resource-distribution.txt");
                var terrainFile = Path.Combine(reportsPath, "terrain-statistics.txt");
                File.WriteAllText(resourceFile, resourceText);
                File.WriteAllText(terrainFile, terrainText);
                status = $"Reports exported to {reportsPath}";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                status = $"Report export failed: {ex.Message}";
            }
            continue;
        }
        if (key == ConsoleKey.V)
        {
            ShowInventory();
            continue;
        }
        if (key == ConsoleKey.C)
        {
            ShowCrafting();
            continue;
        }
        if (key == ConsoleKey.B)
        {
            ShowBuilding();
            continue;
        }
        if (key == ConsoleKey.G)
        {
            ShowPickup();
            continue;
        }
        if (key is ConsoleKey.P or ConsoleKey.X or ConsoleKey.I or ConsoleKey.N)
        {
            if (key == ConsoleKey.N)
            {
                var nearby = state.World.GetNearbyObjects(state.World.PlayerPosition, 3)
                    .Select(obj => $"{obj.DefinitionId} ({obj.Position.X},{obj.Position.Y},{obj.Position.Z}) HP:{obj.Durability}");
                status = "Nearby objects (radius 3): " + (nearby.Any() ? string.Join("; ", nearby) : "none");
                continue;
            }
            if (key == ConsoleKey.I)
            {
                var info = InteractionService.Inspect(state, definitions, state.World.PlayerPosition);
                status = info is Result<string>.Success ok ? ok.Value : ((Result<string>.Fail)info).Message;
                continue;
            }
            if (key == ConsoleKey.P)
            {
                Console.CursorVisible = true;
                Console.Write("\\nItem ID: ");
                var itemId = Console.ReadLine() ?? "";
                Console.Write("Quantity: ");
                bool valid = int.TryParse(Console.ReadLine(), out int qty);
                Console.CursorVisible = false;
                var action = !valid
                    ? new Result<SimulationState>.Fail("INVALID_QUANTITY", "Enter a number.")
                    : InteractionService.Drop(state, definitions, itemId, qty);
                Apply(action);
                continue;
            }
            if (key == ConsoleKey.X)
            {
                Console.Write("\nStrike direction (WASD): ");
                var direction = Console.ReadKey(intercept: true).Key;
                var offset = direction switch
                {
                    ConsoleKey.W => (X: 0, Y: -1), ConsoleKey.S => (X: 0, Y: 1),
                    ConsoleKey.A => (X: -1, Y: 0), ConsoleKey.D => (X: 1, Y: 0),
                    _ => (X: 0, Y: 0)
                };
                var pos = state.World.PlayerPosition;
                Apply(InteractionService.Strike(state, definitions,
                    pos with { X = pos.X + offset.X, Y = pos.Y + offset.Y }));
                continue;
            }
        }
        Result<SimulationState>? result = key switch
        {
            ConsoleKey.W or ConsoleKey.UpArrow => MovementService.Move(state, definitions, 0, -1),
            ConsoleKey.S or ConsoleKey.DownArrow => MovementService.Move(state, definitions, 0, 1),
            ConsoleKey.A or ConsoleKey.LeftArrow => MovementService.Move(state, definitions, -1, 0),
            ConsoleKey.D or ConsoleKey.RightArrow => MovementService.Move(state, definitions, 1, 0),
            ConsoleKey.OemPeriod => MovementService.ChangeLevel(state, definitions, -1),
            ConsoleKey.OemComma => MovementService.ChangeLevel(state, definitions, 1),
            _ => null
        };
        if (result is null) status = "Unknown key.";
        else Apply(result);
    }
}
finally
{
    Console.ResetColor();
    Console.CursorVisible = true;
}

void Apply(Result<SimulationState> result)
{
    switch (result)
    {
        case Result<SimulationState>.Success success:
            state = success.Value;
            status = "Action completed.";
            break;
        case Result<SimulationState>.Fail failure:
            status = $"[{failure.Code}] {failure.Message}";
            break;
    }
}

void ShowPickup()
{
    if (!state.World.Tiles.TryGetValue(state.World.PlayerPosition, out var currentTile))
    {
        status = "There is no tile under your character.";
        return;
    }

    // Group duplicate ground stacks so each item type appears only once in the menu.
    var groundItems = currentTile.GroundItems
        .Where(stack => stack.Quantity > 0)
        .GroupBy(stack => stack.ItemId, StringComparer.Ordinal)
        .Select(group => new ItemStack(group.Key, checked(group.Sum(stack => stack.Quantity))))
        .OrderBy(stack => definitions.Items.TryGetValue(stack.ItemId, out var item)
            ? item.Name : stack.ItemId, StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (groundItems.Count == 0)
    {
        status = "There are no items on this tile to pick up.";
        return;
    }

    int selectedIndex = 0;
    while (true)
    {
        Console.Clear();
        SurvivalHud.Render(state.Survival);
        Console.WriteLine("╔════════════════════════ NEARBY ITEMS ═══════════════════════╗");
        Console.WriteLine($"║ Ground items: {groundItems.Count} | Position: ({state.World.PlayerPosition.X}, {state.World.PlayerPosition.Y}, {state.World.PlayerPosition.Z})");
        Console.WriteLine("╚═════════════════════════════════════════════════════════════╝");
        Console.WriteLine("Select an item and press Enter to pick up its full stack. Press A to collect everything.");
        Console.WriteLine();

        for (int i = 0; i < groundItems.Count; i++)
        {
            var stack = groundItems[i];
            string name = definitions.Items.TryGetValue(stack.ItemId, out var item)
                ? item.Name : stack.ItemId;
            decimal weight = definitions.Items.TryGetValue(stack.ItemId, out item)
                ? item.Weight * stack.Quantity : 0m;

            if (i == selectedIndex)
            {
                Console.BackgroundColor = ConsoleColor.DarkCyan;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(" > ");
            }
            else Console.Write("   ");

            Console.WriteLine($"{i + 1,2}. {name} x{stack.Quantity}  ({weight:0.0} kg)");
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.WriteLine("[↑/↓] Select  [Enter] Pick up  [A] Pick up all  [Esc] Close");
        var key = Console.ReadKey(intercept: true).Key;

        if (key == ConsoleKey.Escape) break;
        if (key is ConsoleKey.UpArrow or ConsoleKey.W)
        {
            selectedIndex = (selectedIndex - 1 + groundItems.Count) % groundItems.Count;
            continue;
        }
        if (key is ConsoleKey.DownArrow or ConsoleKey.S)
        {
            selectedIndex = (selectedIndex + 1) % groundItems.Count;
            continue;
        }

        if (key == ConsoleKey.Enter)
        {
            var selected = groundItems[selectedIndex];
            var result = InteractionService.Pickup(state, definitions, selected.ItemId, selected.Quantity);
            Apply(result);
            if (result is Result<SimulationState>.Success)
            {
                status = $"Picked up {selected.ItemId} x{selected.Quantity}.";
                groundItems.RemoveAt(selectedIndex);
                if (groundItems.Count == 0) break;
                selectedIndex = Math.Min(selectedIndex, groundItems.Count - 1);
            }
            else
            {
                status = $"Pickup failed: {((Result<SimulationState>.Fail)result).Message}";
                break;
            }
            continue;
        }

        if (key == ConsoleKey.A)
        {
            int pickedUp = 0;
            foreach (var stack in groundItems.ToArray())
            {
                var result = InteractionService.Pickup(state, definitions, stack.ItemId, stack.Quantity);
                if (result is Result<SimulationState>.Success success)
                {
                    state = success.Value;
                    pickedUp++;
                }
                else
                {
                    status = $"Collected {pickedUp} item types; stopped: {((Result<SimulationState>.Fail)result).Message}";
                    break;
                }
            }
            if (pickedUp == groundItems.Count)
                status = $"Picked up all {pickedUp} item types.";
            break;
        }
    }
}

void ShowInventory()
{
    while (true)
    {
        Console.Clear();
        SurvivalHud.Render(state.Survival);
        Console.WriteLine("╔════════════════════════ INVENTORY ═════════════════════════╗");
        decimal totalWeight = state.Survival.Inventory.Sum(stack =>
            definitions.Items.TryGetValue(stack.ItemId, out var definition) ? definition.Weight * stack.Quantity : 0m);
        Console.WriteLine($"║ {state.Survival.Inventory.Count} stacks | Total weight: {totalWeight:0.0} kg                              ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        if (state.Survival.Inventory.Count == 0) Console.WriteLine("Your inventory is empty.");
        for (int i = 0; i < state.Survival.Inventory.Count; i++)
        {
            var stack = state.Survival.Inventory[i];
            string name = definitions.Items.TryGetValue(stack.ItemId, out var item) ? item.Name : stack.ItemId;
            Console.WriteLine($"{i + 1,2}. {name} ({stack.ItemId}) x{stack.Quantity}");
        }
        Console.WriteLine("\n[D] Drop an item  [Esc] Close inventory");
        var key = Console.ReadKey(true);
        if (key.Key == ConsoleKey.Escape) break;
        if (key.Key == ConsoleKey.D)
        {
            Console.CursorVisible = true;
            Console.Write("Item number: ");
            if (!int.TryParse(Console.ReadLine(), out int index) || index < 1 || index > state.Survival.Inventory.Count)
            {
                Console.CursorVisible = false;
                continue;
            }
            var stack = state.Survival.Inventory[index - 1];
            Console.Write($"Quantity (1-{stack.Quantity}): ");
            if (!int.TryParse(Console.ReadLine(), out int quantity))
            {
                Console.CursorVisible = false;
                continue;
            }
            Console.CursorVisible = false;
            Apply(InteractionService.Drop(state, definitions, stack.ItemId, quantity));
        }
    }
}

void ShowCrafting()
{
    Console.Clear();
    SurvivalHud.Render(state.Survival);
    Console.WriteLine("=== CRAFTING ===");
    for (int i = 0; i < recipesSuccess.Value.Length; i++)
    {
        var recipe = recipesSuccess.Value[i];
        string resultName = definitions.Items.TryGetValue(recipe.ResultItemId, out var item) ? item.Name : recipe.ResultItemId;
        string ingredients = string.Join(", ", recipe.Ingredients.Select(x =>
        {
            int available = state.Survival.Inventory.Where(stack => stack.ItemId == x.ItemId).Sum(stack => stack.Quantity);
            string ingredientName = definitions.Items.TryGetValue(x.ItemId, out var ingredientDef) ? ingredientDef.Name : x.ItemId;
            return $"{ingredientName} {Math.Min(available, x.Quantity)}/{x.Quantity}";
        }));
        bool craftable = recipe.Ingredients.GroupBy(x => x.ItemId).All(group =>
            state.Survival.Inventory.Where(stack => stack.ItemId == group.Key).Sum(stack => stack.Quantity) >= group.Sum(x => x.Quantity));
        Console.WriteLine($"{i + 1}. {(craftable ? "[READY]" : "[MISSING]")} {resultName} x{recipe.ResultQuantity} <- {ingredients} | {recipe.DurationMinutes} min | {recipe.EnergyCost} energy");
    }
    Console.WriteLine("Choose recipe number, or Esc to cancel:");
    var key = Console.ReadKey(true);
    if (key.Key == ConsoleKey.Escape || key.KeyChar < '1' || key.KeyChar > '9') return;
    int selected = key.KeyChar - '1';
    if (selected >= recipesSuccess.Value.Length) return;
    var chosen = recipesSuccess.Value[selected];
    var crafted = CraftingService.CraftWithResult(state.Survival, recipesSuccess.Value, chosen.Id,
        Game.normalHunger, Game.normalThirst);
    if (crafted is Result<GameState>.Success success)
    {
        state = state with { Survival = success.Value };
        status = $"Crafted {chosen.ResultItemId} x{chosen.ResultQuantity}.";
    }
    else status = $"Crafting failed: {((Result<GameState>.Fail)crafted).Message}";
}

void ShowBuilding()
{
    Console.Clear();
    SurvivalHud.Render(state.Survival);
    Console.WriteLine("=== BUILDING ===");
    for (int i = 0; i < buildingRecipes.Count; i++)
    {
        var recipe = buildingRecipes[i];
        string objectName = definitions.Objects.TryGetValue(recipe.ObjectDefinitionId, out var obj) ? obj.Name : recipe.ObjectDefinitionId;
        string ingredients = string.Join(", ", recipe.Ingredients.Select(x =>
        {
            int available = state.Survival.Inventory.Where(stack => stack.ItemId == x.ItemId).Sum(stack => stack.Quantity);
            string ingredientName = definitions.Items.TryGetValue(x.ItemId, out var ingredientDef) ? ingredientDef.Name : x.ItemId;
            return $"{ingredientName} {Math.Min(available, x.Quantity)}/{x.Quantity}";
        }));
        bool buildable = recipe.Ingredients.GroupBy(x => x.ItemId).All(group =>
            state.Survival.Inventory.Where(stack => stack.ItemId == group.Key).Sum(stack => stack.Quantity) >= group.Sum(x => x.Quantity));
        Console.WriteLine($"{i + 1}. {(buildable ? "[READY]" : "[MISSING]")} {objectName} <- {ingredients} | {recipe.DurationMinutes} min | {recipe.EnergyCost} energy");
    }
    Console.WriteLine("Choose a structure number, or Esc to cancel:");
    var key = Console.ReadKey(true);
    if (key.Key == ConsoleKey.Escape || key.KeyChar < '1' || key.KeyChar > '9') return;
    int selected = key.KeyChar - '1';
    if (selected >= buildingRecipes.Count) return;
    Console.WriteLine("Build direction: W/A/S/D");
    var direction = Console.ReadKey(true).Key;
    var p = state.World.PlayerPosition;
    var target = direction switch
    {
        ConsoleKey.W => p with { Y = p.Y - 1 },
        ConsoleKey.S => p with { Y = p.Y + 1 },
        ConsoleKey.A => p with { X = p.X - 1 },
        ConsoleKey.D => p with { X = p.X + 1 },
        _ => p
    };
    var built = BuildingService.Build(state, definitions, buildingRecipes[selected], target);
    Apply(built);
}

string FormatTime(int minutes) => $"{(minutes / 60) % 24:00}:{minutes % 60:00}";

void RunLegacyDemo()
{
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
    
    var initial = new GameState(new Player(100, 100, 100), 480, ImmutableArray<ItemStack>.Empty);
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
}
