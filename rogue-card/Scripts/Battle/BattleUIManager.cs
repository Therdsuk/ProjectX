using Godot;
using RogueCard.Core;

/// <summary>
/// Manages the battle UI including phase display and controls
/// </summary>
public partial class BattleUIManager : CanvasLayer
{
    private BattleManager _battleManager;
    private Label _phaseLabel;
    private Label _roundLabel;
    private Label _phaseDescriptionLabel;
    private Button _advancePhaseButton;
    private VBoxContainer _phoneContainer;

    public override void _Ready()
    {
        _battleManager = GetTree().Root.GetChild(0).FindChild("BattleManager", owned: false) as BattleManager;
        
        if (_battleManager == null)
        {
            GD.PushError("BattleUIManager: Could not find BattleManager");
            return;
        }

        CreateUI();
        ConnectSignals();
        UpdateDisplay();

        GD.Print("BattleUIManager: UI ready");
    }

    /// <summary>
    /// Create the UI elements
    /// </summary>
    private void CreateUI()
    {
        // Create a control to hold UI elements
        var container = new Control();
        container.AnchorLeft = 0;
        container.AnchorTop = 0;
        container.AnchorRight = 1;
        container.AnchorBottom = 1;
        AddChild(container);

        // Top panel for phase info
        var topPanel = new PanelContainer();
        topPanel.AnchorRight = 1;
        topPanel.CustomMinimumSize = new Vector2(0, 100);
        container.AddChild(topPanel);

        var topVBox = new VBoxContainer();
        topPanel.AddChild(topVBox);

        // Round label
        _roundLabel = new Label();
        _roundLabel.Text = "Round: 1";
        _roundLabel.AddThemeStyleOverride("normal", new StyleBoxEmpty());
        topVBox.AddChild(_roundLabel);

        // Phase label
        _phaseLabel = new Label();
        _phaseLabel.Text = "MOVE PHASE";
        _phaseLabel.AddThemeFontSizeOverride("font_size", 32);
        topVBox.AddChild(_phaseLabel);

        // Phase description
        _phaseDescriptionLabel = new Label();
        _phaseDescriptionLabel.Text = "Move characters and play Move-type cards";
        _phaseDescriptionLabel.WordWrapMode = TextServer.WordWrapMode.Word;
        topVBox.AddChild(_phaseDescriptionLabel);

        // Bottom panel for controls
        var bottomPanel = new PanelContainer();
        bottomPanel.AnchorTop = 1;
        bottomPanel.AnchorBottom = 1;
        bottomPanel.AnchorRight = 1;
        bottomPanel.CustomMinimumSize = new Vector2(0, 80);
        bottomPanel.OffsetTop = -80;
        container.AddChild(bottomPanel);

        var bottomHBox = new HBoxContainer();
        bottomPanel.AddChild(bottomHBox);

        _advancePhaseButton = new Button();
        _advancePhaseButton.Text = "End Phase (Space)";
        _advancePhaseButton.Pressed += OnAdvancePhasePressed;
        bottomHBox.AddChild(_advancePhaseButton);

        var debugLabel = new Label();
        debugLabel.Text = "[DEBUG MODE] Press Space or click button to advance phase";
        debugLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        bottomHBox.AddChild(debugLabel);
    }

    /// <summary>
    /// Connect to battle manager signals
    /// </summary>
    private void ConnectSignals()
    {
        if (_battleManager != null)
        {
            _battleManager.PhaseManager.PhaseChanged += OnPhaseChanged;
        }
    }

    /// <summary>
    /// Update the UI display
    /// </summary>
    private void UpdateDisplay()
    {
        if (_battleManager == null)
            return;

        var phaseManager = _battleManager.PhaseManager;
        _roundLabel.Text = $"Round: {phaseManager.CurrentRound}";
        _phaseLabel.Text = phaseManager.GetPhaseDisplayName();
        _phaseDescriptionLabel.Text = phaseManager.GetPhaseDescription();
    }

    /// <summary>
    /// Called when phase changes
    /// </summary>
    private void OnPhaseChanged(BattlePhase newPhase)
    {
        UpdateDisplay();
    }

    /// <summary>
    /// Called when advance phase button is pressed
    /// </summary>
    private void OnAdvancePhasePressed()
    {
        _battleManager?.AdvancePhase();
    }

    public override void _Process(double delta)
    {
        // Allow spacebar to advance phase
        if (Input.IsActionJustPressed("ui_select"))
        {
            _battleManager?.AdvancePhase();
        }
    }
}
