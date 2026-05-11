using System;
using System.Threading;

// Everything the player sees on screen comes from here.
// Call Render() after any state change to redraw the whole UI from scratch.
public static class Display
{
    // Total width of the frame in characters
    private const int W = 76;

    // Column widths for the 3-panel layout
    // 20 + 32 + 20 = 72, plus 4 border chars (the ║ symbols) = 76 total
    private const int LW = 20; // Player column
    private const int MW = 32; // Air Pump column
    private const int RW = 20; // Enemy column

    public static void Pause(int ms = 4500)      => Thread.Sleep(ms);
    public static void ShortPause(int ms = 3200) => Thread.Sleep(ms);
    public static void HideInput()               => Console.CursorVisible = false;

    // Redraws the entire game screen from scratch
    public static void Render(GameState s, string? ev = null)
    {
        if (ev != null) s.LastEvent = ev;
        // ANSI: move cursor to top-left first, then clear visible screen
        // More reliable than Console.Clear() inside the VS Code terminal
        Console.Write("\x1b[H\x1b[2J");

        // ── TITLE BLOCK ─────────────────────────────────────────────────────
        Border('╔', '═', '╗');
        FullRow("  ✦  S U F F O C A T I O N  ✦", ConsoleColor.White);
        FullRow($"  Match #{s.MatchNumber}   |   AI Difficulty: {s.AiDifficulty}", ConsoleColor.DarkGray);

        // ── 3-COLUMN HEADER ─────────────────────────────────────────────────
        SplitBorder('╠', '╦', '╦', '╣');

        // Arrows appear on whoever's turn it currently is
        string playerHeader = s.IsPlayerTurn  ? "◄  PLAYER  ►" : "   PLAYER   ";
        string enemyHeader  = !s.IsPlayerTurn ? "◄  ENEMY   ►" : "   ENEMY    ";

        UI("║");
        Ink(Center(playerHeader, LW), s.IsPlayerTurn ? ConsoleColor.Green : ConsoleColor.DarkGreen);
        UI("║");
        Ink(Center("AIR  PUMP", MW), ConsoleColor.White);
        UI("║");
        Ink(Center(enemyHeader, RW), !s.IsPlayerTurn ? ConsoleColor.Magenta : ConsoleColor.DarkMagenta);
        UI("║\n");

        // Blank spacer row inside columns
        UI("║" + Spaces(LW) + "║" + Spaces(MW) + "║" + Spaces(RW) + "║\n");

        // ── HEALTH + PUMP DATA ───────────────────────────────────────────────
        // Breath counters row
        UI("║");
        Ink(Pad($"  Breath: {s.PlayerHealth} / {s.MaxHealth}", LW), ConsoleColor.Green);
        UI("║");
        Ink(Pad($"  ◉  Breathable  :  {s.AirPump.GoodCount}", MW), ConsoleColor.Cyan);
        UI("║");
        Ink(Pad($"  Breath: {s.EnemyHealth} / {s.MaxHealth}", RW), ConsoleColor.Magenta);
        UI("║\n");

        // Health bars + toxic count row
        UI("║");
        Ink(Pad("  " + Bar(s.PlayerHealth, s.MaxHealth, 16), LW), ConsoleColor.Green);
        UI("║");
        Ink(Pad($"  ✕  Toxic       :  {s.AirPump.ToxicCount}", MW), ConsoleColor.Red);
        UI("║");
        Ink(Pad("  " + Bar(s.EnemyHealth, s.MaxHealth, 16), RW), ConsoleColor.Magenta);
        UI("║\n");

        // Pump total row
        UI("║" + Spaces(LW) + "║");
        Ink(Pad($"  ─  Total       :  {s.AirPump.Count}", MW), ConsoleColor.DarkGray);
        UI("║" + Spaces(RW) + "║\n");

        // ── INVENTORY + ITEMS + PRIZE POOL ─────────────────────────────────────
        SplitBorder('╠', '╩', '╩', '╣');
        FullRow($"  INVENTORY   ▸   Breathable: {s.PlayerInventory.GoodCount}   Toxic: {s.PlayerInventory.ToxicCount}", ConsoleColor.Cyan);

        string gunStatus   = !s.PlayerHasEmptyGun   ? "--"
                           : s.EmptyGunPrimed        ? "PRIMED ◄"
                           : s.EmptyGunUsedThisMatch ? "used"
                           : "READY";
        string serumStatus = s.PlayerSerumCount == 0 ? "none"
                           : s.SerumActiveThisTurn   ? $"{s.PlayerSerumCount} [ACTIVE!]"
                           : $"{s.PlayerSerumCount}";
        FullRow($"  ITEMS       ▸   Gun: {gunStatus,-16}  Serum: {serumStatus}", ConsoleColor.Magenta);

        FullRow($"  PRIZE POOL  ▸   Breathable: {s.PrizePool.GoodCount}   Toxic: {s.PrizePool.ToxicCount}", ConsoleColor.DarkYellow);

        // ── EVENT LOG ────────────────────────────────────────────────────────
        Border('╠', '═', '╣');
        UI("║");
        Ink(Pad($"  ▶  {s.LastEvent}", W - 2), ConsoleColor.Yellow);
        UI("║\n");
        Border('╚', '═', '╝');

        Console.ResetColor();
    }

