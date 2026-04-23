using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Deck validation rules for the deck builder.
/// </summary>
public static class DeckRules
{
    public const int MinDeckSize = 5;
    public const int MaxDeckSize = 20;
    public const int MaxCopiesPerCard = 3;

    /// <summary>
    /// Validate a deck (list of card IDs).
    /// Returns (true, null) if valid, or (false, errorMessage) if invalid.
    /// </summary>
    public static (bool valid, string error) Validate(List<string> cardIds)
    {
        if (cardIds == null || cardIds.Count < MinDeckSize)
            return (false, $"Deck needs at least {MinDeckSize} cards (has {cardIds?.Count ?? 0}).");

        if (cardIds.Count > MaxDeckSize)
            return (false, $"Deck can have at most {MaxDeckSize} cards (has {cardIds.Count}).");

        // Check max copies
        var counts = new Dictionary<string, int>();
        foreach (var id in cardIds)
        {
            if (!counts.ContainsKey(id)) counts[id] = 0;
            counts[id]++;

            if (counts[id] > MaxCopiesPerCard)
                return (false, $"Too many copies of '{id}' (max {MaxCopiesPerCard}).");
        }

        // Check all cards exist in database
        if (CardDatabase.Instance != null)
        {
            foreach (var id in cardIds)
            {
                if (!CardDatabase.Instance.HasCard(id))
                    return (false, $"Unknown card '{id}' not found in database.");
            }
        }

        return (true, null);
    }

    /// <summary>Check if a specific card can be added to the deck.</summary>
    public static (bool canAdd, string reason) CanAddCard(List<string> currentDeck, string cardId, string classId = null)
    {
        if (currentDeck.Count >= MaxDeckSize)
            return (false, "Deck is full.");

        int copies = currentDeck.Count(id => id == cardId);
        if (copies >= MaxCopiesPerCard)
            return (false, $"Already have {MaxCopiesPerCard} copies.");

        // Class restriction check
        if (classId != null && CardDatabase.Instance != null)
        {
            var card = CardDatabase.Instance.GetCard(cardId);
            if (card != null && card.AllowedClasses != null && card.AllowedClasses.Length > 0)
            {
                if (!card.AllowedClasses.Contains(classId))
                    return (false, "This card is not available for your class.");
            }
        }

        return (true, null);
    }
}
