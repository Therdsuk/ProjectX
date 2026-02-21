using Godot;

/// <summary>
/// Global singleton (AutoLoad) that manages the overall game lifecycle:
/// scene transitions, global state, and references to core managers.
/// Register this as an AutoLoad singleton in Godot project settings:
///   Name: GameManager  Path: res://Scripts/Core/GameManager.cs
/// </summary>
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    // Cached reference to the current run's data (populated when a run starts)
    public RunData CurrentRun { get; private set; }

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
    }

    // -------------------------------------------------------------------------
    // Scene Navigation
    // -------------------------------------------------------------------------

    /// <summary>Change to the Battle scene.</summary>
    public void GoToBattle()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Battle/BattleScene.tscn");
    }

    /// <summary>Change to the overworld Map scene.</summary>
    public void GoToMap()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Map/MapScene.tscn");
    }

    /// <summary>Change to the Main Menu.</summary>
    public void GoToMainMenu()
    {
        GetTree().ChangeSceneToFile("res://Scenes/UI/MainMenu.tscn");
    }

    // -------------------------------------------------------------------------
    // Run Management
    // -------------------------------------------------------------------------

    /// <summary>Start a brand-new run with the given class selection.</summary>
    public void StartNewRun(string className)
    {
        CurrentRun = new RunData(className);
        GoToMap();
    }
}

/// <summary>
/// Holds the persistent state that travels through the entire run
/// (deck, gold, current map position, etc.).
/// </summary>
public class RunData
{
    public string ClassName { get; }
    public int Gold { get; set; } = 0;
    // Add deck, map progress, etc. in later milestones

    public RunData(string className)
    {
        ClassName = className;
    }
}
