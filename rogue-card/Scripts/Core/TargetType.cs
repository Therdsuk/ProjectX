/// <summary>
/// Defines who or what this card is allowed to target on the grid.
/// </summary>
public enum TargetType
{
    /// <summary>Targets the Godot node invoking the card.</summary>
    Self,
    /// <summary>Must click directly on an enemy unit.</summary>
    SingleEnemy,
    /// <summary>Can target any valid grid coordinate.</summary>
    AnyTile,
    /// <summary>Affects everything, no specific target selection needed.</summary>
    Global
}