    // Numbered option menu shown below the game board
    public static void ShowMenu(string[] options)
    {
        Console.WriteLine();
        for (int i = 0; i < options.Length; i++)
        {
            Ink($"  [{i + 1}] ", ConsoleColor.DarkYellow);
            Ink(options[i] + "\n", ConsoleColor.White);
        }
        Ink("\n  > ", ConsoleColor.Cyan);
        Console.CursorVisible = true;
        Console.ResetColor();
    }

    // One step of the bet selection — used twice (once for good, once for toxic)
    public static void ShowBetStep(GameState s, string prompt, int max, int alreadyChosenGood = -1)
    {
        Render(s, "The enemy awaits your wager...");
        Ink($"\n  Your inventory: {s.PlayerInventory.GoodCount} Breathable   {s.PlayerInventory.ToxicCount} Toxic\n\n", ConsoleColor.DarkCyan);

        if (alreadyChosenGood >= 0)
            Ink($"  Breathable to bet: {alreadyChosenGood}\n", ConsoleColor.Cyan);

        Ink($"  {prompt} (1 – {max})\n\n  > ", ConsoleColor.Yellow);
        Console.CursorVisible = true;
        Console.ResetColor();
    }

    // The 50/50 hand-guessing challenge shown at the start of each match
    public static void ShowChallengePrompt(GameState s)
    {
        Render(s, "The enemy extends two closed fists...");
        Ink("\n  One hand holds a Breathable capsule. One holds Toxic.\n", ConsoleColor.White);
        Ink("  Guess correctly to go first.\n\n", ConsoleColor.Gray);
        Ink("  [1]  Left hand     [2]  Right hand\n\n  > ", ConsoleColor.DarkYellow);
        Console.CursorVisible = true;
        Console.ResetColor();
    }

    // Win or loss screen shown at the end of a match
    public static void ShowMatchEnd(bool won, int goodWon, int toxicWon, GameState s)
    {
        Console.Clear();

        if (won)
        {
            Ink("\n\n  ╔══════════════════════════════╗\n", ConsoleColor.Green);
            Ink("  ║        YOU SURVIVED          ║\n", ConsoleColor.Green);
            Ink("  ╚══════════════════════════════╝\n\n", ConsoleColor.Green);
            Ink($"  You won {goodWon} Breathable + {toxicWon} Toxic capsules.\n", ConsoleColor.White);
        }
        else
        {
            Ink("\n\n  ╔══════════════════════════════╗\n", ConsoleColor.Red);
            Ink("  ║       YOU SUFFOCATED         ║\n", ConsoleColor.Red);
            Ink("  ╚══════════════════════════════╝\n\n", ConsoleColor.Red);
        }

        Ink($"  Current inventory:  {s.PlayerInventory.GoodCount} Breathable   {s.PlayerInventory.ToxicCount} Toxic\n\n", ConsoleColor.DarkGray);

        bool canContinue = s.PlayerInventory.GoodCount >= 1 && s.PlayerInventory.ToxicCount >= 1;
        if (canContinue)
            Ink("  Play another match?   [Y] Yes     [N] No\n\n  > ", ConsoleColor.Gray);
        else
            Ink("  Not enough capsules to continue. Press any key.\n\n  > ", ConsoleColor.DarkRed);

        Console.CursorVisible = true;
        Console.ResetColor();
    }

    public static void ShowOutOfCapsules()
    {
        Console.Clear();
        Ink("\n\n  You have no capsules left. The game is over.\n\n", ConsoleColor.Red);
        Ink("  Press any key to exit...\n", ConsoleColor.DarkGray);
        Console.ReadKey(true);
    }

    // Prompt shown after winning: visit trader or skip
    public static void ShowTraderPrompt(GameState s)
    {
        Console.Write("\x1b[H\x1b[2J");
        Ink("\n\n  ╔══════════════════════════════════════════╗\n", ConsoleColor.DarkYellow);
        Ink("  ║            THE TRADER AWAITS            ║\n", ConsoleColor.DarkYellow);
        Ink("  ╚══════════════════════════════════════════╝\n\n", ConsoleColor.DarkYellow);
        Ink($"  Inventory:  {s.PlayerInventory.GoodCount} Breathable   {s.PlayerInventory.ToxicCount} Toxic", ConsoleColor.DarkGray);
        if (s.PlayerHasEmptyGun) Ink("   Gun: OWNED", ConsoleColor.Magenta);
        if (s.PlayerSerumCount > 0) Ink($"   Serum: {s.PlayerSerumCount}", ConsoleColor.Magenta);
        Ink("\n\n  Visit the trader before the next match?\n\n", ConsoleColor.White);
        Ink("  [Y] Enter shop     [N] Skip\n\n  > ", ConsoleColor.Gray);
        Console.CursorVisible = true;
        Console.ResetColor();
    }

