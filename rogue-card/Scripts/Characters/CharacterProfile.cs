using System;

/// <summary>
/// Represents a saved player character (name + class).
/// Serialised to JSON via CharacterSaveSystem.
/// </summary>
[Serializable]
public class CharacterProfile
{
    public string Id      { get; set; } = Guid.NewGuid().ToString();
    public string Name    { get; set; } = "Hero";
    public string ClassId { get; set; } = ClassRegistry.Warrior;
}
