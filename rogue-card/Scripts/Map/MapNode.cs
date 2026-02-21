using System.Collections.Generic;

/// <summary>
/// Data representing one node on the overworld map.
/// </summary>
public class MapNode
{
    public int      Id          { get; set; }
    public NodeType Type        { get; set; }
    public int      Layer       { get; set; }  // depth from start

    /// <summary>IDs of nodes this node connects to (forward edges).</summary>
    public List<int> Connections { get; set; } = new();

    public bool Visited { get; set; } = false;
}
