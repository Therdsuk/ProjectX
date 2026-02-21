using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Randomly selects which sub-node services are available inside a City node.
/// In a City, a random subset of: Shop, Exchange, Quest, Heal, Revive are present.
/// M5+ milestone — stub.
/// </summary>
public partial class CityGenerator : Node
{
    private static readonly NodeType[] _possibleSubNodes =
    {
        NodeType.Shop,
        NodeType.Exchange,
        NodeType.Quest,
        NodeType.Heal,
        NodeType.Revive
    };

    [Export] public int MinSubNodes { get; set; } = 1;
    [Export] public int MaxSubNodes { get; set; } = 3;

    /// <summary>Generate a random set of sub-nodes for this city visit.</summary>
    public List<NodeType> GenerateCitySubNodes()
    {
        var rng    = new Random();
        var pool   = new List<NodeType>(_possibleSubNodes);
        var result = new List<NodeType>();
        int count  = rng.Next(MinSubNodes, MaxSubNodes + 1);

        while (result.Count < count && pool.Count > 0)
        {
            int idx = rng.Next(pool.Count);
            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        GD.Print($"[CityGenerator] City has sub-nodes: {string.Join(", ", result)}");
        return result;
    }
}
