/// <summary>
/// The type of a battlefield cell, affecting damage, movement, and status effects.
/// </summary>
public enum FieldType
{
    /// <summary>No special modifier.</summary>
    Normal = 0,

    /// <summary>Reduces Fire damage; Thunder damage splashes to adjacent cells.</summary>
    Water  = 1,

    /// <summary>Deals passive damage each round to units standing on it.</summary>
    Lava   = 2,

    /// <summary>Increases Defence; reduces Movement range.</summary>
    Forest = 3,

    /// <summary>Chance to Freeze on contact; slippery movement.</summary>
    Ice    = 4,

    /// <summary>Reduces Speed; no special damage interaction.</summary>
    Sand   = 5
}
