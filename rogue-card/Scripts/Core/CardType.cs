/// <summary>
/// The type of a card, determining which phase it can be played in
/// and what kind of effect it applies.
/// </summary>
public enum CardType
{
    /// <summary>Move Phase — move characters or alter positioning.</summary>
    Move    = 0,

    /// <summary>Battle Phase — direct attacks or skills.</summary>
    Battle  = 1,

    /// <summary>Battle Phase — positive status effects for allies.</summary>
    Buff    = 2,

    /// <summary>Battle Phase — negative status effects on enemies.</summary>
    Debuff  = 3,

    /// <summary>Setup Phase — traps, field-change effects, persistent auras.</summary>
    Setup   = 4
}
