namespace RogueCard.Cards
{
    /// <summary>
    /// Base card data structure
    /// TODO: Complete implementation with card effects and upgrades
    /// </summary>
    public class CardData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        
        // Cost and resource requirements
        public int Energy { get; set; }
        public int Mana { get; set; }
        
        // Card properties
        public Core.CardSpeed Speed { get; set; }
        public Core.CardType Type { get; set; }
        
        // Range and effect
        public int Range { get; set; }
        public string EffectId { get; set; }
        
        // Combat properties
        public int BaseDamage { get; set; }
        public int UpgradeLevel { get; set; }
    }

    /// <summary>
    /// Manages player decks
    /// TODO: Implement deck validation and card management
    /// </summary>
    public class Deck
    {
        public string DeckId { get; set; }
        public string OwnerCharacterId { get; set; }
        
        private System.Collections.Generic.List<CardData> Cards { get; set; }
        
        public const int MAX_CARDS = 30;
        public const int MAX_COPIES = 3;

        public Deck()
        {
            Cards = new System.Collections.Generic.List<CardData>();
        }

        // TODO: Add card to deck with validation
        // TODO: Remove card from deck
        // TODO: Upgrade card in deck
        // TODO: Save/load deck from file
    }

    /// <summary>
    /// Card effect system
    /// TODO: Implement effect resolution and callbacks
    /// </summary>
    public interface ICardEffect
    {
        void Resolve(Core.BattlePhase phase);
        bool IsValidTarget(string targetId);
    }
}
