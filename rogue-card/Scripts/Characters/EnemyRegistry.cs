using Godot;

/// <summary>
/// Registry of all enemy types. Mirrors the design of ClassRegistry but
/// for enemy characters. Each entry defines stats and points to a sprite
/// in Assets/Art/Enemies/.
///
/// Add new enemy types here as the game grows.
/// </summary>
public static class EnemyRegistry
{
    // -------------------------------------------------------------------------
    // Enemy type constants
    // -------------------------------------------------------------------------
    public const string Goblin  = "goblin";
    public const string Slime   = "slime";
    public const string Orc     = "orc";

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Returns a fresh CharacterData for the given enemy type Id.
    /// Falls back to Goblin if the Id is unknown.</summary>
    public static CharacterData Get(string typeId)
    {
        return typeId switch
        {
            Slime  => MakeSlime(),
            Orc    => MakeOrc(),
            _      => MakeGoblin(),   // "goblin" or anything unknown
        };
    }

    // -------------------------------------------------------------------------
    // Enemy Definitions
    // -------------------------------------------------------------------------

    private static CharacterData MakeGoblin() => new CharacterData
    {
        Id               = Goblin,
        ClassName        = "Goblin",
        ClassDescription = "A weak but fast enemy. Watch out for swarms.",
        Sprite           = LoadSprite("goblin.png"),
        BaseHp           = 50,
        BaseMana         = 1,
        BaseEnergy       = 20,
        BaseSpeed        = 6,
        BaseAttack       = 8,
        BaseDefense      = 3,
        MoveRange        = 3,
        HandSize         = 4,
    };

    private static CharacterData MakeSlime() => new CharacterData
    {
        Id               = Slime,
        ClassName        = "Slime",
        ClassDescription = "Slow and tanky. Hard to kill.",
        Sprite           = LoadSprite("slime.png"),
        BaseHp           = 100,
        BaseMana         = 1,
        BaseEnergy       = 10,
        BaseSpeed        = 2,
        BaseAttack       = 5,
        BaseDefense      = 8,
        MoveRange        = 1,
        HandSize         = 3,
    };

    private static CharacterData MakeOrc() => new CharacterData
    {
        Id               = Orc,
        ClassName        = "Orc",
        ClassDescription = "A heavy melee brute with high attack.",
        Sprite           = LoadSprite("orc.png"),
        BaseHp           = 120,
        BaseMana         = 2,
        BaseEnergy       = 25,
        BaseSpeed        = 3,
        BaseAttack       = 18,
        BaseDefense      = 6,
        MoveRange        = 2,
        HandSize         = 4,
    };

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Loads a sprite from Assets/Art/Enemies/. Returns null if file doesn't exist yet.</summary>
    private static Texture2D LoadSprite(string filename)
    {
        string path = $"res://Assets/Art/Enemies/{filename}";
        if (Godot.ResourceLoader.Exists(path))
            return Godot.ResourceLoader.Load<Texture2D>(path);
        return null;
    }
}
