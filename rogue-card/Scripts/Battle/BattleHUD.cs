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
            NextPhaseBtn.Text = phase switch
            {
                BattlePhase.MovePhase   => "Confirm Move",
                BattlePhase.BattlePhase => "Confirm Battle",
                BattlePhase.SetupPhase  => "Confirm Setup",
                _                       => "Confirm"
            };
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

    /// <summary>Preloaded CardUI scene for instantiation.</summary>
    private static readonly PackedScene CardUIScene = GD.Load<PackedScene>("res://Scenes/UI/CardUI.tscn");

    /// <summary>Clear and recreate card UI elements in the hand container.</summary>
    public void UpdateHand(System.Collections.Generic.IReadOnlyList<CardData> cards)
    {
        if (HandContainer == null) return;

        // Clear existing cards
        foreach (Node child in HandContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Create CardUI instances for each card in hand
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            int index = i; // capture for the lambda

            var cardUI = CardUIScene.Instantiate<CardUI>();
            HandContainer.AddChild(cardUI);
            cardUI.SetCard(card);

            // Disable the card if not playable in the current phase
            bool isPlayable = false;
            if (_currentPhase == BattlePhase.MovePhase && card.CardType == CardType.Move) isPlayable = true;
            if (_currentPhase == BattlePhase.BattlePhase && (card.CardType == CardType.Battle || card.CardType == CardType.Buff || card.CardType == CardType.Debuff)) isPlayable = true;
            if (_currentPhase == BattlePhase.SetupPhase && card.CardType == CardType.Setup) isPlayable = true;

            cardUI.SetDisabled(!isPlayable);

            // When clicked, request to play this card
            cardUI.CardClicked += () => EmitSignal(SignalName.CardPlayedRequested, index);
            
            // Hover logic for previewing targeting
            cardUI.CardHoverEntered += () => EmitSignal(SignalName.CardHovered, index);
            cardUI.CardHoverExited += () => EmitSignal(SignalName.CardUnhovered, index);
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

        // Add CardUI elements for each queued action (smaller size for the queue)
        foreach (var action in queue)
        {
            var cardUI = CardUIScene.Instantiate<CardUI>();
            cardUI.CustomMinimumSize = new Vector2(90, 130);
            QueueContainer.AddChild(cardUI);
            cardUI.SetCard(action.Card);
            cardUI.SetDisabled(true); // Queue cards are display-only
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
