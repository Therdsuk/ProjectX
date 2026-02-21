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

    [Export] public BattleBoard Board { get; set; }
    [Export] public BattleHUD   HUD   { get; set; }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private BattlePhase _currentPhase = BattlePhase.MovePhase;
    private int         _roundNumber  = 1;

    private readonly List<PlayerCharacter> _players = new();
    private readonly List<EnemyCharacter>  _enemies = new();

    private bool _battleActive = false;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        // Connect HUD next-phase button
        if (HUD != null)
            HUD.NextPhaseRequested += OnNextPhaseRequested;

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
        // M1: placeholder — players will move characters here
        GD.Print("[BattleManager] Move Phase: players may move and play Move cards.");
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

    // -------------------------------------------------------------------------
    // Input (keyboard shortcut for quick testing during M1)
    // -------------------------------------------------------------------------

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Space)
                AdvancePhase();
        }
    }
}
