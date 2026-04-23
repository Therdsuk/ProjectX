using Godot;

/// <summary>
/// Reusable card UI template. Attach to the root of CardUI.tscn.
/// 
/// Layout (defined in scene):
///   - Top-left badge: Cost
///   - Top-right badge: Speed
///   - Top half: Card artwork
///   - Bottom half: Name + description
///
/// Call SetCard(CardData) after adding to the scene tree.
/// </summary>
public partial class CardUI : PanelContainer
{
    // -------------------------------------------------------------------------
    // Signals
    // -------------------------------------------------------------------------

    [Signal] public delegate void CardClickedEventHandler();
    [Signal] public delegate void CardHoverEnteredEventHandler();
    [Signal] public delegate void CardHoverExitedEventHandler();

    // -------------------------------------------------------------------------
    // Theme Colors per CardType
    // -------------------------------------------------------------------------

    private static readonly Color MoveColor   = new Color(0.2f, 0.6f, 1.0f);   // Blue
    private static readonly Color BattleColor = new Color(0.9f, 0.25f, 0.2f);  // Red
    private static readonly Color BuffColor   = new Color(0.2f, 0.85f, 0.4f);  // Green
    private static readonly Color DebuffColor = new Color(0.7f, 0.2f, 0.8f);   // Purple
    private static readonly Color SetupColor  = new Color(0.9f, 0.7f, 0.1f);   // Gold

    // -------------------------------------------------------------------------
    // Node References (wired via Unique Names in the scene)
    // -------------------------------------------------------------------------

    private PanelContainer _cardFrame;
    private Label _costLabel;
    private Label _speedLabel;
    private TextureRect _artRect;
    private ColorRect _artPlaceholder;
    private Label _nameLabel;
    private Label _descLabel;
    private Control _costBadge;
    private Control _speedBadge;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private CardData _cardData;
    private bool _isDisabled;

    private CardData _pendingCard;
    private bool _nodesReady;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        ResolveNodes();

        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.PointingHand;

