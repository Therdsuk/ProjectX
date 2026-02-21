using Godot;
using RogueCard.Battle;
using RogueCard.Battle.UI;

/// <summary>
/// Initializes and sets up the battle scene
/// This script can be attached to the root node of the battle scene
/// </summary>
public partial class BattleSceneSetup : Node3D
{
    private BattleManager _battleManager;
    private Camera3D _camera;

    public override void _Ready()
    {
        GD.Print("BattleSceneSetup: Initializing battle scene...");

        // Create camera
        SetupCamera();

        // Create battle manager
        SetupBattleManager();

        // Create UI
        SetupUI();

        // Setup initial player positions for testing
        SetupTestPlayers();

        GD.Print("BattleSceneSetup: Battle scene ready");
    }

    /// <summary>
    /// Set up the main camera
    /// </summary>
    private void SetupCamera()
    {
        _camera = new Camera3D();
        _camera.Name = "MainCamera";
        
        // Position camera for isometric view
        _camera.Position = new Vector3(4, 20, 10);
        _camera.LookAt(new Vector3(4, 0, 4), Vector3.Up);
        
        AddChild(_camera);
        _camera.Current = true;

        GD.Print("BattleSceneSetup: Camera set up for isometric view");
    }

    /// <summary>
    /// Set up the battle manager and board
    /// </summary>
    private void SetupBattleManager()
    {
        // Create main 3D scene node for the board
        var boardVisuals = new Node3D();
        boardVisuals.Name = "BoardVisuals";
        AddChild(boardVisuals);

        // Create battle manager
        _battleManager = new BattleManager();
        _battleManager.Name = "BattleManager";
        AddChild(_battleManager);

        // Create board manager under board visuals
        var boardManager = new BoardManager();
        boardManager.Name = "BoardManager";
        boardManager.TileSize = 1.0f;
        boardVisuals.AddChild(boardManager);

        GD.Print("BattleSceneSetup: Battle manager and board created");
    }

    /// <summary>
    /// Set up the battle UI
    /// </summary>
    private void SetupUI()
    {
        var uiLayer = new CanvasLayer();
        uiLayer.Name = "UILayer";
        AddChild(uiLayer);

        var uiManager = new BattleUIManager();
        uiManager.Name = "BattleUIManager";
        uiLayer.AddChild(uiManager);

        GD.Print("BattleSceneSetup: UI created");
    }

    /// <summary>
    /// Place test player characters on the board for demonstration
    /// </summary>
    private void SetupTestPlayers()
    {
        var boardManager = GetNode<BoardManager>("BoardVisuals/BoardManager");
        if (boardManager != null)
        {
            // Place player 1 at position (1, 1)
            boardManager.PlaceCharacter(new Vector2I(1, 1), "Player1");

            // Place player 2 at position (6, 6)
            boardManager.PlaceCharacter(new Vector2I(6, 6), "Player2");

            // Place enemy at position (3, 3)
            boardManager.PlaceCharacter(new Vector2I(3, 3), "Enemy1");

            GD.Print("BattleSceneSetup: Test characters placed on board");
        }
    }

    public override void _Process(double delta)
    {
        // Handle escape to return to menu (for testing)
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            GetTree().Quit();
        }
    }
}
