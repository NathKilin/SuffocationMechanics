using System;

// The trader screen — shown after winning a match.
// Player can exchange capsules or buy items before the next match.
public static class Trader
{
    private const int GunCost      = 5; // good capsules to buy Empty Gun
    private const int SerumCost    = 3; // good capsules to buy one Serum (prototype price)
    private const int ExchangeCost = 5; // toxic capsules needed to get 1 good

    public static void Run(GameState s)
    {
        // Keep showing the shop until the player leaves
        while (true)
        {
            Display.ShowTrader(s);
            char key = Console.ReadKey(true).KeyChar;

            switch (char.ToLower(key))
            {
                case '1': TryExchange(s); break;
                case '2': TryBuyGun(s);   break;
                case '3': TryBuySerum(s); break;
                case 'c': RunCheat(s);    break;
                case 'x': return;          // leave the shop
            }
        }
    }

    // Exchange 5 Toxic → 1 Breathable
    private static void TryExchange(GameState s)
    {
        if (s.PlayerInventory.ToxicCount < ExchangeCost)
        {
            Display.TraderMessage($"Not enough Toxic capsules. Need {ExchangeCost}, you have {s.PlayerInventory.ToxicCount}.");
            return;
        }
        for (int i = 0; i < ExchangeCost; i++) s.PlayerInventory.TakeOne(CapsuleType.Toxic);
        s.PlayerInventory.Add(new Capsule(CapsuleType.Good));
        Display.TraderMessage($"Deal done. {ExchangeCost} Toxic → 1 Breathable.");
    }

    // Buy Empty Gun for 5 Breathable
    private static void TryBuyGun(GameState s)
    {
        if (s.PlayerHasEmptyGun)
        {
            Display.TraderMessage("You already own an Empty Gun. It stays with you forever.");
            return;
        }
        if (s.PlayerInventory.GoodCount < GunCost)
        {
            Display.TraderMessage($"Not enough Breathable capsules. Need {GunCost}, you have {s.PlayerInventory.GoodCount}.");
            return;
        }
        for (int i = 0; i < GunCost; i++) s.PlayerInventory.TakeOne(CapsuleType.Good);
        s.PlayerHasEmptyGun = true;
        Display.TraderMessage($"Paid {GunCost} Breathable. Empty Gun acquired — it's yours permanently.");
    }

    // Buy 1 Surfactant Serum for 3 Breathable
    private static void TryBuySerum(GameState s)
    {
        if (s.PlayerInventory.ToxicCount < SerumCost)
        {
            Display.TraderMessage($"Not enough Toxic capsules. Need {SerumCost}, you have {s.PlayerInventory.ToxicCount}.");
            return;
        }
        for (int i = 0; i < SerumCost; i++) s.PlayerInventory.TakeOne(CapsuleType.Toxic);
        s.PlayerSerumCount++;
        Display.TraderMessage($"Paid {SerumCost} Toxic. Surfactant Serum acquired. You now have {s.PlayerSerumCount}.");
    }

    // Cheat menu — free items for testing purposes
    private static void RunCheat(GameState s)
    {
        Display.ShowCheatMenu();
        char key = Console.ReadKey(true).KeyChar;

        switch (key)
        {
            case '1':
                s.PlayerHasEmptyGun = true;
                Display.TraderMessage("[CHEAT] Empty Gun added for free.");
                break;
            case '2':
                s.PlayerSerumCount++;
                Display.TraderMessage($"[CHEAT] Surfactant Serum added. You now have {s.PlayerSerumCount}.");
                break;
            case '3':
                for (int i = 0; i < 5; i++) s.PlayerInventory.Add(new Capsule(CapsuleType.Good));
                Display.TraderMessage("[CHEAT] +5 Breathable capsules added.");
                break;
            case '4':
                for (int i = 0; i < 5; i++) s.PlayerInventory.Add(new Capsule(CapsuleType.Toxic));
                Display.TraderMessage("[CHEAT] +5 Toxic capsules added.");
                break;
        }
    }
}