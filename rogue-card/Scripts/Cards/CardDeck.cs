using System.Collections.Generic;
using Godot;

/// <summary>
/// Manages a character's card deck: the full shuffled draw pile and the discard pile.
/// </summary>
public partial class CardDeck : RefCounted
{
    private readonly List<CardData> _drawPile    = new();
    private readonly List<CardData> _discardPile = new();

    /// <summary>Read-only view of the draw pile.</summary>
    public IReadOnlyList<CardData> DrawPile => _drawPile;

    /// <summary>Read-only view of the discard pile.</summary>
    public IReadOnlyList<CardData> DiscardPile => _discardPile;

    // -------------------------------------------------------------------------
    // Initialisation
    // -------------------------------------------------------------------------

    /// <summary>Load the deck from a list of CardData resources and shuffle.</summary>
    public void InitialiseDeck(IEnumerable<CardData> cards)
    {
        _drawPile.Clear();
        _discardPile.Clear();
        _drawPile.AddRange(cards);
        Shuffle(_drawPile);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Draw one card from the top of the draw pile. Returns null if empty after reshuffle attempt.</summary>
    public CardData Draw()
    {
        if (_drawPile.Count == 0)
            ReshuffleDiscardIntoDrawPile();

        if (_drawPile.Count == 0)
            return null; // Truly empty — edge case

        var card = _drawPile[0];
        _drawPile.RemoveAt(0);
        return card;
    }

    /// <summary>Move a card to the discard pile.</summary>
    public void Discard(CardData card)
    {
        _discardPile.Add(card);
    }

    /// <summary>Cards remaining in the draw pile.</summary>
    public int DrawPileCount => _drawPile.Count;

    /// <summary>Cards in the discard pile.</summary>
    public int DiscardPileCount => _discardPile.Count;

    // -------------------------------------------------------------------------
    // Private Helpers
    // -------------------------------------------------------------------------

    private void ReshuffleDiscardIntoDrawPile()
    {
        GD.Print("[CardDeck] Reshuffling discard pile into draw pile.");
        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle(_drawPile);
    }

    private static void Shuffle(List<CardData> list)
    {
        var rng = new System.Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
