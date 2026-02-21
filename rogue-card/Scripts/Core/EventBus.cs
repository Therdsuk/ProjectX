using Godot;

/// <summary>
/// Decoupled event bus for cross-system communication.
/// Register as AutoLoad singleton in Godot project settings:
///   Name: EventBus  Path: res://Scripts/Core/EventBus.cs
///
/// Usage (emit):   EventBus.Instance.EmitSignal(EventBus.SignalName.PhaseChanged, (int)newPhase);
/// Usage (listen): EventBus.Instance.PhaseChanged += OnPhaseChanged;
/// </summary>
public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }

    // -------------------------------------------------------------------------
    // Battle Signals
    // -------------------------------------------------------------------------

    /// <summary>Fired when the battle phase changes. Passes the new BattlePhase as int.</summary>
    [Signal] public delegate void PhaseChangedEventHandler(int newPhase);

    /// <summary>Fired when a card is played from hand. Passes card ID.</summary>
    [Signal] public delegate void CardPlayedEventHandler(string cardId);

    /// <summary>Fired when a character's HP changes.</summary>
    [Signal] public delegate void CharacterHpChangedEventHandler(string characterId, int newHp);

    /// <summary>Fired when the battle ends. Passes true for victory, false for defeat.</summary>
    [Signal] public delegate void BattleEndedEventHandler(bool playerWon);

    // -------------------------------------------------------------------------
    // Map Signals
    // -------------------------------------------------------------------------

    /// <summary>Fired when the player selects a map node to travel to.</summary>
    [Signal] public delegate void MapNodeSelectedEventHandler(int nodeId);

    public override void _Ready()
    {
        if (Instance != null)
        {
            QueueFree();
            return;
        }
        Instance = this;
    }
}
