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

    // Targeting State
    private int _hoveredCardIndex = -1;
    private int _selectedCardIndex = -1;
    private Vector2I _lastHoveredCell = new Vector2I(-999, -999);

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
        {
            HUD.NextPhaseRequested += OnNextPhaseRequested;
            HUD.CardPlayedRequested += OnCardPlayedRequested;
            HUD.CardHovered += OnCardHovered;
            HUD.CardUnhovered += OnCardUnhovered;
        }

        // M1: auto-start battle when scene loads
        StartBattle();
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

        _selectedCardIndex = -1;
        _hoveredCardIndex = -1;
        _lastHoveredCell = new Vector2I(-999, -999);
        Board?.ClearHighlights();

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

    /// <summary>Called by the DebugBattleSpawner to initialize turn mechanics after setting up the board.</summary>
    public void DebugInitializePlayerTurn(PlayerCharacter player)
    {
        // Fix: Force calculation since the player did not exist when OnEnterMovePhase fired internally.
        if (_currentPhase == BattlePhase.MovePhase && !_playerHasMoved)
        {
            ShowValidMoves(player);
        }

        // Draw the initial cards and update the visual hand HUD immediately
        player.DrawToHandLimit();
        HUD?.UpdateHand(player.Hand.Cards);
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
            _selectedCardIndex = -1;
            Board?.ClearHighlights(); 
            AdvancePhase();
            return;
        }

        // Handle Target Preview via Mouse Motion
        if (@event is InputEventMouseMotion mouseMotion)
        {
            if (_currentPhase == BattlePhase.BattlePhase && MainCamera != null)
            {
                if (_hoveredCardIndex != -1 || _selectedCardIndex != -1)
                {
                    var spaceState = GetViewport().World3D.DirectSpaceState;
                    var rayOrigin = MainCamera.ProjectRayOrigin(mouseMotion.Position);
                    var rayEnd = rayOrigin + MainCamera.ProjectRayNormal(mouseMotion.Position) * 1000f;

                    var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
                    var result = spaceState.IntersectRay(query);

                    if (result.Count > 0)
                    {
                        Vector3 hitPosition = (Vector3)result["position"];
                        Vector2I hoveredCell = Board.WorldToGrid(hitPosition);

                        if (hoveredCell != _lastHoveredCell)
                        {
                            _lastHoveredCell = hoveredCell;
                            int activeCardIdx = _selectedCardIndex != -1 ? _selectedCardIndex : _hoveredCardIndex;
                            UpdateTargetingPreview(activeCardIdx, hoveredCell);
                        }
                    }
                    else if (_lastHoveredCell != new Vector2I(-999, -999))
                    {
                        _lastHoveredCell = new Vector2I(-999, -999);
                        if (_selectedCardIndex == -1) Board.ClearHighlights(); 
                    }
                }
            }
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

            if (_currentPhase == BattlePhase.BattlePhase && MainCamera != null)
            {
                if (mouseBtn.ButtonIndex == MouseButton.Left && _selectedCardIndex != -1 && _players.Count > 0)
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

                        var player = _players[0];
                        var card = player.Hand.Cards[_selectedCardIndex];
                        var validTargets = GetValidTargetCells(player, card);

                        if (validTargets.Contains(clickedCell))
                        {
                            ExecuteCardPlay(player, _selectedCardIndex, clickedCell);
                        }
                        else
                        {
                            GD.Print("[BattleManager] Invalid target for card.");
                        }
                    }
                }
                
                if (mouseBtn.ButtonIndex == MouseButton.Right && _selectedCardIndex != -1)
                {
                    _selectedCardIndex = -1;
                    Board.ClearHighlights();
                    GD.Print("[BattleManager] Cancelled card targeting via Right-Click.");
                    // Reset to hover preview if we are still hovering
                    if (_hoveredCardIndex != -1) UpdateTargetingPreview(_hoveredCardIndex, _lastHoveredCell);
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
        {
            player.DrawToHandLimit();
        }
        
        // M1: Since we only have one test player visibly bound to the HUD right now
        if (_players.Count > 0)
        {
            HUD?.UpdateHand(_players[0].Hand.Cards);
        }
    }

    // -------------------------------------------------------------------------
    // Signal Handlers
    // -------------------------------------------------------------------------

    private void OnNextPhaseRequested()
    {
        AdvancePhase();
    }

    private void OnCardPlayedRequested(int index)
    {
        if (_currentPhase != BattlePhase.BattlePhase || _players.Count == 0) return;

        var player = _players[0];
        if (index < 0 || index >= player.Hand.Cards.Count) return;
        var card = player.Hand.Cards[index];

        if (card.Target == TargetType.Self || card.Target == TargetType.Global)
        {
            ExecuteCardPlay(player, index, player.GridPosition);
        }
        else
        {
            if (_selectedCardIndex == index)
            {
                _selectedCardIndex = -1;
                Board.ClearHighlights();
                GD.Print("[BattleManager] Cancelled card selection.");
            }
            else
            {
                _selectedCardIndex = index;
                GD.Print($"[BattleManager] Selected '{card.Name}'. Click a valid target.");
                UpdateTargetingPreview(_selectedCardIndex, _lastHoveredCell);
            }
        }
    }

    private void ExecuteCardPlay(PlayerCharacter player, int cardIndex, Vector2I targetCell)
    {
        var playedCard = player.Hand.PlayCard(cardIndex);

        if (playedCard != null)
        {
            GD.Print($"[BattleManager] Played card '{playedCard.Name}' (Cost: {playedCard.Cost}) aiming at {targetCell}.");

            // --- Immediate Effect Resolution (M1 Stub) ---
            // Before we implement the full M2 Activation Queue, just apply the damage instantly!
            var affectedCells = Board.GetCellsInAoE(targetCell, playedCard.AoeShape, player.GridPosition);
            foreach (var cell in affectedCells)
            {
                var occupant = Board.GetOccupant(cell);
                if (occupant is EnemyCharacter enemy)
                {
                    if (playedCard.BaseDamage > 0) enemy.ModifyHp(playedCard.BaseDamage);
                    if (playedCard.BaseHealing > 0) enemy.ModifyHp(-playedCard.BaseHealing);
                }
                else if (occupant is PlayerCharacter p)
                {
                    if (playedCard.BaseDamage > 0) p.ModifyHp(playedCard.BaseDamage);
                    if (playedCard.BaseHealing > 0) p.ModifyHp(-playedCard.BaseHealing);
                }
            }
            
            // Send it to the discard pile (conceptually happens after effect resolution, doing it here for testing)
            player.Deck.Discard(playedCard);
            
            // Re-render the hand
            HUD?.UpdateHand(player.Hand.Cards);

            _selectedCardIndex = -1;
            _hoveredCardIndex = -1;
            Board.ClearHighlights();
        }
    }

    private void OnCardHovered(int index)
    {
        if (_currentPhase != BattlePhase.BattlePhase || _selectedCardIndex != -1) return;
        _hoveredCardIndex = index;
        UpdateTargetingPreview(_hoveredCardIndex, _lastHoveredCell);
    }

    private void OnCardUnhovered(int index)
    {
        if (_currentPhase != BattlePhase.BattlePhase || _selectedCardIndex != -1) return;
        if (_hoveredCardIndex == index)
        {
            _hoveredCardIndex = -1;
            Board.ClearHighlights();
        }
    }

    // -------------------------------------------------------------------------
    // Targeting Helpers
    // -------------------------------------------------------------------------
    
    private void UpdateTargetingPreview(int cardIndex, Vector2I hoveredCell)
    {
        Board.ClearHighlights();
        if (_players.Count == 0 || cardIndex < 0 || cardIndex >= _players[0].Hand.Cards.Count) return;

        var player = _players[0];
        var card = player.Hand.Cards[cardIndex];

        // 1. Determine valid aiming cells (Yellow)
        var validTargets = GetValidTargetCells(player, card);

        foreach (var cell in validTargets)
        {
            Board.HighlightCell(cell, new Color(0.8f, 0.8f, 0.2f)); // Yellow
        }

        // 2. If hovering over a valid target, show the splash AOE (Red)
        if (validTargets.Contains(hoveredCell))
        {
            var aoeCells = Board.GetCellsInAoE(hoveredCell, card.AoeShape, player.GridPosition);
            foreach (var aoeCell in aoeCells)
            {
                Board.HighlightCell(aoeCell, new Color(0.8f, 0.2f, 0.2f)); // Red
            }
        }
    }

    private List<Vector2I> GetValidTargetCells(PlayerCharacter player, CardData card)
    {
        var validCells = new List<Vector2I>();
        int range = card.Range;
        Vector2I origin = player.GridPosition;

        for (int q = -range; q <= range; q++)
        {
            for (int r = -range; r <= range; r++)
            {
                if (Mathf.Abs(q) + Mathf.Abs(r) <= range)
                {
                    var cell = origin + new Vector2I(q, r);
                    if (Board.IsInBounds(cell))
                    {
                        bool isValid = false;
                        switch (card.Target)
                        {
                            case TargetType.Self:
                                isValid = (cell == origin);
                                break;
                            case TargetType.SingleEnemy:
                                isValid = Board.IsOccupied(cell) && Board.GetOccupant(cell) is EnemyCharacter;
                                break;
                            case TargetType.AnyTile:
                                isValid = true;
                                break;
                            case TargetType.Global:
                                isValid = true;
                                break;
                        }

                        if (isValid) validCells.Add(cell);
                    }
                }
            }
        }
        return validCells;
    }
}
