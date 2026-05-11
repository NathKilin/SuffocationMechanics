using System.Collections.Generic;

public enum TurnResult { Continue, PlayerDied, EnemyDied }

public class Match
{
    private GameState _s;
    private Random    _rng;

    public Match(GameState state, Random rng)
    {
        _s   = state;
        _rng = rng;
    }

    public bool Run()
    {
        SetupMatch();
        bool playerFirst = LuckyChallenge();
        _s.IsPlayerTurn       = playerFirst;
        _s.IsFirstTurnOfMatch = true;

        while (true)
        {
            TurnResult result = _s.IsPlayerTurn ? PlayerTurn() : EnemyTurn();
            if (result == TurnResult.PlayerDied) return false;
            if (result == TurnResult.EnemyDied)  return true;
            _s.IsFirstTurnOfMatch = false;
            _s.IsPlayerTurn       = !_s.IsPlayerTurn;
        }
    }

    // ── SETUP ───────────────────────────────────────────────────────────────

    private void SetupMatch()
    {
        _s.AirPump.Clear();
        _s.PrizePool.Clear();

        // Reset all per-match item state
        _s.EmptyGunUsedThisMatch = false;
        _s.EmptyGunPrimed        = false;
        _s.SerumUsedThisMatch    = false;
        _s.SerumActiveThisTurn   = false;

        while (_s.EnemyInventory.GoodCount  < 6) _s.EnemyInventory.Add(new Capsule(CapsuleType.Good));
        while (_s.EnemyInventory.ToxicCount < 6) _s.EnemyInventory.Add(new Capsule(CapsuleType.Toxic));

        // Each type is capped at 6 independently — no shared total limit
        int maxGood  = Math.Min(_s.PlayerInventory.GoodCount,  6);
        int maxToxic = Math.Min(_s.PlayerInventory.ToxicCount, 6);

        Display.ShowBetStep(_s, $"How many Breathable to bet? (max {maxGood})", maxGood);
        int goodBet = ReadInt(1, maxGood);
        Display.HideInput();
        Display.ShortPause();

        Display.ShowBetStep(_s, $"How many Toxic to bet? (max {maxToxic})", maxToxic, alreadyChosenGood: goodBet);
        int toxicBet = ReadInt(1, maxToxic);
        Display.HideInput();
        Display.ShortPause();

        for (int i = 0; i < goodBet;  i++) _s.AirPump.Add(_s.PlayerInventory.TakeOne(CapsuleType.Good));
        for (int i = 0; i < toxicBet; i++) _s.AirPump.Add(_s.PlayerInventory.TakeOne(CapsuleType.Toxic));
        for (int i = 0; i < goodBet;  i++) _s.PrizePool.Add(_s.EnemyInventory.TakeOne(CapsuleType.Good));
        for (int i = 0; i < toxicBet; i++) _s.PrizePool.Add(_s.EnemyInventory.TakeOne(CapsuleType.Toxic));

        int hp = _s.AirPump.Count + 1;
        _s.MaxHealth = _s.PlayerHealth = _s.EnemyHealth = hp;

        Display.Render(_s, $"Wager set! Pump: {goodBet}G + {toxicBet}T. Prize: {goodBet}G + {toxicBet}T guaranteed. Both start with {hp} Breath.");
        Display.Pause();
    }

    // ── LUCKY CHALLENGE ─────────────────────────────────────────────────────

    private bool LuckyChallenge()
    {
        Display.ShowChallengePrompt(_s);
        int choice = ReadInt(1, 2);
        Display.HideInput();
        Display.ShortPause();

        int  correct = _rng.Next(1, 3);
        // Subtle first-match advantage: the correct hand always matches the player's choice
        if (_s.MatchNumber == 1) correct = choice;
        bool won     = choice == correct;
        string where = correct == 1 ? "LEFT" : "RIGHT";
        string result = won
            ? $"Correct! The Breathable was in the {where} hand. You go first."
            : $"Wrong. The Breathable was in the {where} hand. Enemy goes first.";

        Display.Render(_s, result);
        Display.Pause();
        return won;
    }

