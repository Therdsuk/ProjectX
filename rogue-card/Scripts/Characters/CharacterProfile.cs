using System;
using System.Collections.Generic;

/// <summary>
/// Represents a saved player character (name + class + custom deck).
/// Serialised to JSON via CharacterSaveSystem.
/// </summary>
[Serializable]
public class CharacterProfile
{
    public string Id      { get; set; } = Guid.NewGuid().ToString();
    public string Name    { get; set; } = "Hero";
    public string ClassId { get; set; } = ClassRegistry.Warrior;

    /// <summary>Card IDs in the player's custom deck. Empty = use class default deck.</summary>
    public List<string> DeckCardIds { get; set; } = new();

    /// <summary>Whether this profile has a custom deck configured.</summary>
    public bool HasCustomDeck => DeckCardIds != null && DeckCardIds.Count > 0;
}
