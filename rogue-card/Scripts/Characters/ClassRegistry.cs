using Godot;

/// <summary>
/// Central registry of all player classes.
/// Returns a fully configured CharacterData for a given class Id.
/// The Sprite field will be null until the user places matching art in
/// Assets/Art/Characters/ and assigns it here.
/// </summary>
public static class ClassRegistry
{
    public const string Warrior = "warrior";
    public const string Archer  = "archer";
    public const string Wizard  = "wizard";
    public const string Healer  = "healer";

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Returns a fresh CharacterData for the given class Id.
    /// Returns Warrior as default if the Id is unknown.</summary>
    public static CharacterData Get(string classId)
    {
        return classId switch
        {
            Archer  => MakeArcher(),
            Wizard  => MakeWizard(),
            Healer  => MakeHealer(),
            _       => MakeWarrior(),
        };
    }

    /// <summary>Try to load a texture from Assets/Art/Characters/. Returns null if the file doesn't exist yet.</summary>
    private static Texture2D LoadSprite(string filename)
    {
        string path = $"res://Assets/Art/Characters/{filename}";
        if (Godot.ResourceLoader.Exists(path))
            return Godot.ResourceLoader.Load<Texture2D>(path);
        return null;
    }

    /// <summary>All available class Ids in display order.</summary>
    public static readonly string[] AllClasses = { Warrior, Archer, Wizard, Healer };

    // -------------------------------------------------------------------------
    // Class Definitions
    // -------------------------------------------------------------------------

    private static CharacterData MakeWarrior() => new CharacterData
    {
        Id               = Warrior,
        ClassName        = "Warrior",
        ClassDescription = "A heavily armoured melee fighter. High HP and Defence, shorter move range.",
        Sprite           = LoadSprite("warrior.png"),
        BaseHp           = 150,
        BaseMana         = 2,
        BaseEnergy       = 30,
        BaseSpeed        = 4,
        BaseAttack       = 15,
        BaseDefense      = 12,
        MoveRange        = 2,
        HandSize         = 5,
    };

    private static CharacterData MakeArcher() => new CharacterData
    {
        Id               = Archer,
        ClassName        = "Archer",
        ClassDescription = "A nimble ranged attacker. High Speed and Move Range, lower HP.",
        Sprite           = LoadSprite("archer.png"),
        BaseHp           = 90,
        BaseMana         = 3,
        BaseEnergy       = 35,
        BaseSpeed        = 8,
        BaseAttack       = 12,
        BaseDefense      = 5,
        MoveRange        = 4,
        HandSize         = 5,
    };

    private static CharacterData MakeWizard() => new CharacterData
    {
        Id               = Wizard,
        ClassName        = "Wizard",
        ClassDescription = "A powerful spell-caster with high Mana. Fragile but devastating.",
        Sprite           = LoadSprite("wizard.png"),
        BaseHp           = 70,
        BaseMana         = 6,
        BaseEnergy       = 25,
        BaseSpeed        = 5,
        BaseAttack       = 20,
        BaseDefense      = 3,
        MoveRange        = 3,
        HandSize         = 6,
    };

    private static CharacterData MakeHealer() => new CharacterData
    {
        Id               = Healer,
        ClassName        = "Healer",
        ClassDescription = "A support class that sustains the party. Moderate stats, high Mana.",
        Sprite           = LoadSprite("healer.png"),
        BaseHp           = 100,
        BaseMana         = 5,
        BaseEnergy       = 30,
        BaseSpeed        = 5,
        BaseAttack       = 6,
        BaseDefense      = 7,
        MoveRange        = 3,
        HandSize         = 5,
    };
}
