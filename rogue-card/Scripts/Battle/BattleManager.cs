using Godot;
using System.Collections.Generic;

/// <summary>
/// Central controller for the battle scene.
///
/// M1 responsibilities:
///   - Receive the board and HUD references
///   - Hold a list of player and enemy units
///   - Manage the round/phase state machine
///   - Expose "Next Phase" advance (called by the HUD button or keyboard shortcut)
///
/// State machine:
///   RoundStart → MovePhase → BattlePhase → SetupPhase → RoundStart (loop)
///                                                     ↓
///                                              BattleEnd (if win/lose condition met)
/// </summary>
public partial class BattleManager : Node
{
    // -------------------------------------------------------------------------
    // Scene Node References (wire these up in BattleScene.tscn)
    // -------------------------------------------------------------------------

    [Export] public BattleBoard Board      { get; set; }
    [Export] public BattleHUD   HUD        { get; set; }
    [Export] public Camera3D    MainCamera { get; set; }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private BattlePhase _currentPhase = BattlePhase.MovePhase;
    private int         _roundNumber  = 1;

    private readonly List<PlayerCharacter> _players = new();
    private readonly List<EnemyCharacter>  _enemies = new();

    private bool _battleActive = false;

    // Test Config (M1)
    private readonly int _testMoveRange = 3;
    private readonly List<Vector2I> _validMoves = new();
    
    // M1 Turn Tracking
    private bool _playerHasMoved = false;
    private bool _isMovePending  = false;
    private Vector2I _moveOrigin;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        // Fix for 3D Conversion: Since we replaced the old 2D Board node with a 3D Board,
        // the exported reference in the inspector might have broken. Re-acquire it dynamically.
        if (Board == null)   Board      = GetNodeOrNull<BattleBoard>("../BattleBoard");
        if (MainCamera == null) MainCamera = GetNodeOrNull<Camera3D>("../Camera3D");

        // Connect HUD next-phase button
        if (HUD != null)
            HUD.NextPhaseRequested += OnNextPhaseRequested;

        // M1: auto-start battle when scene loads
        StartBattle();

