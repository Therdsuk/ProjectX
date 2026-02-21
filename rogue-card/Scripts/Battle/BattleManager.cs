using Godot;
using RogueCard.Core;
using System;
using System.Collections.Generic;

/// <summary>
/// Main battle manager that coordinates the entire battle system
/// </summary>
public partial class BattleManager : Node
{
    [Signal]
    public delegate void BattleStartedEventHandler();

    [Signal]
    public delegate void BattleEndedEventHandler(string winner);

    private PhaseManager _phaseManager;
    private BoardManager _boardManager;
    private Node3D _battleScene;

    [Export]
    public bool DEBUG_MODE = true;

    public PhaseManager PhaseManager => _phaseManager;
    public BoardManager BoardManager => _boardManager;

    public override void _Ready()
    {
        GD.Print("BattleManager: Initializing battle system...");
        
        // Get or create phase manager
        _phaseManager = GetNodeOrNull<PhaseManager>("PhaseManager");
        if (_phaseManager == null)
        {
            _phaseManager = new PhaseManager();
            _phaseManager.Name = "PhaseManager";
            AddChild(_phaseManager);
        }

        // Get or create board manager
        _boardManager = GetNodeOrNull<BoardManager>("BoardVisuals/BoardManager");
        if (_boardManager == null)
        {
            var visualsNode = GetNode<Node3D>("BoardVisuals");
            _boardManager = new BoardManager();
            _boardManager.Name = "BoardManager";
            visualsNode.AddChild(_boardManager);
        }

        // Connect phase signals
        if (_phaseManager != null)
        {
            _phaseManager.PhaseChanged += OnPhaseChanged;
            _phaseManager.PhaseStarted += OnPhaseStarted;
            _phaseManager.PhaseEnded += OnPhaseEnded;
        }

        GD.Print("BattleManager: Initialization complete");
        EmitSignal(SignalName.BattleStarted);
    }

    public override void _Process(double delta)
    {
        // Handle input for phase changes (for testing)
        if (DEBUG_MODE && Input.IsActionJustPressed("ui_accept"))
        {
            AdvancePhase();
        }
    }

    /// <summary>
    /// Advance to the next battle phase
    /// </summary>
    public void AdvancePhase()
    {
        if (_phaseManager != null)
        {
            _phaseManager.AdvancePhase();
        }
    }

    /// <summary>
    /// Set a specific battle phase
    /// </summary>
    public void SetPhase(BattlePhase phase)
    {
        if (_phaseManager != null)
        {
            _phaseManager.SetPhase(phase);
        }
    }

    /// <summary>
    /// Get current battle phase
    /// </summary>
    public BattlePhase GetCurrentPhase()
    {
        return _phaseManager?.CurrentPhase ?? BattlePhase.Move;
    }

    /// <summary>
    /// Get current round number
    /// </summary>
    public int GetCurrentRound()
    {
        return _phaseManager?.CurrentRound ?? 1;
    }

    // Signal handlers
    private void OnPhaseChanged(BattlePhase newPhase)
    {
        GD.Print($"BattleManager: Phase changed to {newPhase}");
        if (DEBUG_MODE)
        {
            GD.Print($"  Description: {_phaseManager.GetPhaseDescription()}");
        }
    }

    private void OnPhaseStarted(BattlePhase phase)
    {
        GD.Print($"BattleManager: {_phaseManager.GetPhaseDisplayName()} started");
    }

    private void OnPhaseEnded(BattlePhase phase)
    {
        GD.Print($"BattleManager: {phase} ended");
    }

    /// <summary>
    /// End the current battle
    /// </summary>
    public void EndBattle(string winner)
    {
        GD.Print($"BattleManager: Battle ended - Winner: {winner}");
        EmitSignal(SignalName.BattleEnded, winner);
    }
}
