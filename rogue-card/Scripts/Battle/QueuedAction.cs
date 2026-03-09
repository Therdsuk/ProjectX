using Godot;

/// <summary>
/// Represents a card that has been played and targeted, waiting in the Activation Queue
/// to be resolved at the end of the Battle Phase.
/// </summary>
public struct QueuedAction
{
    public CharacterData Caster;
    public CardData Card;
    public Vector2I TargetCell;
    public Vector2I CasterOrigin;

    public QueuedAction(CharacterData caster, CardData card, Vector2I targetCell, Vector2I casterOrigin)
    {
        Caster = caster;
        Card = card;
        TargetCell = targetCell;
        CasterOrigin = casterOrigin;
    }
}
