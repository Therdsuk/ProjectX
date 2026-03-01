using Godot;

/// <summary>
/// A helper configuration class for the DebugBattleSpawner to define
/// multiple characters and their initial positions in the Godot Inspector.
/// </summary>
[GlobalClass]
public partial class DebugUnitSetup : Resource
{
    [Export] public bool IsPlayer { get; set; } = true;
    [Export] public int BaseHp { get; set; } = 100;
    [Export] public int BaseMana { get; set; } = 50;
    [Export] public Vector2I StartPos { get; set; } = Vector2I.Zero;

    /// <summary>
    /// If assigned, the Spawner will use this CharacterData resource directly 
    /// instead of building a purely generic test mock with BaseHp/BaseMana.
    /// </summary>
    [Export] public CharacterData OptionalDataOverride { get; set; }
}
