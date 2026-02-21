using System.Collections.Generic;

/// <summary>
/// Manages the cards currently in a character's hand.
/// </summary>
public class CardHand
{
    private readonly List<CardData> _cards = new();

    /// <summary>Maximum number of cards allowed in hand.</summary>
    public int MaxHandSize { get; set; } = 5;

    /// <summary>Read-only view of the current hand.</summary>
    public IReadOnlyList<CardData> Cards => _cards;

    /// <summary>Number of cards in hand.</summary>
    public int Count => _cards.Count;

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Add a card to the hand if there is space.</summary>
    public bool AddCard(CardData card)
    {
        if (_cards.Count >= MaxHandSize)
            return false;
        _cards.Add(card);
        return true;
    }

    /// <summary>Remove and return the card at the given index.</summary>
    public CardData PlayCard(int index)
    {
        if (index < 0 || index >= _cards.Count)
            return null;
        var card = _cards[index];
        _cards.RemoveAt(index);
        return card;
    }

    /// <summary>Remove a specific card instance.</summary>
    public bool PlayCard(CardData card)
    {
        return _cards.Remove(card);
    }

    /// <summary>Clear all cards (e.g. on battle end).</summary>
    public void Clear() => _cards.Clear();

    /// <summary>How many more cards can fit in the hand.</summary>
    public int FreeSlotsCount => MaxHandSize - _cards.Count;
}
