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

    private BattlePhase _currentPhase = BattlePhase.MovePhase;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public HBoxContainer QueueContainer { get; private set; }

    public override void _Ready()
    {
        // Dynamically build the Visual Queue Container at the top of the screen
        var queueMargin = new MarginContainer();
        queueMargin.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        queueMargin.AddThemeConstantOverride("margin_top", 10);
        queueMargin.MouseFilter = Control.MouseFilterEnum.Ignore;
        
        QueueContainer = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        queueMargin.AddChild(QueueContainer);
        AddChild(queueMargin);

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
        if (PhaseLabel != null)
        {
            PhaseLabel.Text = phase switch
            {
                BattlePhase.MovePhase   => "⚡ MOVE PHASE",
                BattlePhase.BattlePhase => "⚔ BATTLE PHASE",
                BattlePhase.SetupPhase  => "🔧 SETUP PHASE",
                _                       => "—"
            };
        }

        if (NextPhaseBtn != null)
        {
            NextPhaseBtn.Text = phase == BattlePhase.MovePhase ? "Confirm Move" : "Next Phase";
            NextPhaseBtn.Disabled = false;
        }
    }

    public void SetNextPhaseButton(string text, bool disabled)
    {
        if (NextPhaseBtn != null)
        {
            NextPhaseBtn.Text = text;
            NextPhaseBtn.Disabled = disabled;
        }
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

            // Disable the button if the card is not playable in the current phase
            bool isPlayable = false;
            if (_currentPhase == BattlePhase.MovePhase && card.CardType == CardType.Move) isPlayable = true;
            if (_currentPhase == BattlePhase.BattlePhase && (card.CardType == CardType.Battle || card.CardType == CardType.Buff || card.CardType == CardType.Debuff)) isPlayable = true;
            if (_currentPhase == BattlePhase.SetupPhase && card.CardType == CardType.Setup) isPlayable = true;

            btn.Disabled = !isPlayable;

            // When clicked, request to play this card
            btn.Pressed += () => EmitSignal(SignalName.CardPlayedRequested, index);
            
            // Hover logic for previewing targeting (only emit if not disabled)
            btn.MouseEntered += () => { if (!btn.Disabled) EmitSignal(SignalName.CardHovered, index); };
            btn.MouseExited += () => { if (!btn.Disabled) EmitSignal(SignalName.CardUnhovered, index); };

            HandContainer.AddChild(btn);
        }
        GD.Print($"[BattleHUD] Rendered {cards.Count} cards in hand.");
    }

    public void UpdateQueueDisplay(System.Collections.Generic.IReadOnlyList<QueuedAction> queue)
    {
        if (QueueContainer == null) return;

        // Clear existing visual queue
        foreach (Node child in QueueContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Add new UI elements for each queued action
        foreach (var action in queue)
        {
            var panel = new PanelContainer();
            panel.CustomMinimumSize = new Vector2(90, 120);
            
            var lbl = new Label
            {
                Text = $"{action.Card.Name}\nLvl {action.Card.UpgradeLevel}\nSpd: {action.Card.Speed}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.Word
            };
            panel.AddChild(lbl);
            QueueContainer.AddChild(panel);
        }
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
        _currentPhase = (BattlePhase)phaseInt;
        UpdatePhaseDisplay(_currentPhase);
    }
}
