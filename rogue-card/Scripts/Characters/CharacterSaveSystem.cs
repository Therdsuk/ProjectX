using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Handles loading and saving the player's character roster to disk.
/// Saved at: user://characters.json
/// </summary>
public static class CharacterSaveSystem
{
    private const string SavePath = "user://characters.json";

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented              = true,
        DefaultIgnoreCondition     = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Load all saved characters from disk. Returns empty list if none exist.</summary>
    public static List<CharacterProfile> Load()
    {
        var path = ProjectSettings.GlobalizePath(SavePath);
        if (!System.IO.File.Exists(path))
            return new List<CharacterProfile>();

        try
        {
            string json = System.IO.File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<CharacterProfile>>(json, _jsonOptions)
                   ?? new List<CharacterProfile>();
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CharacterSaveSystem] Failed to load characters: {e.Message}");
            return new List<CharacterProfile>();
        }
    }

    /// <summary>Save the full character list to disk.</summary>
    public static void Save(List<CharacterProfile> characters)
    {
        try
        {
            string path = ProjectSettings.GlobalizePath(SavePath);
            string json = JsonSerializer.Serialize(characters, _jsonOptions);
            System.IO.File.WriteAllText(path, json);
            GD.Print($"[CharacterSaveSystem] Saved {characters.Count} character(s).");
        }
        catch (Exception e)
        {
            GD.PrintErr($"[CharacterSaveSystem] Failed to save characters: {e.Message}");
        }
    }

    /// <summary>Add a new character and persist immediately.</summary>
    public static void AddCharacter(CharacterProfile profile)
    {
        var characters = Load();
        characters.Add(profile);
        Save(characters);
    }

    /// <summary>Delete a character by Id and persist immediately.</summary>
    public static void DeleteCharacter(string id)
    {
        var characters = Load();
        characters.RemoveAll(c => c.Id == id);
        Save(characters);
    }
}
