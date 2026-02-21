using Godot;
using System.Collections.Generic;

/// <summary>
/// Procedurally generates the overworld node map as a directed graph.
///
/// M5 milestone — stub only. The full generation algorithm is not yet implemented.
/// </summary>
public partial class MapGenerator : Node
{
    [Export] public int Layers     { get; set; } = 8;   // depth of map
    [Export] public int MaxPerLayer { get; set; } = 3;  // max nodes per layer

    /// <summary>Generate and return a list of MapNodes. (M5: full implementation)</summary>
    public List<MapNode> GenerateMap()
    {
        var nodes = new List<MapNode>();
        GD.Print("[MapGenerator] GenerateMap — not yet implemented (M5 milestone).");
        return nodes;
    }
}
