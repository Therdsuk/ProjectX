using Godot;
using System.Collections.Generic;

/// <summary>
/// Tracks the player's current position on the map and handles node selection.
/// M5 milestone — stub.
/// </summary>
public partial class MapManager : Node
{
    public static MapManager Instance { get; private set; }

    private List<MapNode> _map      = new();
    private int           _currentNodeId = 0;

    public override void _Ready()
    {
        if (Instance != null) { QueueFree(); return; }
        Instance = this;
    }

    public void LoadMap(List<MapNode> map) => _map = map;

    public MapNode CurrentNode => _map.Find(n => n.Id == _currentNodeId);

    public void TravelTo(int nodeId)
    {
        _currentNodeId = nodeId;
        GD.Print($"[MapManager] Travelled to node {nodeId} ({CurrentNode?.Type})");
        // M5: trigger the appropriate scene based on node type
    }
}