    // ── PLAYER TURN ─────────────────────────────────────────────────────────

    private TurnResult PlayerTurn()
    {
        if (!_s.IsFirstTurnOfMatch)
        {
            _s.PlayerHealth--;
            Display.Render(_s, "Your oxygen depletes passively... -1 Breath.");
            Display.ShortPause();

            if (_s.PlayerHealth <= 0)
            {
                Display.Render(_s, "You suffocate before you can act. You lose.");
                Display.Pause();
                return TurnResult.PlayerDied;
            }
        }

        var actions = BuildPlayerActions();
        Display.Render(_s, "YOUR TURN — choose an action:");
        Display.ShowMenu(actions.ConvertAll(a => a.label).ToArray());

        int pick = ReadInt(1, actions.Count);
        Display.HideInput();
        Display.ShortPause();

        var chosen = actions[pick - 1];

        // Items are used BEFORE the capsule act — after picking an item, show capsule sub-menu
        if (chosen.action == TurnAction.UseEmptyGun)
        {
            // Note (future): design allows cancelling the gun before the next step
            _s.EmptyGunPrimed = true;
            Display.Render(_s, "You load the Empty Gun under the table... it's aimed. Next enemy turn: they MUST inhale.");
            Display.ShortPause();
            return DoCapsuleSubMenu("Empty Gun primed — now choose your capsule act:");
        }

        if (chosen.action == TurnAction.UseSerum)
        {
            _s.SerumUsedThisMatch  = true;
            _s.SerumActiveThisTurn = true;
            _s.PlayerSerumCount--;
            Display.Render(_s, "You inject the Surfactant Serum... next capsule effect is DOUBLED. No turning back.");
            Display.ShortPause();
            return DoCapsuleSubMenu("SERUM ACTIVE — choose your capsule act (effect will be doubled):");
        }

        return ExecuteAction(chosen.action, forPlayer: true);
    }

    // Shown after using an item — only capsule acts, no items in this sub-menu
    private TurnResult DoCapsuleSubMenu(string prompt)
    {
        var capsuleActions = BuildCapsuleActions(forPlayer: true);
        Display.Render(_s, prompt);
        Display.ShowMenu(capsuleActions.ConvertAll(a => a.label).ToArray());

        int pick = ReadInt(1, capsuleActions.Count);
        Display.HideInput();
        Display.ShortPause();

        return ExecuteAction(capsuleActions[pick - 1].action, forPlayer: true);
    }

    // ── ENEMY TURN ──────────────────────────────────────────────────────────

    private TurnResult EnemyTurn()
    {
        if (!_s.IsFirstTurnOfMatch)
        {
            _s.EnemyHealth--;
            Display.Render(_s, "The enemy's oxygen depletes passively... -1 Breath.");
            Display.ShortPause();

            if (_s.EnemyHealth <= 0)
            {
                Display.Render(_s, "The enemy suffocates. You win!");
                Display.Pause();
                return TurnResult.EnemyDied;
            }
        }

        // Empty Gun fires here — enemy is forced to inhale, no decision
        if (_s.EmptyGunPrimed)
        {
            _s.EmptyGunPrimed        = false;
            _s.EmptyGunUsedThisMatch = true;
            Display.Render(_s, "You raise the Empty Gun from under the table...");
            Display.Pause();
            Display.Render(_s, "The enemy's eyes go wide. They have no choice.");
            Display.ShortPause();
            return DoActivate(forPlayer: false);
        }

        TurnAction action = EnemyAI.Decide(_s);

        string announce = action switch
        {
            TurnAction.Activate       => "The enemy reaches for the air pump...",
            TurnAction.Discard        => "The enemy holds their breath...",
            TurnAction.AddAndActivate => "The enemy pulls a capsule from their pocket and loads it...",
            _                         => "The enemy is deciding..."
        };

        Display.Render(_s, announce);
        Display.ShortPause();

        return ExecuteAction(action, forPlayer: false);
    }

    // ── ACTIONS ─────────────────────────────────────────────────────────────

