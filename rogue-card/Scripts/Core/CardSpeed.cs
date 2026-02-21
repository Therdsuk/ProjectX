/// <summary>
/// The activation speed of a card within the Battle Phase.
/// Cards resolve in order: Burst first, then Fast, then Slow.
/// Within the same tier, the character's Speed stat determines priority.
/// </summary>
public enum CardSpeed
{
    /// <summary>Activates first — buffs, debuffs, instant reactions.</summary>
    Burst = 0,

    /// <summary>Activates second — normal single-target attacks, standard skills.</summary>
    Fast  = 1,

    /// <summary>Activates last — heavy AoE, powerful but costly effects.</summary>
    Slow  = 2
}
