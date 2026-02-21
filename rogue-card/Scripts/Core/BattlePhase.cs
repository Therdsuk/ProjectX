/// <summary>
/// Enumerates all game phases within a single battle round.
///
/// Round flow:
///   MovePhase   → characters move; Move-type cards can be played
///   BattlePhase → Battle / Buff / Debuff cards played; activation queue resolves
///   SetupPhase  → Setup-type cards played; hand refilled; status effects tick
/// </summary>
public enum BattlePhase
{
    MovePhase   = 0,
    BattlePhase = 1,
    SetupPhase  = 2
}