    private TurnResult ExecuteAction(TurnAction action, bool forPlayer)
    {
        switch (action)
        {
            case TurnAction.Activate:       return DoActivate(forPlayer);
            case TurnAction.Discard:        return DoDiscard(forPlayer);
            case TurnAction.AddAndActivate: return DoAddAndActivate(forPlayer);
            default:                        return TurnResult.Continue;
        }
    }

    private TurnResult DoActivate(bool forPlayer)
    {
        if (_s.AirPump.Count == 0)
        {
            string emptyMsg = forPlayer ? "You reach for the pump — it's empty!" : "The enemy finds the pump empty!";
            Display.Render(_s, emptyMsg + " Forced to hold breath instead.");
            Display.ShortPause();
            return DoDiscard(forPlayer);
        }

        // First match subtle advantage: if there's at least one good capsule, the player always draws it
        // If odds are 0% (no good capsules) nothing can be done — purely random in that case
        Capsule drawn;
        if (forPlayer && _s.MatchNumber == 1 && _s.AirPump.GoodCount > 0)
            drawn = _s.AirPump.TakeOne(CapsuleType.Good);
        else
            drawn = _s.AirPump.DrawRandom();
        bool isGood = drawn.Type == CapsuleType.Good;
        bool    serum     = forPlayer && _s.SerumActiveThisTurn;
        if (serum) _s.SerumActiveThisTurn = false; // serum consumed after this capsule act

        if (forPlayer)
        {
            if (isGood)
            {
                int gain = serum ? 2 : 1;
                _s.PlayerHealth = Math.Min(_s.PlayerHealth + gain, _s.MaxHealth);
                Display.Render(_s, serum
                    ? "You inhale...  BREATHABLE!  SERUM DOUBLES IT!  +2 Breath."
                    : "You inhale...  BREATHABLE!  +1 Breath.");
            }
            else
            {
                int dmg = serum ? 4 : 2;
                _s.PlayerHealth -= dmg;
                Display.Render(_s, serum
                    ? "You inhale...  TOXIC!  SERUM DOUBLES IT!  -4 Breath."
                    : "You inhale...  TOXIC!  -2 Breath.");
            }
        }
        else
        {
            if (isGood)
            {
                _s.EnemyHealth = Math.Min(_s.EnemyHealth + 1, _s.MaxHealth);
                Display.Render(_s, "The enemy inhales...  Breathable.  Enemy +1 Breath.");
            }
            else
            {
                _s.EnemyHealth -= 2;
                Display.Render(_s, "The enemy inhales...  Toxic.  Enemy -2 Breath.");
            }
        }

        // All inhaled capsules convert to Toxic before entering the prize pool
        _s.PrizePool.Add(new Capsule(CapsuleType.Toxic));
        Display.Pause();
        return CheckDeath();
    }

    private TurnResult DoDiscard(bool forPlayer)
    {
        string who         = forPlayer ? "You" : "Enemy";
        string capsuleInfo = "The pump is empty — nothing to discard.";

        if (_s.AirPump.Count > 0)
        {
            Capsule discarded  = _s.AirPump.DrawRandom();
            string capsuleName = discarded.Type == CapsuleType.Good ? "Breathable" : "Toxic";
            _s.PrizePool.Add(new Capsule(discarded.Type)); // unchanged type when discarded
            capsuleInfo = $"A {capsuleName} capsule slips out to the prize pool.";
        }

        bool serum = forPlayer && _s.SerumActiveThisTurn;
        if (serum) _s.SerumActiveThisTurn = false;

        int dmg = serum ? 2 : 1;
        if (forPlayer) _s.PlayerHealth -= dmg;
        else           _s.EnemyHealth  -= dmg;

        Display.Render(_s, serum
            ? $"You hold breath. SERUM DOUBLES IT! -2 extra Breath.  {capsuleInfo}"
            : $"{who} hold{(forPlayer ? "" : "s")} breath. -1 extra Breath.  {capsuleInfo}");
        Display.Pause();
        return CheckDeath();
    }

