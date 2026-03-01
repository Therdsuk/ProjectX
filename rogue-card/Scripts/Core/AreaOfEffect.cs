/// <summary>
/// Defines the shape of the Area of Effect (AoE) surrounding a targeted tile.
/// </summary>
public enum AreaOfEffect
{
    /// <summary>Affects only the initially targeted node/tile.</summary>
    SingleNode,
    /// <summary>Affects the targeted tile and the 4 adjacent tiles.</summary>
    Cross,
    /// <summary>Affects the targeted tile and all 8 surrounding tiles.</summary>
    Square3x3,
    /// <summary>Projects a line area originating from the caster towards the target.</summary>
    LineForward
}
