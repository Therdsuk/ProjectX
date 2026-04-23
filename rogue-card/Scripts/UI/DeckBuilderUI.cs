using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Deck Builder UI screen. Players can browse available cards and build a custom deck.
/// Instantiate as a scene or add to an existing UI.
///
/// Layout:
///   Left panel: Available cards (filtered by class)
///   Right panel: Current deck
///   Bottom: Save/Clear/Back buttons + deck status
/// </summary>
public partial class DeckBuilderUI : Control
{
    // -------------------------------------------------------------------------
    // Signals
    // -------------------------------------------------------------------------

    [Signal] public delegate void DeckSavedEventHandler(string[] cardIds);
    [Signal] public delegate void BackRequestedEventHandler();

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private string _classId = "";
    private readonly List<string> _currentDeck = new();
    private CharacterProfile _profile;

    // Node references
    private GridContainer _availableGrid;
    private VBoxContainer _deckList;
    private Label _deckCountLabel;
    private Label _statusLabel;
    private Button _saveBtn;
    private Button _clearBtn;
    private Button _backBtn;
    private OptionButton _filterDropdown;

    private static readonly PackedScene CardUIScene = GD.Load<PackedScene>("res://Scenes/UI/CardUI.tscn");

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        BuildLayout();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Open the deck builder for a specific character profile.</summary>
    public void Open(CharacterProfile profile)
    {
        _profile = profile;
        _classId = profile.ClassId;
        _currentDeck.Clear();

        // Load existing deck from profile
        if (profile.HasCustomDeck)
        {
            _currentDeck.AddRange(profile.DeckCardIds);
        }

        RefreshAvailableCards();
        RefreshDeckDisplay();
        Visible = true;
    }

    // -------------------------------------------------------------------------
    // Layout Builder
    // -------------------------------------------------------------------------

