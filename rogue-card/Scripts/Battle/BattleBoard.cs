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
public partial class BattleBoard : Node3D
{
    // -------------------------------------------------------------------------
    // Configuration (editable in Inspector)
    // -------------------------------------------------------------------------

    [Export] public int Columns    { get; set; } = 8;
    [Export] public int Rows       { get; set; } = 6;
    [Export] public float CellSize { get; set; } = 2.0f;  // meters in 3D

    // -------------------------------------------------------------------------
    // Internal State
    // -------------------------------------------------------------------------

    /// <summary>2-D array of cell data indexed by [col, row].</summary>
    private FieldCell[,] _cells;

    /// <summary>2-D array of 3D meshes for visual highlighting.</summary>
    private MeshInstance3D[,] _cellMeshes;

    /// <summary>Mapping from grid position → occupying character node (null if empty).</summary>
    private readonly Dictionary<Vector2I, Node3D> _occupants = new();

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
        _cellMeshes = new MeshInstance3D[Columns, Rows];
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

    /// <summary>Convert a grid position to world pos (top-left of cell usually, but keeping logic consistent).</summary>
    public Vector3 GridToWorld(Vector2I grid)
    {
        return new Vector3(grid.X * CellSize, 0, grid.Y * CellSize);
    }

    /// <summary>Convert a world position to the nearest grid coordinate.</summary>
    public Vector2I WorldToGrid(Vector3 world)
    {
        return new Vector2I((int)(world.X / CellSize), (int)(world.Z / CellSize));
    }

    /// <summary>World position at the centre of a cell.</summary>
    public Vector3 CellCentre(Vector2I grid)
    {
        return GridToWorld(grid) + new Vector3(CellSize / 2f, 0, CellSize / 2f);
    }

    // -------------------------------------------------------------------------
    // Occupancy
    // -------------------------------------------------------------------------

    public bool IsInBounds(Vector2I grid)
        => grid.X >= 0 && grid.X < Columns && grid.Y >= 0 && grid.Y < Rows;

    public bool IsOccupied(Vector2I grid)
        => IsInBounds(grid) && _occupants[grid] != null;

    public Node3D GetOccupant(Vector2I grid)
        => IsInBounds(grid) ? _occupants[grid] : null;

    /// <summary>Place a unit on the board at the given grid cell.</summary>
    public bool PlaceUnit(Node3D unit, Vector2I grid)
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

                // Give each cell a unique material instance so changing one doesn't change all
                var material = new StandardMaterial3D();
                material.AlbedoColor = fill;
                
                var plane = new PlaneMesh { Size = new Vector2(CellSize - 0.1f, CellSize - 0.1f) };
                
                var meshInstance = new MeshInstance3D
                {
                    Mesh = plane,
                    MaterialOverride = material,
                    Position = CellCentre(new Vector2I(col, row)),
                    Name = $"Cell_{col}_{row}"
                };
                AddChild(meshInstance);
                _cellMeshes[col, row] = meshInstance;

                // Add collision so we can detect mouse clicks via Raycast
                var staticBody = new StaticBody3D();
                meshInstance.AddChild(staticBody);
                
                var collisionShape = new CollisionShape3D();
                var boxShape = new BoxShape3D { Size = new Vector3(CellSize, 0.1f, CellSize) };
                collisionShape.Shape = boxShape;
                staticBody.AddChild(collisionShape);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Gameplay Helpers (Highlighter & Move)
    // -------------------------------------------------------------------------

    /// <summary>Change a cell's color to show it's valid for an action.</summary>
    public void HighlightCell(Vector2I grid, Color color)
    {
        if (!IsInBounds(grid)) return;
        var mat = _cellMeshes[grid.X, grid.Y].MaterialOverride as StandardMaterial3D;
        if (mat != null) mat.AlbedoColor = color;
    }

    /// <summary>Reset all highlights back to their default checkerboard colors.</summary>
    public void ClearHighlights()
    {
        for (int col = 0; col < Columns; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                bool even = (col + row) % 2 == 0;
                Color fill = even
                    ? new Color(0.20f, 0.22f, 0.28f)
                    : new Color(0.25f, 0.27f, 0.35f);

                HighlightCell(new Vector2I(col, row), fill);
            }
        }
    }

    /// <summary>Calculates and returns a list of valid grid cells covered by the specified AoE.</summary>
    public List<Vector2I> GetCellsInAoE(Vector2I targetCell, AreaOfEffect aoe, Vector2I playerPos)
    {
        var cells = new List<Vector2I>();
        
        switch (aoe)
        {
            case AreaOfEffect.SingleNode:
                if (IsInBounds(targetCell)) cells.Add(targetCell);
                break;
                
            case AreaOfEffect.Cross:
                Vector2I[] crossOffsets = { Vector2I.Zero, Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };
                foreach (var offset in crossOffsets)
                {
                    var pos = targetCell + offset;
                    if (IsInBounds(pos)) cells.Add(pos);
                }
                break;
                
            case AreaOfEffect.Square3x3:
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        var pos = targetCell + new Vector2I(x, y);
                        if (IsInBounds(pos)) cells.Add(pos);
                    }
                }
                break;
                
            case AreaOfEffect.LineForward:
                // Direction from player to target for the line beam
                Vector2I dir = new Vector2I(Mathf.Sign(targetCell.X - playerPos.X), Mathf.Sign(targetCell.Y - playerPos.Y));
                
                // If diagonal or same cell, default to horizontal beam
                if (dir.X != 0 && dir.Y != 0) dir.Y = 0; 
                if (dir == Vector2I.Zero) dir = Vector2I.Right;
                
                for (int i = 0; i < 5; i++) // Example max beam length
                {
                    var pos = targetCell + dir * i;
                    if (IsInBounds(pos)) cells.Add(pos);
                }
                break;
        }
        
        return cells;
    }

    /// <summary>Move a unit cleanly from one cell to another, using a Tween.</summary>
    public void MoveUnit(Node3D unit, Vector2I from, Vector2I to)
    {
        if (!IsInBounds(to) || IsOccupied(to)) return;

        // Update Backend
        if (IsInBounds(from) && _occupants[from] == unit)
        {
             _occupants[from] = null;
        }
        _occupants[to] = unit;

        // Update Physics (Slide smoothly over 0.2 seconds)
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(unit, "position", CellCentre(to), 0.2f)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.Out);
    }
}
