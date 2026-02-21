namespace RogueCard.Characters
{
    /// <summary>
    /// Character class definitions
    /// TODO: Implement class-specific stats and abilities
    /// </summary>
    public class CharacterClass
    {
        public string ClassName { get; set; }
        public Core.CharacterClass ClassType { get; set; }
        
        // Base stats (can be modified by accessories)
        public int BaseHealth { get; set; }
        public int BaseMana { get; set; }
        public int BaseEnergy { get; set; }
        
        // Stat scaling per level
        public float HealthPerLevel { get; set; }
        public float ManaPerLevel { get; set; }
        
        // Class-specific bonuses
        public int AttackBonus { get; set; }
        public int DefenseBonus { get; set; }
        public int SpeedBonus { get; set; }
    }

    /// <summary>
    /// Character instance with current stats
    /// TODO: Implement character initialization, state management
    /// </summary>
    public class Character
    {
        public string CharacterId { get; set; }
        public string CharacterName { get; set; }
        public Core.CharacterClass Class { get; set; }
        
        // Current stats (can be modified by buffs/debuffs)
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        
        public int CurrentMana { get; set; }
        public int MaxMana { get; set; }
        
        public int CurrentEnergy { get; set; }
        public int MaxEnergy { get; set; }
        
        // Position on board
        public Godot.Vector2I GridPosition { get; set; }
        
        // TODO: Implement status effects
        // TODO: Implement accessory system
        // TODO: Implement character progression
    }

    /// <summary>
    /// Character stats calculator
    /// TODO: Calculate final stats including buffs, debuffs, accessories
    /// </summary>
    public class CharacterStats
    {
        // TODO: Calculate total health with level and accessories
        // TODO: Calculate total mana with level and accessories
        // TODO: Calculate total energy with level and accessories
        // TODO: Apply buff/debuff modifiers
    }
}
