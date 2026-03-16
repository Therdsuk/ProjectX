using Godot;

/// <summary>
/// Represents a player-controlled character on the battle board.
/// Attach to a Node2D scene (Scenes/Battle/PlayerCharacter.tscn).
///
/// Responsibilities (M1 scope):
///   - Hold runtime battle stats (HP, Mana, Energy)
///   - Track board position (grid coordinates)
///   - Manage card deck and hand
///
/// Higher milestones will add movement, animation, and card-play logic.
/// </summary>
public partial class PlayerCharacter : Node3D
{
    // -------------------------------------------------------------------------
    // Configuration (set via Inspector or BattleManager at spawn time)
    // -------------------------------------------------------------------------
    [Export] public CharacterData Data { get; set; }
    public ulong SteamId { get; set; }

    // -------------------------------------------------------------------------
    // Runtime Battle Stats
    // -------------------------------------------------------------------------

    public int CurrentHp      { get; private set; }
    public int MaxHp          { get; private set; }
    public int CurrentMana    { get; private set; }
    public int MaxMana        { get; private set; }
    public int CurrentEnergy  { get; private set; }
    public int MaxEnergy      { get; private set; }

    /// <summary>Current position on the battle grid (column, row).</summary>
    public Vector2I GridPosition { get; set; } = Vector2I.Zero;

    public CardDeck Deck  { get; } = new();
    public CardHand Hand  { get; } = new();

    private HealthBar3D _healthBar;
    private Sprite3D _sprite;

    public bool IsAlive => CurrentHp > 0;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        // Health bar
        _healthBar = new HealthBar3D();
        AddChild(_healthBar);

        // Sprite3D — shows character art. Texture assigned below or on InitialiseFromData.
        _sprite = new Sprite3D
        {
            PixelSize     = 0.1f,
            Centered      = true,
            Billboard     = BaseMaterial3D.BillboardModeEnum.Enabled,
            Position      = new Vector3(0, 1.2f, 0),
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
        };
        AddChild(_sprite);

        // If InitialiseFromData was already called before _Ready() (spawner case),
        // Data is already set but _sprite wasn't ready yet — apply texture now.
        if (Data?.Sprite != null)
            _sprite.Texture = Data.Sprite;

        // Health bar flat at ground level - avoids perspective floating issues
        _healthBar?.SetYOffset(1.8f);

        if (Data != null)
            InitialiseFromData(Data);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Initialise runtime stats from a CharacterData resource.</summary>
    public void InitialiseFromData(CharacterData data)
    {
        Data          = data;
        MaxHp         = data.BaseHp;
        CurrentHp     = MaxHp;
        MaxMana       = data.BaseMana;
        CurrentMana   = MaxMana;
        MaxEnergy     = data.BaseEnergy;
        CurrentEnergy = MaxEnergy;
        Hand.MaxHandSize = data.HandSize;

        // Initialise deck from the class starting deck
        Deck.InitialiseDeck(data.StartingDeck);

        // Apply sprite texture if one is defined for this class
        if (_sprite != null && data.Sprite != null)
            _sprite.Texture = data.Sprite;

        _healthBar?.UpdateHealth(CurrentHp, MaxHp, isEnemy: false);

        GD.Print($"[PlayerCharacter] Initialised: {data.ClassName} | HP:{MaxHp} MP:{MaxMana} EN:{MaxEnergy}");
    }

    public void ModifyHp(int amount)
    {
        // amount is negative for damage, positive for healing
        CurrentHp = Mathf.Clamp(CurrentHp + amount, 0, MaxHp);
        _healthBar?.UpdateHealth(CurrentHp, MaxHp, isEnemy: false);
        
        EventBus.Instance?.EmitSignal(EventBus.SignalName.CharacterHpChanged, Data?.Id ?? "", CurrentHp);

        if (CurrentHp <= 0)
            GD.Print($"[PlayerCharacter] {Data?.ClassName ?? "?"} has been defeated.");
    }

    /// <summary>Draw cards up to hand limit.</summary>
    public void DrawToHandLimit()
    {
        while (Hand.FreeSlotsCount > 0)
        {
            var card = Deck.Draw();
            if (card == null) break;
            Hand.AddCard(card);
        }
    }
}
