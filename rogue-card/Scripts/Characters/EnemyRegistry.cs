using Godot;

/// <summary>
/// Registry of all enemy types.
/// Each enemy is defined as a CharacterData .tres file under Resources/Enemies/.
/// The file name must match the enum name exactly, e.g. Goblin → Resources/Enemies/Goblin.tres
///
/// To add a new enemy:
///   1. Add its name to the EnemyType enum below
///   2. Create Resources/Enemies/YourEnemy.tres in the Godot Editor
///   3. Fill in stats and StartingDeck in the Inspector — no code changes needed
/// </summary>
public static class EnemyRegistry
{
    // -------------------------------------------------------------------------
    // Enemy type enum — shown as a dropdown in Inspector for all [Export] fields
    // -------------------------------------------------------------------------
    public enum EnemyType { Goblin, Slime, Orc }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads and returns a fresh duplicate of the CharacterData for the given enemy type.
    /// Each call returns an independent copy so runtime stat changes don't bleed between enemies.
    /// </summary>
    public static CharacterData Get(EnemyType type)
    {
        string path = $"res://Resources/Enemies/{type}.tres";

        if (!Godot.ResourceLoader.Exists(path))
        {
            GD.PushWarning($"[EnemyRegistry] Missing .tres for '{type}' at {path}. Using blank stub — create the file in the Godot Editor.");
            return MakeStub(type.ToString());
        }

        var data = Godot.ResourceLoader.Load<CharacterData>(path, cacheMode: ResourceLoader.CacheMode.Reuse);
        return (CharacterData)data.Duplicate(true); // Deep duplicate ensures unique sub-resources (cards) are copied too
    }

    // -------------------------------------------------------------------------
    // Fallback stub — used only when the .tres file doesn't exist yet
    // -------------------------------------------------------------------------

    private static CharacterData MakeStub(string name) => new CharacterData
    {
        Id               = name,
        ClassName        = name,
        ClassDescription = $"[STUB] Create Resources/Enemies/{name}.tres in the editor.",
        BaseHp           = 50,
        BaseMana         = 1,
        MoveRange        = 2,
        HandSize         = 3,
    };
}
