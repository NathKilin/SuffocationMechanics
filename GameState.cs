using System;

// Everything the game needs to know at any moment lives here.
public class GameState
{
    public int PlayerHealth { get; set; }
    public int EnemyHealth  { get; set; }
    public int MaxHealth    { get; set; }

    public CapsulePool PlayerInventory { get; }
    public CapsulePool EnemyInventory  { get; }
    public CapsulePool AirPump         { get; }
    public CapsulePool PrizePool       { get; }

    public int  AiDifficulty       { get; }
    public int  MatchNumber        { get; set; } = 1;
    public bool IsPlayerTurn       { get; set; }
    public bool IsFirstTurnOfMatch { get; set; }

    public string LastEvent { get; set; } = "...";

    // ── ITEMS ────────────────────────────────────────────────────────────────

    // Empty Gun — permanent item, player keeps it forever once bought
    public bool PlayerHasEmptyGun     { get; set; } = false;
    // These two reset at the start of every match
    public bool EmptyGunUsedThisMatch { get; set; } = false;
    public bool EmptyGunPrimed        { get; set; } = false; // set on player's turn, fires on enemy's next turn

    // Surfactant Serum — consumable, disappears after use
    public int  PlayerSerumCount    { get; set; } = 0;
    public bool SerumUsedThisMatch  { get; set; } = false; // only 1 serum per match even with multiple
    public bool SerumActiveThisTurn { get; set; } = false; // cleared after the capsule act resolves

    public GameState(Random rng, int aiDifficulty)
    {
        AiDifficulty    = aiDifficulty;
        PlayerInventory = new CapsulePool(rng);
        EnemyInventory  = new CapsulePool(rng);
        AirPump         = new CapsulePool(rng);
        PrizePool       = new CapsulePool(rng);

        for (int i = 0; i < 4; i++)
        {
            PlayerInventory.Add(new Capsule(CapsuleType.Good));
            PlayerInventory.Add(new Capsule(CapsuleType.Toxic));
            EnemyInventory.Add(new Capsule(CapsuleType.Good));
            EnemyInventory.Add(new Capsule(CapsuleType.Toxic));
        }
    }
}