using Godot;

/// <summary>
/// Handles saving and loading the current run state.
/// Milestone 7 — placeholder stub.
/// </summary>
public partial class SaveSystem : Node
{
    private const string SavePath = "user://save_data.json";

    public static SaveSystem Instance { get; private set; }

    public override void _Ready()
    {
        if (Instance != null) { QueueFree(); return; }
        Instance = this;
    }

    /// <summary>Save the current run to disk.</summary>
    public void SaveRun()
    {
        // TODO (M7): serialize GameManager.Instance.CurrentRun to JSON
        GD.Print("[SaveSystem] SaveRun — not yet implemented.");
    }

    /// <summary>Load run data from disk.</summary>
    public void LoadRun()
    {
        // TODO (M7): deserialize from JSON and restore RunData
        GD.Print("[SaveSystem] LoadRun — not yet implemented.");
    }
}