    private TurnResult DoAddAndActivate(bool forPlayer)
    {
        CapsulePool inv = forPlayer ? _s.PlayerInventory : _s.EnemyInventory;

        if (inv.Count == 0) return DoActivate(forPlayer);

        CapsuleType typeToAdd;

        if (forPlayer)
        {
            var addOpts = new List<(string label, CapsuleType type)>();
            if (inv.Has(CapsuleType.Good))  addOpts.Add(("Breathable capsule", CapsuleType.Good));
            if (inv.Has(CapsuleType.Toxic)) addOpts.Add(("Toxic capsule",      CapsuleType.Toxic));

            Display.Render(_s, "Which capsule do you want to add from your inventory?");
            Display.ShowMenu(addOpts.ConvertAll(o => o.label).ToArray());

            int choice = ReadInt(1, addOpts.Count);
            Display.HideInput();
            Display.ShortPause();
            typeToAdd = addOpts[choice - 1].type;
        }
        else
        {
            typeToAdd = EnemyAI.ChooseToAdd(_s);
        }

        Capsule? added = inv.TakeOne(typeToAdd);
        if (added != null) _s.AirPump.Add(added);

        string name = typeToAdd == CapsuleType.Good ? "Breathable" : "Toxic";
        string who  = forPlayer ? "You add" : "Enemy adds";
        Display.Render(_s, $"{who} a {name} capsule to the pump. Now activating from it...");
        Display.ShortPause();

        return DoActivate(forPlayer);
    }

    // ── HELPERS ─────────────────────────────────────────────────────────────

    private TurnResult CheckDeath()
    {
        if (_s.PlayerHealth <= 0) return TurnResult.PlayerDied;
        if (_s.EnemyHealth  <= 0) return TurnResult.EnemyDied;
        return TurnResult.Continue;
    }

    // Full menu: capsule acts + item acts (shown at the start of a player turn)
    private List<(string label, TurnAction action)> BuildPlayerActions()
    {
        var list         = new List<(string, TurnAction)>();
        bool pumpHasCaps = _s.AirPump.Count > 0;
        bool hasInv      = _s.PlayerInventory.Count > 0;

        if (pumpHasCaps)
            list.Add(("Inhale — activate a random capsule from the pump", TurnAction.Activate));

        list.Add(("Hold breath — discard a capsule without inhaling, -1 extra Breath", TurnAction.Discard));

        // Allow adding from inventory whether the pump is empty or not
        if (hasInv)
            list.Add(("Add from your inventory to the pump, then activate", TurnAction.AddAndActivate));

        if (_s.PlayerHasEmptyGun && !_s.EmptyGunUsedThisMatch && !_s.EmptyGunPrimed)
            list.Add(("Use Empty Gun — forces enemy to inhale next turn", TurnAction.UseEmptyGun));

        if (_s.PlayerSerumCount > 0 && !_s.SerumUsedThisMatch)
            list.Add(($"Use Surfactant Serum  [{_s.PlayerSerumCount} left] — doubles next capsule effect", TurnAction.UseSerum));

        return list;
    }

    // Capsule-only sub-menu shown after using an item (no items listed here)
    private List<(string label, TurnAction action)> BuildCapsuleActions(bool forPlayer)
    {
        var list         = new List<(string, TurnAction)>();
        CapsulePool inv  = forPlayer ? _s.PlayerInventory : _s.EnemyInventory;
        bool pumpHasCaps = _s.AirPump.Count > 0;

        if (pumpHasCaps)
            list.Add(("Inhale — activate a random capsule from the pump", TurnAction.Activate));

        list.Add(("Hold breath — discard a capsule without inhaling, -1 extra Breath", TurnAction.Discard));

        // Same rule: allow adding from inventory even if pump is empty
        if (inv.Count > 0)
            list.Add(("Add from your inventory to the pump, then activate", TurnAction.AddAndActivate));

        return list;
    }

    private static int ReadInt(int min, int max)
    {
        while (true)
        {
            var key = Console.ReadKey(true);
            if (int.TryParse(key.KeyChar.ToString(), out int v) && v >= min && v <= max)
                return v;
        }
    }
}