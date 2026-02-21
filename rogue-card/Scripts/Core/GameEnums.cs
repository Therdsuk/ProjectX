namespace RogueCard.Core
{
    /// <summary>
    /// Defines the speed tier for card execution in battle
    /// </summary>
    public enum CardSpeed
    {
        Burst = 0,  // Executes first
        Fast = 1,   // Executes second
        Slow = 2    // Executes last
    }

    /// <summary>
    /// Defines when a card can be used in battle
    /// </summary>
    public enum CardType
    {
        Move,    // Used in Move Phase
        Attack,  // Used in Battle Phase
        Buff,    // Used in Battle Phase
        Setup    // Used in Setup Phase
    }

    /// <summary>
    /// Environmental field types on the board
    /// </summary>
    public enum FieldType
    {
        Normal,
        Water,
        Lava,
        Ice,
        Forest
    }

    /// <summary>
    /// Battle phases in round sequence
    /// </summary>
    public enum BattlePhase
    {
        Move = 0,
        Battle = 1,
        Setup = 2
    }

    /// <summary>
    /// Character classes
    /// </summary>
    public enum CharacterClass
    {
        Warrior,
        Mage,
        Rogue
    }
}
