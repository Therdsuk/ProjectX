using Godot;

/// <summary>
/// Godot Resource holding the base stats and class definition for a character.
/// Store each class definition as a .tres file under Resources/Characters/.
///
/// Both PlayerCharacter and EnemyCharacter reference a CharacterData resource
/// to initialise their battle stats.
/// </summary>
[GlobalClass]
public partial class CharacterData : Resource
{
    [Export] public string Id          { get; set; } = "";
    [Export] public string ClassName   { get; set; } = "Unknown";

    // -------------------------------------------------------------------------
    // Base Stats
    // -------------------------------------------------------------------------

    [Export] public int BaseHp         { get; set; } = 100;
    [Export] public int BaseMana       { get; set; } = 50;
    [Export] public int BaseEnergy     { get; set; } = 30;
    [Export] public int BaseSpeed      { get; set; } = 5;
    [Export] public int BaseAttack     { get; set; } = 10;
    [Export] public int BaseDefense    { get; set; } = 5;

    /// <summary>Maximum cells the character can move per Move Phase.</summary>
    [Export] public int MoveRange      { get; set; } = 3;

    /// <summary>Maximum hand size for this class.</summary>
    [Export] public int HandSize       { get; set; } = 5;

    // Starting deck is defined as an array of CardData resource paths.
    // Populate these in the Godot editor on the .tres file.
    [Export] public Godot.Collections.Array<CardData> StartingDeck { get; set; } = new();
}
