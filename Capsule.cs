using System;
using System.Collections.Generic;
using System.Linq;

// The two possible states a capsule can be in
public enum CapsuleType { Good, Toxic }

// A single capsule — just needs to know what type it is
public class Capsule
{
    public CapsuleType Type { get; }
    public Capsule(CapsuleType type) => Type = type;
}

// A bag of capsules — used for inventory, the air pump, and the prize pool.
// All three locations in the game use this same structure.
public class CapsulePool
{
    private readonly List<Capsule> _list = new List<Capsule>();
    private readonly Random _rng;

    public int Count      => _list.Count;
    public int GoodCount  => _list.Count(c => c.Type == CapsuleType.Good);
    public int ToxicCount => _list.Count(c => c.Type == CapsuleType.Toxic);

    public CapsulePool(Random rng) => _rng = rng;

    public void Add(Capsule c) => _list.Add(c);

    // Pull out a random capsule and remove it from the pool
    public Capsule DrawRandom()
    {
        if (_list.Count == 0) return null;
        int i = _rng.Next(_list.Count);
        var c = _list[i];
        _list.RemoveAt(i);
        return c;
    }

    // Remove and return the first capsule of a specific type found
    public Capsule TakeOne(CapsuleType type)
    {
        var c = _list.FirstOrDefault(x => x.Type == type);
        if (c != null) _list.Remove(c);
        return c;
    }

    // Dump everything from this pool into another pool
    public void TransferAllTo(CapsulePool other)
    {
        foreach (var c in _list) other.Add(c);
        _list.Clear();
    }

    public void Clear() => _list.Clear();
    public bool Has(CapsuleType type) => _list.Any(c => c.Type == type);
}