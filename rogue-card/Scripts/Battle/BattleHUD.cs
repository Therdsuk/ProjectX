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

    /// <summary>Emitted when the player clicks a card button in their hand.</summary>
    [Signal] public delegate void CardPlayedRequestedEventHandler(int cardIndex);

    /// <summary>Emitted when the player hovers over a card button in their hand.</summary>
    [Signal] public delegate void CardHoveredEventHandler(int cardIndex);

    /// <summary>Emitted when the player's mouse leaves a card button in their hand.</summary>
    [Signal] public delegate void CardUnhoveredEventHandler(int cardIndex);

    // -------------------------------------------------------------------------
    // Child Node References (wire in Inspector via @Export or find by name)
    // -------------------------------------------------------------------------

    [Export] public Label  PhaseLabel  { get; set; }
    [Export] public Label  RoundLabel  { get; set; }
    [Export] public Button NextPhaseBtn { get; set; }
    [Export] public HBoxContainer HandContainer { get; set; }

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

    /// <summary>Clear and recreate card buttons in the hand container.</summary>
    public void UpdateHand(System.Collections.Generic.IReadOnlyList<CardData> cards)
    {
        if (HandContainer == null) return;

        // Clear existing card buttons
        foreach (Node child in HandContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Create new buttons for each card in hand
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            int index = i; // capture for the lambda

            var btn = new Button
            {
                Text = $"{card.Name}\nCost: {card.Cost}",
                CustomMinimumSize = new Vector2(100, 140)
            };

            // When clicked, request to play this card
            btn.Pressed += () => EmitSignal(SignalName.CardPlayedRequested, index);
            
            // Hover logic for previewing targeting
            btn.MouseEntered += () => EmitSignal(SignalName.CardHovered, index);
            btn.MouseExited += () => EmitSignal(SignalName.CardUnhovered, index);

            HandContainer.AddChild(btn);
        }
        GD.Print($"[BattleHUD] Rendered {cards.Count} cards in hand.");
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
