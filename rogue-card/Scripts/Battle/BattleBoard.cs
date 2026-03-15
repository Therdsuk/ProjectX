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

    [ExportGroup("Generation Settings")]
    [Export] public float ObstacleDensity { get; set; } = 0.15f; // Chance for Rock/Forest
    [Export] public float WaterDensity    { get; set; } = 0.05f;

    [ExportCategory("Visuals")]
    [Export] public Texture2D WaterTexture;
    [Export] public Texture2D GrassTexture;
    [Export] public Texture2D RockTexture;
    [Export] public PackedScene TreeScene;
    [Export] public PackedScene RockScene;
    [Export] public PackedScene GroundScene;
    [Export] public PackedScene RampScene;
    [Export] public float GroundThickness { get; set; } = 0.5f;
    [Export] public float ElevationStep { get; set; } = 0.5f;
    [Export] public float HighGroundDensity { get; set; } = 0.1f;

    // -------------------------------------------------------------------------
    // Internal State
    // -------------------------------------------------------------------------

    /// <summary>2-D array of cell data indexed by [col, row].</summary>
    private FieldCell[,] _cells;

    /// <summary>2-D array of 3D meshes for visual highlighting.</summary>
    private MeshInstance3D[,] _cellMeshes;

    /// <summary>A second layer of meshes for dynamic highlights (overlay).</summary>
    private MeshInstance3D[,] _highlightMeshes;

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

    /// <summary>Creates a procedurally generated board.</summary>
    public void GenerateBoard()
    {
        // Clear existing visuals
        foreach (var child in GetChildren())
        {
            if (child is MeshInstance3D || child is Sprite3D) child.QueueFree();
        }

        _cells = new FieldCell[Columns, Rows];
        _cellMeshes = new MeshInstance3D[Columns, Rows];
        _highlightMeshes = new MeshInstance3D[Columns, Rows];
        _occupants.Clear();

        // 1. Noise Setup
        var noise = new FastNoiseLite();
        noise.Seed = (int)GD.Randi();
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        noise.Frequency = 0.15f; // Scale of hills/lakes

        var obstacleNoise = new FastNoiseLite();
        obstacleNoise.Seed = (int)GD.Randi();
        obstacleNoise.Frequency = 0.25f; // More granular for trees/rocks

        // 2. Base Terrain & Elevation Pass
        for (int col = 0; col < Columns; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                var pos = new Vector2I(col, row);
                float val = noise.GetNoise2D(col, row);
                
                FieldType type = FieldType.Normal;
                int elevation = 0;

                // Thresholds for biomes (multi-level steps)
                if (val < -0.25f) 
                {
                    type = FieldType.Water;
                }
                else if (val > 0.6f)  elevation = 3; 
                else if (val > 0.45f) elevation = 2;
                else if (val > 0.25f) elevation = 1;

                // Obstacle pass (Forest/Rock) - only on lower levels
                if (type == FieldType.Normal && elevation < 2)
                {
                    float obsVal = obstacleNoise.GetNoise2D(col, row);
                    if (obsVal > 0.5f) type = FieldType.Forest;
                    else if (obsVal < -0.6f) type = FieldType.Rock;
                }

                _cells[col, row] = new FieldCell(pos, type)
                {
                    Elevation = elevation
                };
                _occupants[pos] = null;
            }
        }

        // --- Pass 2: Smoothing (Fill Holes) ---
        for (int col = 1; col < Columns - 1; col++)
        {
            for (int row = 1; row < Rows - 1; row++)
            {
                var cell = _cells[col, row];
                if (cell.Elevation >= 1) continue;
                int highCount = 0;
                Vector2I[] neighbors = { new(col + 1, row), new(col - 1, row), new(col, row + 1), new(col, row - 1) };
                foreach (var n in neighbors) if (_cells[n.X, n.Y].Elevation >= 1) highCount++;
                if (highCount >= 3) cell.Elevation = 1;
            }
        }

        GD.Print($"[BattleBoard] Generated {Columns}×{Rows} seamless procedural board.");
        DrawBoardVisuals();
    }

    // -------------------------------------------------------------------------
    // Coordinate Helpers
    // -------------------------------------------------------------------------

    /// <summary>Convert a grid position to world pos (top-left of cell usually, but keeping logic consistent).</summary>
    public Vector3 GridToWorld(Vector2I grid)
    {
        float y = 0;
        if (IsInBounds(grid))
        {
            float h00 = GetVertexHeight(grid.X, grid.Y);
            float h10 = GetVertexHeight(grid.X + 1, grid.Y);
            float h01 = GetVertexHeight(grid.X, grid.Y + 1);
            float h11 = GetVertexHeight(grid.X + 1, grid.Y + 1);
            y = (h00 + h10 + h01 + h11) / 4f; // Interpolated center height
        }
        return new Vector3(grid.X * CellSize, y, grid.Y * CellSize);
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
    {
        if (!IsInBounds(grid)) return true;
        
        // Block rocks
        if (_cells[grid.X, grid.Y].FieldType == FieldType.Rock) return true;

        return _occupants[grid] != null;
    }

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
    /// Draws the grid cells and spawns 3D decorations based on terrain.
    /// </summary>
    private void DrawBoardVisuals()
    {
        for (int col = 0; col < Columns; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                var cell = _cells[col, row];
                var center = CellCentre(new Vector2I(col, row));
                var material = new StandardMaterial3D();
                
                Texture2D tex = GetTextureForType(cell.FieldType);
                if (tex != null)
                {
                    material.AlbedoTexture = tex;
                    material.AlbedoColor = Colors.White;
                }
                else
                {
                    material.AlbedoColor = GetColorForType(cell.FieldType);
                }

                // 1. Get Corner Heights
                float h00 = GetVertexHeight(col, row);
                float h10 = GetVertexHeight(col + 1, row);
                float h01 = GetVertexHeight(col, row + 1);
                float h11 = GetVertexHeight(col + 1, row + 1);

                // 2. Build Seamless Mesh
                var mi = new MeshInstance3D
                {
                    Mesh = BuildSeamlessCellMesh(h00, h10, h01, h11),
                    MaterialOverride = material,
                    Position = new Vector3(col * CellSize, 0, row * CellSize),
                    Name = $"Cell_{col}_{row}"
                };
                AddChild(mi);
                _cellMeshes[col, row] = mi;

                // 2b. Add Collision for Raycasts
                mi.CreateTrimeshCollision();
                // CreateTrimeshCollision automatically creates a StaticBody3D as a child
                // We need to move it to the correct layer if necessary, but default layer 1 is fine for now.

                // 3. Highlight Layer
                var highlightMat = new StandardMaterial3D
                {
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    AlbedoColor = new Color(1, 1, 1, 0.4f),
                };
                float avgY = (h00 + h10 + h01 + h11) / 4f;
                var highlightMi = new MeshInstance3D
                {
                    Mesh = new BoxMesh { Size = new Vector3(CellSize * 0.9f, 0.05f, CellSize * 0.9f) },
                    MaterialOverride = highlightMat,
                    Position = new Vector3(CellSize / 2f, avgY + 0.02f, CellSize / 2f),
                    Visible = false,
                    Name = $"Highlight_{col}_{row}"
                };
                mi.AddChild(highlightMi);
                _highlightMeshes[col, row] = highlightMi;

                // 4. Decoration (Tree/Rock)
                SpawnDecoration(cell, center);
            }
        }
    }

    /// <summary>Calculates elevation at a grid corner by picking the max height of its 4 neighbor cells.</summary>
    private float GetVertexHeight(int x, int z)
    {
        int maxElev = 0;
        for (int dx = -1; dx <= 0; dx++)
        {
            for (int dz = -1; dz <= 0; dz++)
            {
                int nx = x + dx;
                int nz = z + dz;
                if (IsInBounds(new Vector2I(nx, nz)))
                    maxElev = Mathf.Max(maxElev, _cells[nx, nz].Elevation);
            }
        }
        return maxElev * ElevationStep;
    }

    /// <summary>Creates a procedural block mesh where the top vertices follow the given corner heights.</summary>
    private Mesh BuildSeamlessCellMesh(float h00, float h10, float h01, float h11)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        // Define top vertices (Local to the cell origin)
        Vector3 v00 = new Vector3(0, h00, 0);
        Vector3 v10 = new Vector3(CellSize, h10, 0);
        Vector3 v01 = new Vector3(0, h01, CellSize);
        Vector3 v11 = new Vector3(CellSize, h11, CellSize);

        // Bottom vertices
        float bY = -GroundThickness;
        Vector3 b00 = new Vector3(0, bY, 0);
        Vector3 b10 = new Vector3(CellSize, bY, 0);
        Vector3 b01 = new Vector3(0, bY, CellSize);
        Vector3 b11 = new Vector3(CellSize, bY, CellSize);

        // --- TOP FACE ---
        // Calculate a single normal for the top face to ensure "flat" shading (low-poly look)
        // We use the average normal of the two triangles
        Vector3 nTop1 = (v11 - v00).Cross(v10 - v00).Normalized();
        Vector3 nTop2 = (v01 - v00).Cross(v11 - v00).Normalized();
        Vector3 nTop = (nTop1 + nTop2).Normalized();

        st.SetNormal(nTop);
        st.SetUV(new Vector2(0, 0)); st.AddVertex(v00);
        st.SetUV(new Vector2(1, 0)); st.AddVertex(v10);
        st.SetUV(new Vector2(1, 1)); st.AddVertex(v11);

        st.SetNormal(nTop);
        st.SetUV(new Vector2(0, 0)); st.AddVertex(v00);
        st.SetUV(new Vector2(1, 1)); st.AddVertex(v11);
        st.SetUV(new Vector2(0, 1)); st.AddVertex(v01);

        // --- SIDE WALLS ---
        // North face (z=0)
        st.SetNormal(new Vector3(0, 0, -1));
        st.AddVertex(v10); st.AddVertex(v00); st.AddVertex(b00);
        st.AddVertex(v10); st.AddVertex(b00); st.AddVertex(b10);

        // South face (z=CellSize)
        st.SetNormal(new Vector3(0, 0, 1));
        st.AddVertex(v01); st.AddVertex(v11); st.AddVertex(b11);
        st.AddVertex(v01); st.AddVertex(b11); st.AddVertex(b01);

        // East face (x=CellSize)
        st.SetNormal(new Vector3(1, 0, 0));
        st.AddVertex(v11); st.AddVertex(v10); st.AddVertex(b10);
        st.AddVertex(v11); st.AddVertex(b10); st.AddVertex(b11);

        // West face (x=0)
        st.SetNormal(new Vector3(-1, 0, 0));
        st.AddVertex(v00); st.AddVertex(v01); st.AddVertex(b01);
        st.AddVertex(v00); st.AddVertex(b01); st.AddVertex(b00);

        // Note: Don't call st.GenerateNormals() as we set them manually for flat shading
        st.GenerateTangents(); 
        return st.Commit();
    }

    private Color GetColorForType(FieldType type)
    {
        return type switch
        {
            FieldType.Water  => new Color(0.1f, 0.3f, 0.7f), // Blue
            FieldType.Lava   => new Color(0.8f, 0.2f, 0.1f), // Red/Orange
            FieldType.Forest => new Color(0.1f, 0.4f, 0.1f), // Dark Green
            FieldType.Rock   => new Color(0.4f, 0.4f, 0.45f), // Grey
            FieldType.Sand   => new Color(0.8f, 0.7f, 0.4f), // Tan
            _                => new Color(0.25f, 0.27f, 0.35f) // Single clean slate color
        };
    }

    private Texture2D GetTextureForType(FieldType type)
    {
        return type switch
        {
            FieldType.Water => WaterTexture,
            FieldType.Rock  => RockTexture,
            _               => GrassTexture
        };
    }

    private void SpawnDecoration(FieldCell cell, Vector3 position)
    {
        if (cell.FieldType == FieldType.Forest)
        {
            if (TreeScene != null)
            {
                var tree = TreeScene.Instantiate<Node3D>();
                tree.Position = position;
                AddChild(tree);
            }
            else
            {
                // Simple placeholder 3D Sprite for Tree
                var sprite = new Sprite3D();
                sprite.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
                sprite.Position = position + new Vector3(0, 0.7f, 0);
                sprite.PixelSize = 0.02f;
                sprite.Modulate = new Color(0.2f, 0.8f, 0.2f);
                // Try load a generic icon or just stay as a green square
                AddChild(sprite);
            }
        }
        else if (cell.FieldType == FieldType.Rock)
        {
            if (RockScene != null)
            {
                var rock = RockScene.Instantiate<Node3D>();
                rock.Position = position;
                AddChild(rock);
            }
            else
            {
                // Placeholder Mesh (Cube)
                var mesh = new MeshInstance3D();
                mesh.Mesh = new BoxMesh { Size = new Vector3(0.8f, 0.8f, 0.8f) };
                mesh.Position = position + new Vector3(0, 0.4f, 0);
                AddChild(mesh);
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
        
        var mesh = _highlightMeshes[grid.X, grid.Y];
        if (mesh.MaterialOverride is StandardMaterial3D mat)
        {
            mat.AlbedoColor = new Color(color.R, color.G, color.B, 0.6f); // Increased opacity for better visibility
            mesh.Visible = true;
        }
    }

    /// <summary>Reset all highlights back to invisible.</summary>
    public void ClearHighlights()
    {
        if (_highlightMeshes == null) return;
        for (int col = 0; col < Columns; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                _highlightMeshes[col, row].Visible = false;
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
