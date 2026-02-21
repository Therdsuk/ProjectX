using Godot;

/// <summary>
/// Manages the battle HUD: displays the current phase, round number, and
/// a "Next Phase" button.
///
/// Attach this script to the BattleHUD CanvasLayer in BattleScene.tscn.
/// Wire the child nodes via @Export or NodePath.
/// </summary>
public partial class BattleHUD : CanvasLayer
{
    // -------------------------------------------------------------------------
    // Signals
    // -------------------------------------------------------------------------

    /// <summary>Emitted when the player presses the Next Phase button.</summary>
    [Signal] public delegate void NextPhaseRequestedEventHandler();

    // -------------------------------------------------------------------------
    // Child Node References (wire in Inspector via @Export or find by name)
    // -------------------------------------------------------------------------

    [Export] public Label  PhaseLabel  { get; set; }
    [Export] public Label  RoundLabel  { get; set; }
    [Export] public Button NextPhaseBtn { get; set; }

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        // Connect button
        if (NextPhaseBtn != null)
            NextPhaseBtn.Pressed += OnNextPhaseBtnPressed;

        // Listen for phase changes from the EventBus
        if (EventBus.Instance != null)
            EventBus.Instance.PhaseChanged += OnPhaseChanged;

        // Set initial display
        UpdatePhaseDisplay(BattlePhase.MovePhase);
        UpdateRoundDisplay(1);
    }

    // -------------------------------------------------------------------------
    // Display Updates
    // -------------------------------------------------------------------------

    private void UpdatePhaseDisplay(BattlePhase phase)
    {
        if (PhaseLabel == null) return;
        PhaseLabel.Text = phase switch
        {
            BattlePhase.MovePhase   => "⚡ MOVE PHASE",
            BattlePhase.BattlePhase => "⚔ BATTLE PHASE",
            BattlePhase.SetupPhase  => "🔧 SETUP PHASE",
            _                       => "—"
        };
    }

    public void UpdateRoundDisplay(int round)
    {
        if (RoundLabel != null)
            RoundLabel.Text = $"Round {round}";
    }

    // -------------------------------------------------------------------------
    // Signal Handlers
    // -------------------------------------------------------------------------

    private void OnNextPhaseBtnPressed()
    {
        EmitSignal(SignalName.NextPhaseRequested);
    }

    private void OnPhaseChanged(int phaseInt)
    {
        var phase = (BattlePhase)phaseInt;
        UpdatePhaseDisplay(phase);
    }
}
