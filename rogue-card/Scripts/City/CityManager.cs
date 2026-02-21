namespace RogueCard.City
{
    /// <summary>
    /// City hub service types
    /// TODO: Implement randomized service selection
    /// </summary>
    public enum ServiceType
    {
        Shop,
        Exchange,
        Quest,
        Heal,
        Revive
    }

    /// <summary>
    /// City hub manager
    /// TODO: Implement randomized services and interactions
    /// </summary>
    public class CityManager
    {
        private System.Collections.Generic.List<ServiceType> _availableServices;
        
        // TODO: Randomly select services (not all services always available)
        // TODO: Implement shop system
        // TODO: Implement card exchange/crafting
        // TODO: Implement quest system
        // TODO: Implement healing service
        // TODO: Implement revive service
    }

    /// <summary>
    /// Shop interface
    /// TODO: Implement card purchasing and currency management
    /// </summary>
    public class Shop
    {
        private System.Collections.Generic.List<Cards.CardData> _availableCards;
        
        // TODO: Generate random card stock
        // TODO: Calculate card prices
        // TODO: Handle purchase transactions
    }
}
