/// <summary>
/// Base class for all card effect resolvers.
/// For each unique card effect, create a subclass and override Execute().
///
/// M2+ milestone: effect classes will be instantiated by BattleManager
/// when a card is popped off the ActivationQueue.
/// </summary>
public abstract class CardEffect
{
    /// <summary>Execute the card effect in the context of a battle.</summary>
    /// <param name="caster">The character who played the card.</param>
    /// <param name="target">The primary target of the effect (may be null for AoE).</param>
    /// <param name="card">The card data being resolved.</param>
    public abstract void Execute(CharacterData caster, CharacterData target, CardData card);
}
