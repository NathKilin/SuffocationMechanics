using System;
using System.Collections.Generic;

// All 5 possible actions a player (or enemy) can take on their turn
public enum TurnAction { Activate, Discard, AddAndActivate, UseEmptyGun, UseSerum }

// The enemy's brain. One call to Decide() returns what the enemy does this turn.
// Change AiDifficulty in GameState to get a completely different opponent.
public static class EnemyAI
{
    private static readonly Random _rng = new Random();

    public static TurnAction Decide(GameState s)
    {
        int good  = s.AirPump.GoodCount;
        int total = s.AirPump.Count;
        bool hasGoodInv = s.EnemyInventory.Has(CapsuleType.Good);
        bool hasAnyInv  = s.EnemyInventory.Count > 0;

        // First turn restriction: nobody can add a capsule from inventory yet
        bool canAdd = !s.IsFirstTurnOfMatch && hasAnyInv;

        switch (s.AiDifficulty)
        {
            case 1:  return Lvl1(total, canAdd);
            case 2:  return Lvl2(good, total, canAdd);
            case 3:  return Lvl3(good, total, hasGoodInv, canAdd);
            case 4:  return Lvl4(s, good, total, hasGoodInv, canAdd);
            case 5:  return Lvl5(s, good, total, hasGoodInv, canAdd);
            default: return Lvl3(good, total, hasGoodInv, canAdd);
        }
    }

    // When the enemy picks AddAndActivate, this decides which capsule type to add
    public static CapsuleType ChooseToAdd(GameState s)
    {
        bool hasGood  = s.EnemyInventory.Has(CapsuleType.Good);
        bool hasToxic = s.EnemyInventory.Has(CapsuleType.Toxic);

        // Low difficulties pick randomly; higher ones always add good if possible
        if (s.AiDifficulty <= 2)
        {
            if (hasGood && hasToxic) return _rng.Next(2) == 0 ? CapsuleType.Good : CapsuleType.Toxic;
            return hasGood ? CapsuleType.Good : CapsuleType.Toxic;
        }
        else
        {
            // Adding good is always smarter — improves the odds of breathing
            if (hasGood) return CapsuleType.Good;
            return CapsuleType.Toxic;
        }
    }

    // ── DIFFICULTY LEVELS ────────────────────────────────────────────────────

    // Level 1: Pure random — no logic, picks anything available
    private static TurnAction Lvl1(int total, bool canAdd)
    {
        var opts = new List<TurnAction>();
        if (total > 0) { opts.Add(TurnAction.Activate); opts.Add(TurnAction.Discard); }
        else             opts.Add(TurnAction.Discard);
        if (canAdd)      opts.Add(TurnAction.AddAndActivate);
        return opts[_rng.Next(opts.Count)];
    }

    // Level 2: Simple count check — activates only if there are more good than bad
    private static TurnAction Lvl2(int good, int total, bool canAdd)
    {
        if (total == 0) return canAdd ? TurnAction.AddAndActivate : TurnAction.Discard;
        return good > total / 2.0 ? TurnAction.Activate : TurnAction.Discard;
    }

    // Level 3: Expected Value (EV) — calculates if inhaling is worth the average risk
    // EV formula: (chance of good * +1) + (chance of toxic * -2)
    // If EV > -1, activating beats the cost of holding breath
    private static TurnAction Lvl3(int good, int total, bool hasGoodInv, bool canAdd)
    {
        if (total == 0) return canAdd ? TurnAction.AddAndActivate : TurnAction.Discard;

        double ev = EV(good, total);
        if (ev > -1.0) return TurnAction.Activate;

        // Check if adding a good capsule from inventory would make activating worthwhile
        if (canAdd && hasGoodInv && EV(good + 1, total + 1) > -1.0)
            return TurnAction.AddAndActivate;

        return TurnAction.Discard;
    }

    // Level 4: EV + health awareness — plays more aggressively when losing
    private static TurnAction Lvl4(GameState s, int good, int total, bool hasGoodInv, bool canAdd)
    {
        if (total == 0) return canAdd ? TurnAction.AddAndActivate : TurnAction.Discard;

        double ev = EV(good, total);
        int healthDiff = s.EnemyHealth - s.PlayerHealth; // positive = enemy is winning

        if (healthDiff > 3 && ev < 0)   return TurnAction.Discard;   // winning a lot, play safe
        if (healthDiff < -2 && total > 0) return TurnAction.Activate; // losing badly, take the risk

        if (ev > -1.0) return TurnAction.Activate;

        if (canAdd && hasGoodInv && EV(good + 1, total + 1) > -1.0)
            return TurnAction.AddAndActivate;

        return TurnAction.Discard;
    }

    // Level 5: Full strategy — considers all factors and plans ahead
    private static TurnAction Lvl5(GameState s, int good, int total, bool hasGoodInv, bool canAdd)
    {
        if (total == 0)
        {
            if (canAdd && hasGoodInv) return TurnAction.AddAndActivate;
            return TurnAction.Discard;
        }

        double ev = EV(good, total);
        int healthDiff = s.EnemyHealth - s.PlayerHealth;

        // At critical health with good capsules available: always go for it
        if (s.EnemyHealth <= 3 && good > 0) return TurnAction.Activate;

        // If adding a good capsule significantly improves EV, do it
        if (canAdd && hasGoodInv)
        {
            double evAfterAdd = EV(good + 1, total + 1);
            if (evAfterAdd > ev + 0.3 && evAfterAdd > -0.5)
                return TurnAction.AddAndActivate;
        }

        if (ev >= 0)               return TurnAction.Activate;              // always activate with positive EV
        if (ev >= -0.5 && healthDiff < 0) return TurnAction.Activate;      // losing: accept mild negative EV
        if (healthDiff > 2)        return TurnAction.Discard;               // winning comfortably: conserve
        if (ev > -1.0)             return TurnAction.Activate;

        return TurnAction.Discard;
    }

    // The core math: what is the average health change of drawing a random capsule?
    // Returns a number between -2.0 (all toxic) and +1.0 (all good)
    private static double EV(int good, int total)
    {
        if (total <= 0) return -2.0;
        double pGood = (double)good / total;
        return pGood * 1.0 + (1.0 - pGood) * (-2.0);
    }
}