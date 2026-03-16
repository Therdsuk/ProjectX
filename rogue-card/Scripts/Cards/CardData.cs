using Godot;

/// <summary>
/// Godot Resource representing a single card's data.
/// Store each card as a .tres file under Resources/Cards/.
///
/// Usage: Load via ResourceLoader.Load&lt;CardData&gt;("res://Resources/Cards/fireball.tres")
/// </summary>
[GlobalClass]
public partial class CardData : Resource
{
    /// <summary>Unique identifier used for lookups and serialisation.</summary>
    [Export] public string Id          { get; set; } = "";

    /// <summary>Display name shown in the UI.</summary>
    [Export] public string Name        { get; set; } = "Unnamed Card";

    /// <summary>Flavour/effect text shown on the card.</summary>
    [Export] public string Description { get; set; } = "";

    /// <summary>Which phase this card can be played in.</summary>
    [Export] public CardType CardType  { get; set; } = CardType.Battle;

    /// <summary>Mana cost to play this card.</summary>
    [Export] public int Cost           { get; set; } = 1;

    /// <summary>What kind of entity or coordinate validly receives this card.</summary>
    [Export] public TargetType Target  { get; set; } = TargetType.SingleEnemy;

    /// <summary>Tile range max-distance of the card's target selection.</summary>
    [Export] public int Range          { get; set; } = 1;

    /// <summary>If true, the range of this card is derived from the caster's Strength stat instead of a fixed value.</summary>
    [Export] public bool RangeScalesWithStrength { get; set; } = false;

    /// <summary>Shape of the Area of Effect applied when targeting is resolved.</summary>
    [Export] public AreaOfEffect AoeShape { get; set; } = AreaOfEffect.SingleNode;

    /// <summary>Base damage dealt to targets within the Area of Effect.</summary>
    [Export] public int BaseDamage     { get; set; } = 0;

    /// <summary>Base healing applied to targets within the Area of Effect.</summary>
    [Export] public int BaseHealing    { get; set; } = 0;

    /// <summary>Activation speed tier in the battle queue.</summary>
    [Export] public CardSpeed Speed    { get; set; } = CardSpeed.Fast;

    /// <summary>How many times this card has been upgraded (0 = base).</summary>
    [Export] public int UpgradeLevel   { get; set; } = 0;

    // Upgrade: override fields — future milestone
    // public CardData UpgradedVersion { get; set; }
}
