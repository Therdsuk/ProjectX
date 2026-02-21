namespace RogueCard.Map
{
    /// <summary>
    /// Adventure map node types
    /// TODO: Implement node spawning and transitions
    /// </summary>
    public enum NodeType
    {
        Battle,
        City,
        Event,
        Boss,
        Treasure
    }

    /// <summary>
    /// Individual map node
    /// TODO: Implement node data and transition logic
    /// </summary>
    public class MapNode
    {
        public string NodeId { get; set; }
        public NodeType Type { get; set; }
        public int Difficulty { get; set; }
        
        // TODO: Node position on map
        // TODO: Node rewards
        // TODO: Node connections
    }

    /// <summary>
    /// Adventure map manager
    /// TODO: Implement map generation and navigation
    /// </summary>
    public class MapManager
    {
        private System.Collections.Generic.List<MapNode> _nodes;
        
        // TODO: Generate random map with nodes
        // TODO: Calculate shortest path to destination
        // TODO: Track visited nodes
        // TODO: Handle node transitions
    }
}
