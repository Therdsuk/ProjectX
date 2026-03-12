using Godot;
using System.Collections.Generic;
using System.Linq;

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
    
    // Movement Selection State
    private Vector2I _moveOrigin;
    private Vector2I _selectedMoveTarget = new Vector2I(-999, -999);
    private bool _hasConfirmedMove = false;
    
    // Host Movement Resolution
    private readonly Dictionary<ulong, Vector2I> _confirmedMoves = new();
    private bool _isResolvingMoves = false;

    // Targeting State
    private int _hoveredCardIndex = -1;
    private int _selectedCardIndex = -1;
    private Vector2I _lastHoveredCell = new Vector2I(-999, -999);

    // M2 Activation Queue
    private readonly List<QueuedAction> _activationQueue = new();
    private bool _isResolvingQueue = false;

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

        SteamManager.OnNetworkMessageEvent += OnSteamNetworkMessage;

        // M1: auto-start battle when scene loads
        StartBattle();
    }

    public override void _ExitTree()
    {
        SteamManager.OnNetworkMessageEvent -= OnSteamNetworkMessage;
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
    public async void AdvancePhase()
    {
        if (!_battleActive || _isResolvingQueue || _isResolvingMoves) return;

        // Only the Host is allowed to advance the phase!
        if (SteamManager.Instance != null && SteamManager.Instance.CurrentLobby.HasValue)
        {
            if (SteamManager.Instance.CurrentLobby.Value.Owner.Id != Steamworks.SteamClient.SteamId)
            {
                GD.Print("[BattleManager] Only the Host can advance the phase.");
                return;
            }
        }

        // Check win/lose before advancing
        if (CheckBattleEnd()) return;

        // Try to intercept leaving the Battle Phase to resolve the Queue first
        if (_currentPhase == BattlePhase.BattlePhase)
        {
            _isResolvingQueue = true;

            // Before resolving, the Host allows the AI to inject their cards into the queue!
            if (SteamManager.Instance == null || !SteamManager.Instance.CurrentLobby.HasValue || 
                SteamManager.Instance.CurrentLobby.Value.Owner.Id == Steamworks.SteamClient.SteamId)
            {
                ProcessAIBattlePhase();
            }

            if (SteamManager.Instance != null && SteamManager.Instance.CurrentLobby.HasValue)
            {
                SteamManager.Instance.BroadcastMessage("QUEUE_RESOLVE_START");
            }
            
            await ResolveActivationQueueAsync();
            
            _currentPhase = BattlePhase.SetupPhase;
            
            if (SteamManager.Instance != null && SteamManager.Instance.CurrentLobby.HasValue)
            {
                SteamManager.Instance.BroadcastMessage($"PHASE:{(int)_currentPhase}:{_roundNumber}");
            }
            EnterPhase(_currentPhase);
            _isResolvingQueue = false;
            return;
        }

        _currentPhase = _currentPhase switch
        {
            BattlePhase.MovePhase   => BattlePhase.BattlePhase,
            BattlePhase.BattlePhase => BattlePhase.SetupPhase,
            BattlePhase.SetupPhase  => NextRound(),
            _                       => BattlePhase.MovePhase
        };

        // Broadcast to all clients!
        if (SteamManager.Instance != null && SteamManager.Instance.CurrentLobby.HasValue)
        {
            SteamManager.Instance.BroadcastMessage($"PHASE:{(int)_currentPhase}:{_roundNumber}");
        }

        EnterPhase(_currentPhase);
    }

    private void OnSteamNetworkMessage(Steamworks.SteamId sender, string message)
    {
        GD.Print($"[BattleManager] Received Network Msg from {sender.Value}: {message}");
        var parts = message.Split(':');
        
        if (parts.Length > 0 && parts[0] == "PHASE")
        {
            if (parts.Length == 3 && int.TryParse(parts[1], out int phaseId) && int.TryParse(parts[2], out int round))
            {
                // Sync to Host's active phase and round
                _roundNumber = round;
                _currentPhase = (BattlePhase)phaseId;
                
                // If we skipped BattlePhase (because the host resolved it instantly), we still need to run resolution locally
                // Note: The host should technically send all the exact battle queue events, but for now we sync phases
                EnterPhase(_currentPhase);
            }
        }
        else if (parts[0] == "MOVE_LOCK")
        {
            // Host receives move lock from clients
            if (parts.Length == 3 && int.TryParse(parts[1], out int targetX) && int.TryParse(parts[2], out int targetY))
            {
                HostProcessMoveLock(sender, new Vector2I(targetX, targetY));
            }
        }
        else if (parts[0] == "MOVE_CONFIRM")
        {
            // All clients receive this directive from the Host
            if (parts.Length == 4 && ulong.TryParse(parts[1], out ulong pId) && int.TryParse(parts[2], out int tX) && int.TryParse(parts[3], out int tY))
            {
                var targetCell = new Vector2I(tX, tY);
                var movingPlayer = _players.Find(p => p.SteamId == pId);
                
                Node3D movingEntity = movingPlayer;
                
                // If it wasn't a player, see if it was an enemy (Fake IDs start at 999000)
                if (movingPlayer == null && pId >= 999000 && pId - 999000 < (ulong)_enemies.Count)
                {
                    movingEntity = _enemies[(int)(pId - 999000)];
                }

                if (movingEntity != null)
                {
                    Vector2I currentPos = (movingEntity is PlayerCharacter pc) ? pc.GridPosition : ((EnemyCharacter)movingEntity).GridPosition;
                    Board.MoveUnit(movingEntity, currentPos, targetCell);
                    
                    if (movingEntity is PlayerCharacter p) p.GridPosition = targetCell;
                    if (movingEntity is EnemyCharacter e) e.GridPosition = targetCell;
                    
                    // If it was the local player and we were still in move phase unconfirmed, refresh updates
                    if (movingEntity == _players[0] && _currentPhase == BattlePhase.MovePhase && !_hasConfirmedMove)
                    {
                        ShowValidMoves(_players[0]);
                    }
                }
            }
        }
        else if (parts[0] == "QUEUE_ACTION")
        {
            if (parts.Length == 4 && int.TryParse(parts[2], out int tX) && int.TryParse(parts[3], out int tY))
            {
                HostProcessCardPlay(sender, parts[1], new Vector2I(tX, tY));
            }
        }
        else if (parts[0] == "QUEUE_ADD")
        {
            if (parts.Length == 5 && ulong.TryParse(parts[1], out ulong pId) && int.TryParse(parts[3], out int tX) && int.TryParse(parts[4], out int tY))
            {
                string cardId = parts[2];
                var player = _players.Find(p => p.SteamId == pId);
                if (player != null)
                {
                    // Find a card by ID in hand
                    var targetCard = player.Hand.Cards.FirstOrDefault(c => c.Id == cardId);
                    
                    // Fallback: If local simulation desynchronised because of RNG, grab the reference from ANY deck
                    if (targetCard == null)
                    {
                        foreach (var p in _players)
                        {
                            targetCard = p.Deck.DrawPile.FirstOrDefault(c => c.Id == cardId) ?? 
                                         p.Hand.Cards.FirstOrDefault(c => c.Id == cardId) ??
                                         p.Deck.DiscardPile.FirstOrDefault(c => c.Id == cardId);
                            if (targetCard != null) break;
                        }
                    }

                    if (targetCard != null)
                    {
                        // Remove from hand physically (won't crash if it wasn't there)
                        player.Hand.PlayCard(targetCard);
                        var targetCell = new Vector2I(tX, tY);
                        _activationQueue.Add(new QueuedAction(player.Data, targetCard, targetCell, player.GridPosition));
                        
                        // Resort instantly for visual display
                        _activationQueue.Sort((a, b) => a.Card.Speed.CompareTo(b.Card.Speed));
                        
                        player.Deck.Discard(targetCard);
                        if (player == _players[0]) HUD?.UpdateHand(player.Hand.Cards);
                        HUD?.UpdateQueueDisplay(_activationQueue);
                    }
                }
            }
        }
        else if (parts[0] == "QUEUE_ADD_AI")
        {
            // QUEUE_ADD_AI:EnemyName:CardId:TargetX:TargetY
            if (parts.Length == 5 && int.TryParse(parts[3], out int tX) && int.TryParse(parts[4], out int tY))
            {
                string enemyName = parts[1];
                string cardId = parts[2];
                var enemy = _enemies.Find(e => e.Name == enemyName);

                if (enemy != null)
                {
                    var targetCard = enemy.Hand.Cards.FirstOrDefault(c => c.Id == cardId) 
                                     ?? enemy.Deck.DrawPile.FirstOrDefault(c => c.Id == cardId) 
                                     ?? enemy.Deck.DiscardPile.FirstOrDefault(c => c.Id == cardId);

                    if (targetCard != null)
                    {
                        enemy.Hand.PlayCard(targetCard);
                        var targetCell = new Vector2I(tX, tY);
                        _activationQueue.Add(new QueuedAction(enemy.Data, targetCard, targetCell, enemy.GridPosition));
                        
                        _activationQueue.Sort((a, b) => a.Card.Speed.CompareTo(b.Card.Speed));
                        
                        enemy.Deck.Discard(targetCard);
                        HUD?.UpdateQueueDisplay(_activationQueue);
                    }
                }
            }
        }
        else if (parts[0] == "QUEUE_RESOLVE_START")
        {
            _ = ResolveActivationQueueAsync();
        }
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

        // Notify the event bus (HUD listens to update its label and internal phase)
        EventBus.Instance?.EmitSignal(EventBus.SignalName.PhaseChanged, (int)phase);

        // Update the hand UI to reflect the new phase (enables/disables buttons accordingly)
        if (_players.Count > 0)
        {
            HUD?.UpdateHand(_players[0].Hand.Cards);
        }

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
        
        _hasConfirmedMove = false;
        _selectedMoveTarget = new Vector2I(-999, -999);
        _confirmedMoves.Clear();
        _isResolvingMoves = false;

        if (_players.Count > 0)
        {
            _moveOrigin = _players[0].GridPosition;
            _selectedMoveTarget = _moveOrigin;
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
        if (_currentPhase == BattlePhase.MovePhase)
        {
            _moveOrigin = player.GridPosition;
            _selectedMoveTarget = _moveOrigin;
            _hasConfirmedMove = false;
            ShowValidMoves(player);
        }

        // Draw the initial cards for all units and update HUD
        RefillAllHands();
    }

    // -------------------------------------------------------------------------
    // M1 Mechanics (Test Movement & Highlighting)
    // -------------------------------------------------------------------------

    private void ShowValidMoves(PlayerCharacter player)
    {
        Board?.ClearHighlights();
        _validMoves.Clear();

        for (int q = -_testMoveRange; q <= _testMoveRange; q++)
        {
            for (int r = -_testMoveRange; r <= _testMoveRange; r++)
            {
                if (Mathf.Abs(q) + Mathf.Abs(r) <= _testMoveRange)
                {
                    Vector2I checkPos = _moveOrigin + new Vector2I(q, r);

                    if (Board.IsInBounds(checkPos))
                    {
                        var occupant = Board.GetOccupant(checkPos);
                        // Allow moving through/onto other players, but not enemies
                        bool blocked = occupant != null && occupant is EnemyCharacter;
                        
                        if (!blocked || checkPos == _moveOrigin)
                        {
                            _validMoves.Add(checkPos);
                            Board.HighlightCell(checkPos, new Color(0.1f, 0.7f, 0.2f)); 
                        }
                    }
                }
            }
        }
        
        // Highlight Origin in Blue
        Board.HighlightCell(_moveOrigin, new Color(0.2f, 0.4f, 0.8f));
        
        // Highlight Current Position distinctly (Yellow) if we moved away from origin
        // (For M1 logic, this will highlight the selected target if it's not the origin)
        if (_selectedMoveTarget != new Vector2I(-999, -999) && _selectedMoveTarget != _moveOrigin)
        {
            Board.HighlightCell(_selectedMoveTarget, new Color(0.2f, 0.8f, 0.8f)); // Cyan
        }
    }

    public override void _Input(InputEvent @event)
    {
        // Spacebar to skip phases
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Space)
        {
            if (_currentPhase == BattlePhase.MovePhase)
            {
                if (!_hasConfirmedMove) OnNextPhaseRequested();
                return;
            }

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
            if (_currentPhase == BattlePhase.MovePhase && _players.Count > 0)
            {
                if (MainCamera == null) return;

                // Right Click to Cancel back to origin
                if (mouseBtn.ButtonIndex == MouseButton.Right)
                {
                    CancelPendingMove();
                    return;
                }

                // Left Click to Request Move
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
        if (_hasConfirmedMove) return;

        // Check if the clicked cell is valid (or the origin to cancel)
        if (_validMoves.Contains(targetPos) || targetPos == _moveOrigin)
        {
            _selectedMoveTarget = targetPos;
            GD.Print($"[BattleManager] Selected move to {targetPos}. Waiting for confirmation.");
            
            if (_players.Count > 0)
            {
                ShowValidMoves(_players[0]);
            }
        }
        else
        {
            GD.Print($"[BattleManager] Move to {targetPos} is not valid.");
        }
    }

    private void HostProcessMoveLock(Steamworks.SteamId sender, Vector2I targetPos)
    {
        _confirmedMoves[sender.Value] = targetPos;
        GD.Print($"[BattleManager] Player {sender.Value} locked move to {targetPos}. Confirmed: {_confirmedMoves.Count}/{_players.Count}");

        int expectedPlayers = (SteamManager.Instance != null && SteamManager.Instance.CurrentLobby.HasValue) 
            ? SteamManager.Instance.CurrentLobby.Value.MemberCount 
            : _players.Count;

        if (_confirmedMoves.Count >= expectedPlayers)
        {
            // Human players have confirmed. Host now calculates AI moves.
            ulong aiFakeIdCounter = 999000;
            foreach (var enemy in _enemies)
            {
                if (!enemy.IsAlive) continue;
                Vector2I aiMove = enemy.DecideMoveTarget(Board, _players, _confirmedMoves);
                _confirmedMoves[aiFakeIdCounter] = aiMove; 
                aiFakeIdCounter++;
            }

            _ = ResolveMovesAsync();
        }
    }

    private async System.Threading.Tasks.Task ResolveMovesAsync()
    {
        _isResolvingMoves = true;
        Board?.ClearHighlights();
        
        // Combine Players and Enemies that have a move
        var playerMovers = _players.Where(p => _confirmedMoves.ContainsKey(p.SteamId));
        
        // Map the fake IDs back to the enemies
        var enemyMovers = new List<EnemyCharacter>();
        ulong checkId = 999000;
        foreach (var e in _enemies)
        {
            if (e.IsAlive && _confirmedMoves.ContainsKey(checkId))
            {
                enemyMovers.Add(e);
            }
            checkId++;
        }

        // Create a separate list for players, sorted by base speed
        var playerMoversList = new List<(Node3D Unit, int Speed, ulong Id, Vector2I Target)>();
        foreach (var p in playerMovers) playerMoversList.Add((p, p.Data.BaseSpeed, p.SteamId, _confirmedMoves[p.SteamId]));
        playerMoversList.Sort((a, b) => b.Speed.CompareTo(a.Speed));
        
        // Create a separate list for enemies, sorted by base speed
        var enemyMoversList = new List<(Node3D Unit, int Speed, ulong Id, Vector2I Target)>();
        checkId = 999000;
        foreach (var e in enemyMovers)
        {
            enemyMoversList.Add((e, e.Data.BaseSpeed, checkId, _confirmedMoves[checkId]));
            checkId++;
        }
        enemyMoversList.Sort((a, b) => b.Speed.CompareTo(a.Speed));

        // Combine them so players always move before enemies
        var allMovers = new List<(Node3D Unit, int Speed, ulong Id, Vector2I Target)>();
        allMovers.AddRange(playerMoversList);
        allMovers.AddRange(enemyMoversList);
        
        foreach (var mover in allMovers)
        {
            if (SteamManager.Instance != null && SteamManager.Instance.CurrentLobby.HasValue)
            {
                // We broadcast all moves so clients see the enemies move too!
                SteamManager.Instance.BroadcastMessage($"MOVE_CONFIRM:{mover.Id}:{mover.Target.X}:{mover.Target.Y}");
            }
            OnSteamNetworkMessage(Steamworks.SteamClient.SteamId, $"MOVE_CONFIRM:{mover.Id}:{mover.Target.X}:{mover.Target.Y}");
            
            await ToSignal(GetTree().CreateTimer(0.3f), SceneTreeTimer.SignalName.Timeout);
        }
        
        _isResolvingMoves = false;
        AdvancePhase();
    }

    private void CancelPendingMove()
    {
        if (_hasConfirmedMove) return;
        TrySelectOrConfirmMove(_moveOrigin);
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
        foreach (var enemy in _enemies)
        {
            if (enemy.IsAlive)
            {
                enemy.DrawToHandLimit();
            }
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
        if (_currentPhase == BattlePhase.MovePhase)
        {
            if (_hasConfirmedMove) return;
            
            _hasConfirmedMove = true;
            HUD?.SetNextPhaseButton("Waiting for others...", true);

            var steamId = Steamworks.SteamClient.IsValid ? Steamworks.SteamClient.SteamId : 0;
            
            if (SteamManager.Instance != null && SteamManager.Instance.CurrentLobby.HasValue)
            {
                var hostId = SteamManager.Instance.CurrentLobby.Value.Owner.Id;
                if (hostId == Steamworks.SteamClient.SteamId)
                {
                    HostProcessMoveLock(steamId, _selectedMoveTarget);
                }
                else
                {
                    SteamManager.Instance.SendMessageToHost($"MOVE_LOCK:{_selectedMoveTarget.X}:{_selectedMoveTarget.Y}");
                }
            }
            else
            {
                // Singleplayer fallback or before connection established
                HostProcessMoveLock(steamId, _selectedMoveTarget);
            }
        }
        else
        {
            AdvancePhase();
        }
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
        var card = player.Hand.Cards[cardIndex];
        
        if (SteamManager.Instance != null && SteamManager.Instance.CurrentLobby.HasValue)
        {
            var hostId = SteamManager.Instance.CurrentLobby.Value.Owner.Id;
            if (hostId == Steamworks.SteamClient.SteamId)
            {
                HostProcessCardPlay(Steamworks.SteamClient.SteamId, card.Id, targetCell);
            }
            else
            {
                SteamManager.Instance.SendMessageToHost($"QUEUE_ACTION:{card.Id}:{targetCell.X}:{targetCell.Y}");
            }
        }

        _selectedCardIndex = -1;
        _hoveredCardIndex = -1;
        Board.ClearHighlights();
    }

    private void HostProcessCardPlay(Steamworks.SteamId sender, string cardId, Vector2I targetCell)
    {
        SteamManager.Instance.BroadcastMessage($"QUEUE_ADD:{sender.Value}:{cardId}:{targetCell.X}:{targetCell.Y}");
        OnSteamNetworkMessage(Steamworks.SteamClient.SteamId, $"QUEUE_ADD:{sender.Value}:{cardId}:{targetCell.X}:{targetCell.Y}");
    }

    private void ProcessAIBattlePhase()
    {
        GD.Print("[BattleManager] Host is processing AI Battle Phase decisions...");
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsAlive) continue;

            QueuedAction? actionResult = enemy.DecideCardPlay(Board, _players, _enemies);
            
            if (actionResult.HasValue)
            {
                var action = actionResult.Value;
                
                // Add to the local activation queue
                _activationQueue.Add(action);
                
                // Tell clients what the AI queued
                if (SteamManager.Instance != null && SteamManager.Instance.CurrentLobby.HasValue)
                {
                    SteamManager.Instance.BroadcastMessage($"QUEUE_ADD_AI:{enemy.Name}:{action.Card.Id}:{action.TargetCell.X}:{action.TargetCell.Y}");
                }
                
                // Enemy physically discs the card (Host only visual, clients sync via network message handling)
                enemy.Hand.PlayCard(action.Card);
                enemy.Deck.Discard(action.Card);
            }
            else
            {
                GD.Print($"[BattleManager] AI {enemy.Data?.ClassName} decided to skip turn.");
            }
        }
        
        // Final resort to interleave player and enemy speeds!
        _activationQueue.Sort((a, b) => a.Card.Speed.CompareTo(b.Card.Speed));
        HUD?.UpdateQueueDisplay(_activationQueue);
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

    // -------------------------------------------------------------------------
    // Activation Queue Resolution
    // -------------------------------------------------------------------------
    
    private async System.Threading.Tasks.Task ResolveActivationQueueAsync()
    {
        if (_activationQueue.Count == 0) return;

        GD.Print($"[BattleManager] Resolving Activation Queue ({_activationQueue.Count} actions)...");

        // The queue is already pre-sorted by CardSpeed upon addition
        HUD?.UpdateQueueDisplay(_activationQueue);

        while (_activationQueue.Count > 0)
        {
            var action = _activationQueue[0];
            _activationQueue.RemoveAt(0);

            // Wait 1.5 seconds so players can see the card highlight or parse what's happening
            await ToSignal(GetTree().CreateTimer(1.5f), SceneTreeTimer.SignalName.Timeout);

            GD.Print($"  -> Executing {action.Card.Name} (Speed: {action.Card.Speed}) at target {action.TargetCell}");
            
            var affectedCells = Board.GetCellsInAoE(action.TargetCell, action.Card.AoeShape, action.CasterOrigin);
            foreach (var cell in affectedCells)
            {
                var occupant = Board.GetOccupant(cell);
                if (occupant is EnemyCharacter enemy)
                {
                    if (action.Card.BaseDamage > 0) enemy.ModifyHp(action.Card.BaseDamage);
                    if (action.Card.BaseHealing > 0) enemy.ModifyHp(-action.Card.BaseHealing);
                }
                else if (occupant is PlayerCharacter p)
                {
                    if (action.Card.BaseDamage > 0) p.ModifyHp(action.Card.BaseDamage);
                    if (action.Card.BaseHealing > 0) p.ModifyHp(-action.Card.BaseHealing);
                }
            }

            // Update display to shrink the queue natively
            HUD?.UpdateQueueDisplay(_activationQueue);
        }
    }
}
