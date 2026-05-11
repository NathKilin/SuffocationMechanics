using System.Threading;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.CursorVisible = false;
Console.Title = "SUFFOCATION";
try { Console.SetWindowSize(80, 36); } catch { }

// ── INTRO ────────────────────────────────────────────────────────────────────
Console.Clear();
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("\n\n  ╔══════════════════════════════════╗");
Console.WriteLine("  ║     S U F F O C A T I O N        ║");
Console.WriteLine("  ╚══════════════════════════════════╝\n");
Console.WriteLine("  Mechanics Prototype\n");
Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine("  Select AI difficulty — press 1 through 5:\n");
Console.WriteLine("  1 = Completely random");
Console.WriteLine("  2 = Simple majority check");
Console.WriteLine("  3 = Expected value calculation");
Console.WriteLine("  4 = EV + health awareness");
Console.WriteLine("  5 = Full strategy\n");
Console.Write("  > ");
Console.CursorVisible = true;

int difficulty = 3;
while (true)
{
    var key = Console.ReadKey(true);
    if (key.KeyChar >= '1' && key.KeyChar <= '5')
    {
        difficulty = key.KeyChar - '0';
        Console.WriteLine(difficulty);
        break;
    }
}

Console.CursorVisible = false;
Thread.Sleep(800);

var rng   = new Random();
var state = new GameState(rng, difficulty);

// ── MAIN LOOP ────────────────────────────────────────────────────────────────
while (true)
{
    if (state.PlayerInventory.GoodCount < 1 || state.PlayerInventory.ToxicCount < 1)
    {
        Display.ShowOutOfCapsules();
        break;
    }

    var  match     = new Match(state, rng);
    bool playerWon = match.Run();

    // Reset health display before any end screens
    state.PlayerHealth = state.MaxHealth;
    state.EnemyHealth  = state.MaxHealth;

    if (playerWon)
    {
        int goodWon  = state.PrizePool.GoodCount  + state.AirPump.GoodCount;
        int toxicWon = state.PrizePool.ToxicCount + state.AirPump.ToxicCount;
        state.PrizePool.TransferAllTo(state.PlayerInventory);
        state.AirPump.TransferAllTo(state.PlayerInventory);

        // Ask if player wants to visit the trader before the next match
        Display.ShowTraderPrompt(state);
        char traderAnswer = Console.ReadKey(true).KeyChar;
        Console.CursorVisible = false;

        if (char.ToLower(traderAnswer) == 'y')
            Trader.Run(state);

        Display.ShowMatchEnd(won: true, goodWon, toxicWon, state);
    }
    else
    {
        state.AirPump.Clear();
        state.PrizePool.Clear();
        Display.ShowMatchEnd(won: false, 0, 0, state);

        Console.CursorVisible = true;
        Console.ReadKey(true);
        Console.CursorVisible = false;

        // Full reset — create a brand new state keeping only the difficulty choice
        state = new GameState(rng, state.AiDifficulty);
        continue; // skip the play-again prompt below, go straight back to the loop
    }

    Console.CursorVisible = true;
    char answer = Console.ReadKey(true).KeyChar;
    Console.CursorVisible = false;

    if (char.ToLower(answer) != 'y') break;

    state.MatchNumber++;
}

// ── EXIT ─────────────────────────────────────────────────────────────────────
Console.Clear();
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine("\n\n  Game over. Thanks for playing.");
Console.ResetColor();
Console.ReadKey(true);