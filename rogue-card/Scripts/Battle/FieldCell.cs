using Godot;

/// <summary>
/// Represents a single cell on the battle grid.
/// Holds the cell's field type and a reference to any unit occupying it.
///
/// Not a Node — instantiated as a plain C# class by BattleBoard.
/// </summary>
public class FieldCell
{
    /// <summary>Grid position of this cell (column, row).</summary>
    public Vector2I GridPosition { get; }

    /// <summary>The terrain/field type of this cell.</summary>
    public FieldType FieldType { get; set; }

    public FieldCell(Vector2I gridPosition, FieldType fieldType = FieldType.Normal)
    {
        GridPosition = gridPosition;
        FieldType    = fieldType;
    }

    // -------------------------------------------------------------------------
    // Field-Type Effect Helpers (M3 — stubs for now)
    // -------------------------------------------------------------------------

    /// <summary>Return a damage multiplier for the given damage type on this field.</summary>
    public float GetDamageMultiplier(string damageType)
    {
        // TODO (M3): implement per-field interactions
        // Example: Water + Fire → 0.5f, Water + Thunder → splash to neighbours
        return 1.0f;
    }

    /// <summary>Passive damage dealt to a unit standing here at the start of a round.</summary>
    public int GetPassiveDamage()
    {
        return FieldType switch
        {
            FieldType.Lava => 5,  // TODO (M3): tune value
            _              => 0
        };
    }
}
