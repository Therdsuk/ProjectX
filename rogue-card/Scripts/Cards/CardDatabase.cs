using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Singleton that loads and indexes all CardData resources from Resources/Cards/.
/// Register as an Autoload in Project Settings.
///
/// Usage:
///   CardDatabase.Instance.GetCard("strike")
///   CardDatabase.Instance.GetAllCards()
///   CardDatabase.Instance.GetCardsForClass("warrior")
/// </summary>
public partial class CardDatabase : Node
{
    public static CardDatabase Instance { get; private set; }

    private readonly Dictionary<string, CardData> _cards = new();

    private const string CardsDirectory = "res://Resources/Cards/";

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        Instance = this;
        LoadAllCards();
        GD.Print($"[CardDatabase] Loaded {_cards.Count} card(s) from {CardsDirectory}");
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Get a card by its Id. Returns null if not found.</summary>
    public CardData GetCard(string id)
    {
        _cards.TryGetValue(id, out var card);
        return card;
    }

    /// <summary>Get all cards in the database.</summary>
    public IReadOnlyList<CardData> GetAllCards()
    {
        return _cards.Values.ToList();
    }

    /// <summary>Get all card IDs.</summary>
    public IReadOnlyList<string> GetAllCardIds()
    {
        return _cards.Keys.ToList();
    }

    /// <summary>Get cards available for a specific class (includes universal cards).</summary>
    public IReadOnlyList<CardData> GetCardsForClass(string classId)
    {
        return _cards.Values
            .Where(c => c.AllowedClasses == null || c.AllowedClasses.Length == 0 || c.AllowedClasses.Contains(classId))
            .ToList();
    }

    /// <summary>Get cards filtered by type.</summary>
    public IReadOnlyList<CardData> GetCardsByType(CardType type)
    {
        return _cards.Values.Where(c => c.CardType == type).ToList();
    }

    /// <summary>Check if a card ID exists in the database.</summary>
    public bool HasCard(string id) => _cards.ContainsKey(id);

    /// <summary>Total number of cards in the database.</summary>
    public int Count => _cards.Count;

    // -------------------------------------------------------------------------
    // Loading
    // -------------------------------------------------------------------------

    private void LoadAllCards()
    {
        _cards.Clear();

        using var dir = DirAccess.Open(CardsDirectory);
        if (dir == null)
        {
            GD.PrintErr($"[CardDatabase] Cannot open directory: {CardsDirectory}");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();

        while (!string.IsNullOrEmpty(fileName))
        {
            // Godot may import .tres as .tres.remap in exported builds
            if (fileName.EndsWith(".tres") || fileName.EndsWith(".tres.remap"))
            {
                string resourcePath = CardsDirectory + fileName.Replace(".remap", "");
                var resource = ResourceLoader.Load<CardData>(resourcePath);

                if (resource != null && !string.IsNullOrEmpty(resource.Id))
                {
                    if (_cards.ContainsKey(resource.Id))
                    {
                        GD.PushWarning($"[CardDatabase] Duplicate card Id '{resource.Id}' in {fileName} — skipping.");
                    }
                    else
                    {
                        _cards[resource.Id] = resource;
                    }
                }
                else if (resource != null && string.IsNullOrEmpty(resource.Id))
                {
                    GD.PushWarning($"[CardDatabase] Card in {fileName} has no Id — skipping.");
                }
            }

            fileName = dir.GetNext();
        }

        dir.ListDirEnd();
    }

    /// <summary>Force reload all cards from disk (useful after editing .tres files at runtime).</summary>
    public void Reload()
    {
        LoadAllCards();
        GD.Print($"[CardDatabase] Reloaded {_cards.Count} card(s).");
    }
}
