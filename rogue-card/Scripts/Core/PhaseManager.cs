using Godot;
using System;

/// <summary>
/// Manages battle phase transitions and state
/// </summary>
public partial class PhaseManager : Node
{
    [Signal]
    public delegate void PhaseChangedEventHandler(BattlePhase newPhase);

    [Signal]
    public delegate void PhaseStartedEventHandler(BattlePhase phase);

    [Signal]
    public delegate void PhaseEndedEventHandler(BattlePhase phase);

    private BattlePhase _currentPhase = BattlePhase.Move;
    private int _currentRound = 1;
    private float _phaseTimer = 0f;
    private float _phaseDuration = 30f; // 30 seconds per phase

    [Export]
    public float PhaseDuration { get; set; } = 30f;

    public BattlePhase CurrentPhase => _currentPhase;
    public int CurrentRound => _currentRound;

    public override void _Ready()
    {
        GD.Print($"PhaseManager: Starting Battle - Round {_currentRound}, Phase: {_currentPhase}");
        EmitSignal(SignalName.PhaseStarted, _currentPhase);
    }

    public override void _Process(double delta)
    {
        // Optional: Implement auto-phase transitions based on timer
        // For now, phases are manually advanced
    }

    /// <summary>
    /// Advance to the next phase in the battle round
    /// </summary>
    public void AdvancePhase()
    {
        // Emit phase ended signal
        EmitSignal(SignalName.PhaseEnded, _currentPhase);

        // Move to next phase
        BattlePhase nextPhase = (BattlePhase)(((int)_currentPhase + 1) % 3);

        // If we've cycled back to Move phase, increment round
        if (nextPhase == BattlePhase.Move && _currentPhase == BattlePhase.Setup)
        {
            _currentRound++;
            GD.Print($"PhaseManager: Round {_currentRound} started");
        }

        _currentPhase = nextPhase;
        _phaseTimer = 0f;

        GD.Print($"PhaseManager: Phase changed to {_currentPhase} (Round {_currentRound})");
        EmitSignal(SignalName.PhaseChanged, _currentPhase);
        EmitSignal(SignalName.PhaseStarted, _currentPhase);
    }

    /// <summary>
    /// Set a specific phase directly
    /// </summary>
    public void SetPhase(BattlePhase phase)
    {
        if (_currentPhase == phase)
            return;

        EmitSignal(SignalName.PhaseEnded, _currentPhase);
        _currentPhase = phase;
        _phaseTimer = 0f;

        GD.Print($"PhaseManager: Phase set to {_currentPhase}");
        EmitSignal(SignalName.PhaseChanged, _currentPhase);
        EmitSignal(SignalName.PhaseStarted, _currentPhase);
    }

    /// <summary>
    /// Get the name of the current phase
    /// </summary>
    public string GetPhaseDisplayName()
    {
        return _currentPhase switch
        {
            BattlePhase.Move => "MOVE PHASE",
            BattlePhase.Battle => "BATTLE PHASE",
            BattlePhase.Setup => "SETUP PHASE",
            _ => "UNKNOWN"
        };
    }

    /// <summary>
    /// Get the description of the current phase
    /// </summary>
    public string GetPhaseDescription()
    {
        return _currentPhase switch
        {
            BattlePhase.Move => "Move characters on the board and play Move-type cards",
            BattlePhase.Battle => "Play Attack and Buff cards. Cards execute ordered by Speed",
            BattlePhase.Setup => "Place traps and modify field effects",
            _ => "Unknown phase"
        };
}
