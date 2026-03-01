using Godot;

/// <summary>
/// Represents an enemy-controlled character on the battle board.
/// Mirrors PlayerCharacter structure; AI decision logic will be added in M4.
/// </summary>
public partial class EnemyCharacter : Node3D
{
    [Export] public CharacterData Data { get; set; }

    public int CurrentHp    { get; private set; }
    public int MaxHp        { get; private set; }
    public int CurrentMana  { get; private set; }
    public int MaxMana      { get; private set; }

    public Vector2I GridPosition { get; set; } = Vector2I.Zero;

    public CardDeck Deck { get; } = new();
    public CardHand Hand { get; } = new();

    private HealthBar3D _healthBar;

    public bool IsAlive => CurrentHp > 0;

    public override void _Ready()
    {
        _healthBar = new HealthBar3D();
        AddChild(_healthBar);

        if (Data != null)
            InitialiseFromData(Data);
    }

    public void InitialiseFromData(CharacterData data)
    {
        Data        = data;
        MaxHp       = data.BaseHp;
        CurrentHp   = MaxHp;
        MaxMana     = data.BaseMana;
        CurrentMana = MaxMana;
        Hand.MaxHandSize = data.HandSize;
        Deck.InitialiseDeck(data.StartingDeck);
        _healthBar?.UpdateHealth(CurrentHp, MaxHp, isEnemy: true);
        GD.Print($"[EnemyCharacter] Initialised: {data.ClassName} | HP:{MaxHp}");
    }

    public void ModifyHp(int amount)
    {
        CurrentHp = Mathf.Clamp(CurrentHp - amount, 0, MaxHp);
        _healthBar?.UpdateHealth(CurrentHp, MaxHp, isEnemy: true);
        if (CurrentHp <= 0)
            GD.Print($"[EnemyCharacter] {Data?.ClassName} defeated.");
    }

    /// <summary>Placeholder AI — M4 will implement real decision logic.</summary>
    public void TakeTurn()
    {
        GD.Print($"[EnemyCharacter] {Data?.ClassName} AI turn — not yet implemented.");
    }
}