    private void BuildLayout()
    {
        // Full-screen dark background
        var bg = new ColorRect
        {
            Color = new Color(0.08f, 0.09f, 0.11f, 0.95f),
        };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Main margin
        var mainMargin = new MarginContainer();
        mainMargin.SetAnchorsPreset(LayoutPreset.FullRect);
        mainMargin.AddThemeConstantOverride("margin_left", 40);
        mainMargin.AddThemeConstantOverride("margin_right", 40);
        mainMargin.AddThemeConstantOverride("margin_top", 30);
        mainMargin.AddThemeConstantOverride("margin_bottom", 30);
        AddChild(mainMargin);

        var mainVBox = new VBoxContainer();
        mainVBox.AddThemeConstantOverride("separation", 16);
        mainMargin.AddChild(mainVBox);

        // ==================== Title Bar ====================
        var titleBar = new HBoxContainer();
        titleBar.AddThemeConstantOverride("separation", 20);
        mainVBox.AddChild(titleBar);

        var titleLabel = new Label
        {
            Text = "⚔ DECK BUILDER",
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 28);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.85f, 0.5f));
        titleBar.AddChild(titleLabel);

        // Filter dropdown
        _filterDropdown = new OptionButton
        {
            CustomMinimumSize = new Vector2(180, 0),
        };
        _filterDropdown.AddItem("All Cards", 0);
        _filterDropdown.AddItem("Battle", 1);
        _filterDropdown.AddItem("Move", 2);
        _filterDropdown.AddItem("Buff", 3);
        _filterDropdown.AddItem("Debuff", 4);
        _filterDropdown.AddItem("Setup", 5);
        _filterDropdown.ItemSelected += OnFilterChanged;
        titleBar.AddChild(_filterDropdown);

        // ==================== Main Content (Split View) ====================
        var splitContainer = new HSplitContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        splitContainer.AddThemeConstantOverride("separation", 20);
        mainVBox.AddChild(splitContainer);

        // --- Left Panel: Available Cards ---
        var leftPanel = CreatePanel("Available Cards");
        splitContainer.AddChild(leftPanel);

        var availableScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        leftPanel.AddChild(availableScroll);

        _availableGrid = new GridContainer
        {
            Columns = 3,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _availableGrid.AddThemeConstantOverride("h_separation", 10);
        _availableGrid.AddThemeConstantOverride("v_separation", 10);
        availableScroll.AddChild(_availableGrid);

        // --- Right Panel: Current Deck ---
        var rightPanel = CreatePanel("Your Deck");
        rightPanel.CustomMinimumSize = new Vector2(220, 0);
        splitContainer.AddChild(rightPanel);

        _deckCountLabel = new Label
        {
            Text = "0 / 20 cards",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _deckCountLabel.AddThemeFontSizeOverride("font_size", 14);
        _deckCountLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.72f, 0.78f));
        rightPanel.AddChild(_deckCountLabel);

        var deckScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        rightPanel.AddChild(deckScroll);

        _deckList = new VBoxContainer();
        _deckList.AddThemeConstantOverride("separation", 4);
        deckScroll.AddChild(_deckList);

        // ==================== Bottom Bar ====================
        var bottomBar = new HBoxContainer();
        bottomBar.AddThemeConstantOverride("separation", 12);
        mainVBox.AddChild(bottomBar);

        _statusLabel = new Label
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Text = "",
        };
        _statusLabel.AddThemeFontSizeOverride("font_size", 13);
        bottomBar.AddChild(_statusLabel);

        _clearBtn = new Button
        {
            Text = "Clear Deck",
            CustomMinimumSize = new Vector2(120, 40),
        };
        _clearBtn.Pressed += OnClearPressed;
        bottomBar.AddChild(_clearBtn);

        _saveBtn = new Button
        {
            Text = "Save Deck",
            CustomMinimumSize = new Vector2(120, 40),
        };
        _saveBtn.Pressed += OnSavePressed;
        bottomBar.AddChild(_saveBtn);

        _backBtn = new Button
        {
            Text = "Back",
            CustomMinimumSize = new Vector2(100, 40),
        };
        _backBtn.Pressed += OnBackPressed;
        bottomBar.AddChild(_backBtn);
    }

    private VBoxContainer CreatePanel(string title)
    {
        var panelOuter = new PanelContainer();
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.13f, 0.16f),
            BorderColor = new Color(0.3f, 0.32f, 0.38f),
            BorderWidthTop = 1, BorderWidthBottom = 1,
            BorderWidthLeft = 1, BorderWidthRight = 1,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 8, ContentMarginBottom = 8,
        };
        panelOuter.AddThemeStyleboxOverride("panel", panelStyle);

        var vbox = new VBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        vbox.AddThemeConstantOverride("separation", 8);

        var titleLabel = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        titleLabel.AddThemeFontSizeOverride("font_size", 16);
        titleLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.82f, 0.75f));
        vbox.AddChild(titleLabel);

        panelOuter.AddChild(vbox);

        // Return the VBox but we need to add panelOuter to the parent
        // Workaround: make the vbox carry the panelOuter
        return vbox;
    }

    // -------------------------------------------------------------------------
    // Display Refresh
    // -------------------------------------------------------------------------

    private void RefreshAvailableCards()
    {
        foreach (Node child in _availableGrid.GetChildren())
            child.QueueFree();

        if (CardDatabase.Instance == null) return;

        var cards = CardDatabase.Instance.GetCardsForClass(_classId);

        // Apply type filter
        int filterIdx = _filterDropdown?.Selected ?? 0;
        if (filterIdx > 0)
        {
            CardType filterType = (CardType)(filterIdx - 1);
            cards = cards.Where(c => c.CardType == filterType).ToList();
        }

        foreach (var card in cards)
        {
            var cardUI = CardUIScene.Instantiate<CardUI>();
            _availableGrid.AddChild(cardUI);
            cardUI.SetCard(card);

            // Check if can add more copies
            int copies = _currentDeck.Count(id => id == card.Id);
            if (copies >= DeckRules.MaxCopiesPerCard || _currentDeck.Count >= DeckRules.MaxDeckSize)
            {
                cardUI.SetDisabled(true);
            }

            string cardId = card.Id;
            cardUI.CardClicked += () => OnAddCard(cardId);
        }
    }

    private void RefreshDeckDisplay()
    {
        foreach (Node child in _deckList.GetChildren())
            child.QueueFree();

        // Group by card ID and show counts
        var grouped = _currentDeck.GroupBy(id => id).OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            var card = CardDatabase.Instance?.GetCard(group.Key);
            if (card == null) continue;

            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);

            var nameLabel = new Label
            {
                Text = $"{card.Name}",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            };
            nameLabel.AddThemeFontSizeOverride("font_size", 13);
            nameLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.88f, 0.82f));
            row.AddChild(nameLabel);

            var countLabel = new Label
            {
                Text = $"x{group.Count()}",
            };
            countLabel.AddThemeFontSizeOverride("font_size", 13);
            countLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1.0f));
            row.AddChild(countLabel);

            var removeBtn = new Button
            {
                Text = "✕",
                CustomMinimumSize = new Vector2(28, 28),
            };
            string cardId = group.Key;
            removeBtn.Pressed += () => OnRemoveCard(cardId);
            row.AddChild(removeBtn);

            _deckList.AddChild(row);
        }

        // Update count
        _deckCountLabel.Text = $"{_currentDeck.Count} / {DeckRules.MaxDeckSize} cards";

        // Validation status
        var (valid, error) = DeckRules.Validate(_currentDeck);
        if (valid)
        {
            _statusLabel.Text = "✓ Deck is valid";
            _statusLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.9f, 0.4f));
        }
        else if (_currentDeck.Count == 0)
        {
            _statusLabel.Text = "Add cards to build your deck";
            _statusLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.62f, 0.68f));
        }
        else
        {
            _statusLabel.Text = $"⚠ {error}";
            _statusLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.7f, 0.2f));
        }

        _saveBtn.Disabled = !valid;
    }

    // -------------------------------------------------------------------------
    // Actions
    // -------------------------------------------------------------------------

    private void OnAddCard(string cardId)
    {
        var (canAdd, reason) = DeckRules.CanAddCard(_currentDeck, cardId, _classId);
        if (!canAdd)
        {
            _statusLabel.Text = $"⚠ {reason}";
            _statusLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.5f, 0.3f));
            return;
        }

        _currentDeck.Add(cardId);
        RefreshAvailableCards();
        RefreshDeckDisplay();
    }

    private void OnRemoveCard(string cardId)
    {
        _currentDeck.Remove(cardId);
        RefreshAvailableCards();
        RefreshDeckDisplay();
    }

    private void OnClearPressed()
    {
        _currentDeck.Clear();
        RefreshAvailableCards();
        RefreshDeckDisplay();
    }

    private void OnSavePressed()
    {
        var (valid, error) = DeckRules.Validate(_currentDeck);
        if (!valid)
        {
            _statusLabel.Text = $"⚠ {error}";
            return;
        }

        // Save to profile
        if (_profile != null)
        {
            _profile.DeckCardIds = new List<string>(_currentDeck);
            CharacterSaveSystem.Save(CharacterSaveSystem.Load()); // Re-save all profiles
            GD.Print($"[DeckBuilder] Saved deck with {_currentDeck.Count} cards for {_profile.Name}.");
        }

        EmitSignal(SignalName.DeckSaved, _currentDeck.ToArray());
        _statusLabel.Text = "✓ Deck saved!";
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.3f, 0.9f, 0.4f));
    }

    private void OnBackPressed()
    {
        Visible = false;
        EmitSignal(SignalName.BackRequested);
    }

    private void OnFilterChanged(long index)
    {
        RefreshAvailableCards();
    }
}