        // Spawn test characters after the battle (and board generation) has started
        // Using CallDeferred ensures all nodes are fully initialized in the scene tree
        CallDeferred(MethodName.SpawnTestUnits);
    }

    // -------------------------------------------------------------------------
    // Battle Flow
    // -------------------------------------------------------------------------

    /// <summary>Called once when the scene is ready to begin battle.</summary>
    public void StartBattle()
    {
        GD.Print($"[BattleManager] Battle started — Round {_roundNumber}");
        _battleActive = true;
        _currentPhase = BattlePhase.MovePhase;
        EnterPhase(_currentPhase);
    }

    /// <summary>Advance to the next phase in the round sequence.</summary>
    public void AdvancePhase()
    {
        if (!_battleActive) return;

        // Check win/lose before advancing
        if (CheckBattleEnd()) return;

        _currentPhase = _currentPhase switch
        {
            BattlePhase.MovePhase   => BattlePhase.BattlePhase,
            BattlePhase.BattlePhase => BattlePhase.SetupPhase,
            BattlePhase.SetupPhase  => NextRound(),
            _                       => BattlePhase.MovePhase
        };

        EnterPhase(_currentPhase);
    }

    // -------------------------------------------------------------------------
    // Phase Entry Logic
    // -------------------------------------------------------------------------

    private void EnterPhase(BattlePhase phase)
    {
        GD.Print($"[BattleManager] Round {_roundNumber} — Entering phase: {phase}");

        // Notify the event bus (HUD listens to update its label)
        EventBus.Instance?.EmitSignal(EventBus.SignalName.PhaseChanged, (int)phase);

        switch (phase)
        {
            case BattlePhase.MovePhase:
                OnEnterMovePhase();
                break;
            case BattlePhase.BattlePhase:
                OnEnterBattlePhase();
                break;
            case BattlePhase.SetupPhase:
                OnEnterSetupPhase();
                break;
        }
    }

    private void OnEnterMovePhase()
    {
        GD.Print("[BattleManager] Move Phase: players may move and play Move cards.");
        
        // M1 Test: Calculate and highlight valid moves for the first player
        _playerHasMoved = false;
        if (_players.Count > 0)
        {
            ShowValidMoves(_players[0]);
        }
    }

    private void OnEnterBattlePhase()
    {
        // M2+: populate ActivationQueue, resolve cards
        GD.Print("[BattleManager] Battle Phase: players play attack/buff/debuff cards.");
    }

    private void OnEnterSetupPhase()
    {
        // M2+: resolve Setup cards; refill player hands
        GD.Print("[BattleManager] Setup Phase: players play setup cards; hand refilled.");
        Board?.ClearHighlights(); // Clear any leftover highlights
        RefillAllHands();
    }

    // -------------------------------------------------------------------------
    // Round Transition
    // -------------------------------------------------------------------------

    private BattlePhase NextRound()
    {
        _roundNumber++;
        GD.Print($"[BattleManager] === Round {_roundNumber} Start ===");
        return BattlePhase.MovePhase;
    }

    // -------------------------------------------------------------------------
    // Win / Lose Check
    // -------------------------------------------------------------------------

    /// <summary>Returns true if the battle has ended.</summary>
    private bool CheckBattleEnd()
    {
        bool allEnemiesDead  = _enemies.TrueForAll(e => !e.IsAlive);
        bool allPlayersDead  = _players.TrueForAll(p => !p.IsAlive);

        if (_enemies.Count > 0 && allEnemiesDead)
        {
            EndBattle(true);
            return true;
        }
        if (_players.Count > 0 && allPlayersDead)
        {
            EndBattle(false);
            return true;
        }
        return false;
    }

    private void EndBattle(bool playerWon)
    {
        _battleActive = false;
        GD.Print($"[BattleManager] Battle ended. Player won: {playerWon}");
        EventBus.Instance?.EmitSignal(EventBus.SignalName.BattleEnded, playerWon);
        // M4+: show reward screen or game-over screen via GameManager
    }

    // -------------------------------------------------------------------------
    // Unit Management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Spawn a player character onto the board.
    /// Call this from BattleScene._Ready() or a test setup method.
    /// </summary>
    public void AddPlayer(PlayerCharacter player, Vector2I startCell)
    {
        _players.Add(player);
        Board?.PlaceUnit(player, startCell);
        player.GridPosition = startCell;
    }

    /// <summary>Spawn an enemy onto the board.</summary>
    public void AddEnemy(EnemyCharacter enemy, Vector2I startCell)
    {
        _enemies.Add(enemy);
        Board?.PlaceUnit(enemy, startCell);
        enemy.GridPosition = startCell;
    }

    private void SpawnTestUnits()
    {
        // --- 1. Spawn a Test Player ---
        var player = new PlayerCharacter { Name = "TestPlayer" };
        
        // Add a visual 3D box so we can see the player
        var playerMesh = new MeshInstance3D();
        playerMesh.Mesh = new BoxMesh();
        // Lift the box slightly so it sits on top of the floor tile
        playerMesh.Position = new Vector3(0, 0.5f, 0); 
        player.AddChild(playerMesh);
        
        // Must add to the SceneTree securely
        Board.AddChild(player);
        
        // Register to BattleManager & Board at Grid Pos (Col 1, Row 2)
        AddPlayer(player, new Vector2I(1, 2));

        // --- 2. Spawn a Test Enemy ---
        var enemy = new EnemyCharacter { Name = "TestEnemy" };
        
        // Add a red 3D box so we can visually tell it's an enemy
        var enemyMesh = new MeshInstance3D();
        var material = new StandardMaterial3D();
        material.AlbedoColor = new Color(1.0f, 0.0f, 0.0f); // Red
        enemyMesh.Mesh = new CapsuleMesh { Material = material }; // Make enemy a capsule
        enemyMesh.Position = new Vector3(0, 1.0f, 0);
        enemy.AddChild(enemyMesh);
        
        Board.AddChild(enemy);
        
        // Register at Grid Pos (Col 6, Row 2)
        AddEnemy(enemy, new Vector2I(6, 2));

        // Fix: SpawnTestUnits is called deferred, so OnEnterMovePhase() fired before the player existed.
        // We must manually trigger the move calculation here for the spawned unit!
        if (_currentPhase == BattlePhase.MovePhase && !_playerHasMoved && _players.Count > 0)
        {
            ShowValidMoves(_players[0]);
        }
    }

    // -------------------------------------------------------------------------
    // M1 Mechanics (Test Movement & Highlighting)
    // -------------------------------------------------------------------------

    private void ShowValidMoves(PlayerCharacter player)
    {
        Board?.ClearHighlights();
        _validMoves.Clear();

        // If we cancel a move, we revert to this origin
        _moveOrigin = player.GridPosition;

        for (int q = -_testMoveRange; q <= _testMoveRange; q++)
        {
            for (int r = -_testMoveRange; r <= _testMoveRange; r++)
            {
                // Simple Manhattan distance
                if (Mathf.Abs(q) + Mathf.Abs(r) <= _testMoveRange)
                {
                    Vector2I checkPos = _moveOrigin + new Vector2I(q, r);

                    if (Board.IsInBounds(checkPos) && (!Board.IsOccupied(checkPos) || checkPos == _moveOrigin))
                    {
                        _validMoves.Add(checkPos);
                        // Highlight valid cell green
                        Board.HighlightCell(checkPos, new Color(0.1f, 0.7f, 0.2f)); 
                    }
                }
            }
        }
        
        // Highlight player's underlying cell distinctively (e.g., cyan/blue)
        Board.HighlightCell(_moveOrigin, new Color(0.2f, 0.4f, 0.8f));
    }

    public override void _Input(InputEvent @event)
    {
        // Spacebar to skip phases
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Space)
        {
            if (_isMovePending) CancelPendingMove();
            Board?.ClearHighlights(); 
            AdvancePhase();
            return;
        }

        // Mouse clicks on the grid
        if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed)
        {
            if (_currentPhase == BattlePhase.MovePhase && !_playerHasMoved && _players.Count > 0)
            {
                if (MainCamera == null) return;

                // Right Click to Cancel
                if (mouseBtn.ButtonIndex == MouseButton.Right && _isMovePending)
                {
                    CancelPendingMove();
                    return;
                }

                // Left Click to Select / Confirm
                if (mouseBtn.ButtonIndex == MouseButton.Left)
                {
                    var spaceState = GetViewport().World3D.DirectSpaceState;
                    var rayOrigin = MainCamera.ProjectRayOrigin(mouseBtn.Position);
                    var rayEnd = rayOrigin + MainCamera.ProjectRayNormal(mouseBtn.Position) * 1000f;

                    var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
                    var result = spaceState.IntersectRay(query);

                    if (result.Count > 0)
                    {
                        Vector3 hitPosition = (Vector3)result["position"];
                        Vector2I clickedCell = Board.WorldToGrid(hitPosition);

                        TrySelectOrConfirmMove(clickedCell);
                    }
                }
            }
        }
    }

    private void TrySelectOrConfirmMove(Vector2I targetPos)
    {
        var player = _players[0];

        // 1. Confirming a move (Clicking the cell we are currently standing on while testing)
        if (_isMovePending && player.GridPosition == targetPos)
        {
            _playerHasMoved = true;
            _isMovePending = false;
            Board.ClearHighlights();
            GD.Print($"[BattleManager] Player confirmed move at {targetPos}. Turn finished.");
            return;
        }

        // 2. Testing a move (Clicking a valid green square)
        if (_validMoves.Contains(targetPos))
        {
            Board.MoveUnit(player, player.GridPosition, targetPos);
            player.GridPosition = targetPos;
            
            _isMovePending = true;
            
            // Re-draw valid moves based on ORIGIN, but highlight current location in Blue
            // We temporarily manipulate validMoves logic below for visual feedback:
            Board.ClearHighlights();
            foreach (var validCell in _validMoves)
            {
                Board.HighlightCell(validCell, new Color(0.1f, 0.7f, 0.2f)); 
            }
            Board.HighlightCell(targetPos, new Color(0.2f, 0.4f, 0.8f)); // Highlight new selected location

            GD.Print($"[BattleManager] Testing move to {targetPos}. Left-click again to confirm, Right-click to cancel.");
        }
        else
        {
            GD.Print($"[BattleManager] Move to {targetPos} is not valid.");
        }
    }

    private void CancelPendingMove()
    {
        if (!_isMovePending) return;

        var player = _players[0];
        Board.MoveUnit(player, player.GridPosition, _moveOrigin);
        player.GridPosition = _moveOrigin;
        _isMovePending = false;

        ShowValidMoves(player); // Recalculate and re-highlight green from origin
        GD.Print($"[BattleManager] Move cancelled. Returned to {_moveOrigin}");
    }
    
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void RefillAllHands()
    {
        foreach (var player in _players)
            player.DrawToHandLimit();
    }

    // -------------------------------------------------------------------------
    // Signal Handlers
    // -------------------------------------------------------------------------

    private void OnNextPhaseRequested()
    {
        AdvancePhase();
    }
}
