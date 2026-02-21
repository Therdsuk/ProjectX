using Godot;
using System.Collections.Generic;

/// <summary>
/// Generates and manages the battle grid (board).
///
/// M1 scope:
///   - Creates a plain grid of cells (all FieldType.Normal)
///   - Tracks which unit occupies each cell
///   - Provides helper queries (is cell occupied, world-pos ↔ grid-pos)
///
/// Attach to a Node2D in BattleScene.tscn called "BattleBoard".
/// </summary>
public partial class BattleBoard : Node2D
{
    // -------------------------------------------------------------------------
    // Configuration (editable in Inspector)
    // -------------------------------------------------------------------------

    [Export] public int Columns    { get; set; } = 8;
    [Export] public int Rows       { get; set; } = 6;
    [Export] public int CellSize   { get; set; } = 80;  // pixels

    // -------------------------------------------------------------------------
    // Internal State
    // -------------------------------------------------------------------------

    /// <summary>2-D array of cell data indexed by [col, row].</summary>
    private FieldCell[,] _cells;

    /// <summary>Mapping from grid position → occupying character node (null if empty).</summary>
    private readonly Dictionary<Vector2I, Node2D> _occupants = new();

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        GenerateBoard();
    }

    // -------------------------------------------------------------------------
    // Board Generation
    // -------------------------------------------------------------------------

    /// <summary>Creates the plain (Normal) grid. Called at battle start.</summary>
    public void GenerateBoard()
    {
        _cells = new FieldCell[Columns, Rows];
        _occupants.Clear();

        for (int col = 0; col < Columns; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                _cells[col, row] = new FieldCell(new Vector2I(col, row), FieldType.Normal);
                _occupants[new Vector2I(col, row)] = null;
            }
        }

        GD.Print($"[BattleBoard] Generated {Columns}×{Rows} plain board.");
        DrawDebugGrid(); // M1: draw outline cells so we can see the board
    }

    // -------------------------------------------------------------------------
    // Coordinate Helpers
    // -------------------------------------------------------------------------

    /// <summary>Convert a grid position to world (pixel) position (top-left of cell).</summary>
    public Vector2 GridToWorld(Vector2I grid)
    {
        return new Vector2(grid.X * CellSize, grid.Y * CellSize);
    }

    /// <summary>Convert a world position to the nearest grid coordinate.</summary>
    public Vector2I WorldToGrid(Vector2 world)
    {
        return new Vector2I((int)(world.X / CellSize), (int)(world.Y / CellSize));
    }

    /// <summary>World position at the centre of a cell.</summary>
    public Vector2 CellCentre(Vector2I grid)
    {
        return GridToWorld(grid) + new Vector2(CellSize / 2f, CellSize / 2f);
    }

    // -------------------------------------------------------------------------
    // Occupancy
    // -------------------------------------------------------------------------

    public bool IsInBounds(Vector2I grid)
        => grid.X >= 0 && grid.X < Columns && grid.Y >= 0 && grid.Y < Rows;

    public bool IsOccupied(Vector2I grid)
        => IsInBounds(grid) && _occupants[grid] != null;

    public Node2D GetOccupant(Vector2I grid)
        => IsInBounds(grid) ? _occupants[grid] : null;

    /// <summary>Place a unit on the board at the given grid cell.</summary>
    public bool PlaceUnit(Node2D unit, Vector2I grid)
    {
        if (!IsInBounds(grid))
        {
            GD.PrintErr($"[BattleBoard] Out-of-bounds placement attempted at {grid}");
            return false;
        }
        if (IsOccupied(grid))
        {
            GD.PrintErr($"[BattleBoard] Cell {grid} already occupied.");
            return false;
        }

        _occupants[grid] = unit;
        unit.Position = CellCentre(grid);
        GD.Print($"[BattleBoard] Placed {unit.Name} at {grid}");
        return true;
    }

    /// <summary>Remove a unit from its current cell.</summary>
    public void RemoveUnit(Vector2I grid)
    {
        if (IsInBounds(grid))
            _occupants[grid] = null;
    }

    /// <summary>Get the FieldCell data for a grid position.</summary>
    public FieldCell GetCell(Vector2I grid)
        => IsInBounds(grid) ? _cells[grid.X, grid.Y] : null;

    // -------------------------------------------------------------------------
    // M1 Debug Visualisation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Draws a grid of ColorRect nodes so the board is visible in the editor/game
    /// without any art assets. Each cell gets a solid fill + outline colour.
    /// Remove or replace this in later milestones when real tile art is added.
    /// </summary>
    private void DrawDebugGrid()
    {
        for (int col = 0; col < Columns; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                // Checkerboard colouring
                bool even = (col + row) % 2 == 0;
                Color fill = even
                    ? new Color(0.20f, 0.22f, 0.28f)   // dark slate
                    : new Color(0.25f, 0.27f, 0.35f);  // slightly lighter

                var rect = new ColorRect
                {
                    Size     = new Vector2(CellSize - 2, CellSize - 2),
                    Position = GridToWorld(new Vector2I(col, row)) + new Vector2(1, 1),
                    Color    = fill,
                    Name     = $"Cell_{col}_{row}"
                };
                AddChild(rect);
            }
        }
    }
}