        // Apply any card data that was set before _Ready
        if (_pendingCard != null)
        {
            ApplyCardData(_pendingCard);
            _pendingCard = null;
        }
    }

    private void ResolveNodes()
    {
        if (_nodesReady) return;

        _cardFrame      = GetNode<PanelContainer>("CardFrame");
        _costLabel      = GetNode<Label>("CardFrame/VBox/ArtContainer/CostBadge/CostLabel");
        _speedLabel     = GetNode<Label>("CardFrame/VBox/ArtContainer/SpeedBadge/SpeedLabel");
        _artRect        = GetNode<TextureRect>("CardFrame/VBox/ArtContainer/CardArt");
        _artPlaceholder = GetNode<ColorRect>("CardFrame/VBox/ArtContainer/ArtPlaceholder");
        _nameLabel      = GetNode<Label>("CardFrame/VBox/InfoContainer/InfoVBox/CardName");
        _descLabel      = GetNode<Label>("CardFrame/VBox/InfoContainer/InfoVBox/CardDescription");
        _costBadge      = GetNode<Control>("CardFrame/VBox/ArtContainer/CostBadge");
        _speedBadge     = GetNode<Control>("CardFrame/VBox/ArtContainer/SpeedBadge");

        _nodesReady = _costLabel != null;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Populate the card UI with data from a CardData resource.</summary>
    public void SetCard(CardData card)
    {
        _cardData = card;
        if (card == null) return;

        // If nodes aren't resolved yet (_Ready hasn't fired), defer
        if (!_nodesReady)
        {
            _pendingCard = card;
            return;
        }

        ApplyCardData(card);
    }

    private void ApplyCardData(CardData card)
    {
        _costLabel.Text = card.Cost.ToString();
        _speedLabel.Text = GetSpeedLabel(card.Speed);
        _nameLabel.Text = card.Name;
        _descLabel.Text = card.Description;

        // Card art
        if (card.Art != null)
        {
            _artRect.Texture = card.Art;
            _artRect.Visible = true;
            _artPlaceholder.Visible = false;
        }
        else
        {
            _artRect.Visible = false;
            _artPlaceholder.Visible = true;
            _artPlaceholder.Color = GetTypeColor(card.CardType).Darkened(0.3f);
        }

        // Color the frame border based on card type
        Color typeColor = GetTypeColor(card.CardType);
        if (_cardFrame.GetThemeStylebox("panel") is StyleBoxFlat frameStyle)
        {
            var newStyle = (StyleBoxFlat)frameStyle.Duplicate();
            newStyle.BorderColor = typeColor;
            _cardFrame.AddThemeStyleboxOverride("panel", newStyle);
        }

        // Color badge labels
        SetLabelBadgeColor(_costLabel, typeColor);
        SetLabelBadgeColor(_speedLabel, typeColor);
    }

    public CardData GetCard() => _cardData;

    public void SetDisabled(bool disabled)
    {
        _isDisabled = disabled;
        Modulate = disabled ? new Color(0.5f, 0.5f, 0.5f, 0.7f) : Colors.White;
        MouseDefaultCursorShape = disabled ? CursorShape.Arrow : CursorShape.PointingHand;
    }

    public void SetSelected(bool selected)
    {
        if (_cardFrame == null) return;
        if (_cardFrame.GetThemeStylebox("panel") is StyleBoxFlat baseStyle)
        {
            var style = (StyleBoxFlat)baseStyle.Duplicate();
            int bw = selected ? 3 : 2;
            style.BorderWidthTop = bw;
            style.BorderWidthBottom = bw;
            style.BorderWidthLeft = bw;
            style.BorderWidthRight = bw;

            if (selected)
            {
                style.BorderColor = new Color(1.0f, 0.9f, 0.3f); // Gold
            }
            else if (_cardData != null)
            {
                style.BorderColor = GetTypeColor(_cardData.CardType);
            }
            _cardFrame.AddThemeStyleboxOverride("panel", style);
        }
    }

    // -------------------------------------------------------------------------
    // Input — handle clicks and hover on the root PanelContainer
    // -------------------------------------------------------------------------

    public override void _GuiInput(InputEvent @event)
    {
        if (_isDisabled) return;

        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            EmitSignal(SignalName.CardClicked);
            AcceptEvent();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseEnter && !_isDisabled)
        {
            PivotOffset = Size / 2f;
            var tween = CreateTween();
            tween.TweenProperty(this, "scale", new Vector2(1.08f, 1.08f), 0.1f);
            ZIndex = 10;
            EmitSignal(SignalName.CardHoverEntered);
        }
        else if (what == NotificationMouseExit)
        {
            var tween = CreateTween();
            tween.TweenProperty(this, "scale", Vector2.One, 0.1f);
            ZIndex = 0;
            EmitSignal(SignalName.CardHoverExited);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SetLabelBadgeColor(Label label, Color color)
    {
        if (label == null) return;

        // Font color = bright version of type color
        label.AddThemeColorOverride("font_color", color.Lightened(0.3f));

        // Semi-transparent background behind the text
        var bg = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.06f, 0.08f, 0.75f),
            BorderColor = color,
            BorderWidthTop = 1, BorderWidthBottom = 1,
            BorderWidthLeft = 1, BorderWidthRight = 1,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            ContentMarginLeft = 4, ContentMarginRight = 4,
            ContentMarginTop = 2, ContentMarginBottom = 2,
        };
        label.AddThemeStyleboxOverride("normal", bg);
    }

    private static Color GetTypeColor(CardType type)
    {
        return type switch
        {
            CardType.Move   => MoveColor,
            CardType.Battle => BattleColor,
            CardType.Buff   => BuffColor,
            CardType.Debuff => DebuffColor,
            CardType.Setup  => SetupColor,
            _               => new Color(0.5f, 0.5f, 0.5f),
        };
    }

    private static string GetSpeedLabel(CardSpeed speed)
    {
        return speed switch
        {
            CardSpeed.Burst => "B",
            CardSpeed.Fast  => "F",
            CardSpeed.Slow  => "S",
            _               => "?",
        };
    }
}
