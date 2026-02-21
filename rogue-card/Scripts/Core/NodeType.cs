/// <summary>
/// All possible node types on the overworld node map.
/// </summary>
public enum NodeType
{
    Battle   = 0,   // Triggers a card combat encounter
    City     = 1,   // Hub with random sub-nodes (Shop, Quest, etc.)
    Shop     = 2,   // Buy / sell cards and items
    Exchange = 3,   // Trade cards or currency for other rewards
    Quest    = 4,   // Accept / complete a quest for rewards
    Heal     = 5,   // Restore HP (and possibly Mana/Energy)
    Revive   = 6,   // Resurrect a fallen character or restore a major resource
    Boss     = 7    // Elite / boss battle — always at act end
}