    // Main trader shop screen
    public static void ShowTrader(GameState s)
    {
        Console.Write("\x1b[H\x1b[2J");
        Ink("\n  ╔════════════════════════════════════════════════════════════════════════╗\n", ConsoleColor.DarkYellow);
        Ink("  ║                         T H E  T R A D E R                          ║\n", ConsoleColor.DarkYellow);
        Ink("  ╚════════════════════════════════════════════════════════════════════════╝\n\n", ConsoleColor.DarkYellow);

        Ink($"  Inventory:  {s.PlayerInventory.GoodCount} Breathable   {s.PlayerInventory.ToxicCount} Toxic", ConsoleColor.Cyan);
        if (s.PlayerHasEmptyGun) Ink("   │  Gun: OWNED", ConsoleColor.Magenta);
        if (s.PlayerSerumCount > 0) Ink($"   │  Serum: {s.PlayerSerumCount}", ConsoleColor.Magenta);
        Ink("\n\n", ConsoleColor.White);

        Ink("  [1]  Exchange 5 Toxic → 1 Breathable\n", ConsoleColor.White);

        Ink("  [2]  Buy Empty Gun  ", ConsoleColor.White);
        if (s.PlayerHasEmptyGun)
            Ink("(already owned)\n", ConsoleColor.DarkGray);
        else
            Ink("— costs 5 Breathable  │  PERMANENT — forces enemy to inhale next turn, once per match\n", ConsoleColor.Gray);

        Ink("  [3]  Buy Surfactant Serum ", ConsoleColor.White);
        Ink("— costs 3 Toxic  │  CONSUMABLE — doubles your next capsule effect (good or bad)\n", ConsoleColor.Gray);

        Ink("\n  [C]  Cheat menu (testing)\n", ConsoleColor.DarkGray);
        Ink("  [X]  Leave shop\n\n  > ", ConsoleColor.DarkGray);
        Console.CursorVisible = true;
        Console.ResetColor();
    }

    // Message shown inline in the trader screen after a transaction
    public static void TraderMessage(string msg)
    {
        Ink($"\n  ▶  {msg}\n", ConsoleColor.Yellow);
        Ink("  Press any key to continue...", ConsoleColor.DarkGray);
        Console.ReadKey(true);
        Console.ResetColor();
    }

    // Cheat sub-menu
    public static void ShowCheatMenu()
    {
        Ink("\n  ── CHEAT MENU ──────────────────────────────\n", ConsoleColor.DarkRed);
        Ink("  [1]  Get Empty Gun for free\n", ConsoleColor.Red);
        Ink("  [2]  Get 1 Surfactant Serum for free\n", ConsoleColor.Red);
        Ink("  [3]  Add 5 Breathable to inventory\n", ConsoleColor.Red);
        Ink("  [4]  Add 5 Toxic to inventory\n", ConsoleColor.Red);
        Ink("\n  > ", ConsoleColor.DarkRed);
        Console.CursorVisible = true;
        Console.ResetColor();
    }

    // ── PRIVATE HELPERS ──────────────────────────────────────────────────────

    // Draws a health bar: ████████████░░░░
    private static string Bar(int current, int max, int length)
    {
        if (max <= 0) return new string('░', length);
        int filled = (int)Math.Round((double)Math.Max(0, current) / max * length);
        filled = Math.Min(filled, length);
        return new string('█', filled) + new string('░', length - filled);
    }

    // Centers text inside a fixed-width space
    private static string Center(string text, int width)
    {
        if (text.Length >= width) return text.Substring(0, width);
        int pad = width - text.Length;
        return new string(' ', pad / 2) + text + new string(' ', pad - pad / 2);
    }

    // Pads text with trailing spaces to fill a fixed width
    private static string Pad(string text, int width)
    {
        if (text.Length >= width) return text.Substring(0, width);
        return text + new string(' ', width - text.Length);
    }

    private static string Spaces(int n) => new string(' ', n);

    // Full-width horizontal border: ╔══════════════╗
    private static void Border(char left, char fill, char right)
    {
        UI(left + new string(fill, W - 2) + right + "\n");
    }

    // Full-width content row: ║  text here  ║
    private static void FullRow(string text, ConsoleColor color)
    {
        UI("║");
        Ink(Pad(text, W - 2), color);
        UI("║\n");
    }

    // 3-column border row — separators can be ╦ (merge down) or ╩ (merge up)
    private static void SplitBorder(char left, char midA, char midB, char right)
    {
        UI($"{left}{new string('═', LW)}{midA}{new string('═', MW)}{midB}{new string('═', RW)}{right}\n");
    }

    // Write border/frame characters in dark gray
    private static void UI(string text)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(text);
    }

    // Write colored content text
    private static void Ink(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
    }
}