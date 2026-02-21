using Godot;
using System.Collections.Generic;

/// <summary>
/// Sorts and activates cards in the correct order during the Battle Phase.
///
/// Activation order:
///   1. Speed tier: Burst (0) → Fast (1) → Slow (2)
///   2. Within the same tier: higher character Speed stat goes first.
///
/// M1 — stub. Will be fully wired in M2 when cards can be played.
/// </summary>
public class ActivationQueue
{
    // -------------------------------------------------------------------------
    // Queue Entry
    // -------------------------------------------------------------------------

    public record QueueEntry(CardData Card, Node3D Caster, int CharacterSpeed);

    private readonly List<QueueEntry> _entries = new();

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Add a card-play action to the queue.</summary>
    public void Enqueue(CardData card, Node3D caster, int characterSpeed)
    {
        _entries.Add(new QueueEntry(card, caster, characterSpeed));
    }

    /// <summary>Sort cards by speed tier then character Speed stat (descending).</summary>
    public void Sort()
    {
        _entries.Sort((a, b) =>
        {
            int tierCompare = ((int)a.Card.Speed).CompareTo((int)b.Card.Speed);
            if (tierCompare != 0) return tierCompare;
            // Higher character speed goes first within same tier
            return b.CharacterSpeed.CompareTo(a.CharacterSpeed);
        });
    }

    /// <summary>Resolve all queued cards in sorted order, then clear.</summary>
    public void ResolveAll()
    {
        Sort();
        foreach (var entry in _entries)
        {
            GD.Print($"[ActivationQueue] Resolving: {entry.Card.Name} (tier:{entry.Card.Speed}) by {entry.Caster.Name}");
            // TODO (M2): call entry.Card's CardEffect.Execute(...)
        }
        _entries.Clear();
    }

    public void Clear() => _entries.Clear();

    public int Count => _entries.Count;
}
