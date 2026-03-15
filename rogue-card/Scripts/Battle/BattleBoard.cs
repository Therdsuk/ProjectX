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
[Tool]
public partial class BattleBoard : Node3D
{
    // -------------------------------------------------------------------------
    // Configuration (editable in Inspector)
    // -------------------------------------------------------------------------

    [Export] public int Columns    { get; set; } = 8;
    [Export] public int Rows       { get; set; } = 6;
    [Export] public float CellSize { get; set; } = 2.0f;  // meters in 3D

    [ExportGroup("Generation Settings")]
    [Export] public int MapSeed { get; set; } = -1; // -1 means random seed
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

    [ExportGroup("Editor Tools")]
    [Export]
    public bool RegenerateBoard
    {
        get => false;
        set
        {
            if (value && Engine.IsEditorHint())
            {
                GenerateBoard();
            }
        }
    }

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
        if (Engine.IsEditorHint())
        {
            // Only generate automatically if the board is empty in the editor
            if (GetChildCount() == 0)
            {
                GenerateBoard();
            }
            return;
        }

        GenerateBoard();
    }

    // -------------------------------------------------------------------------
    // Board Generation
    // -------------------------------------------------------------------------

    /// <summary>Creates a procedurally generated board.</summary>
    public void GenerateBoard()
    {
        // Clear existing visuals safely (works for both runtime and editor)
        var children = GetChildren();
        foreach (var child in children)
        {
            if (child is MeshInstance3D || child is Sprite3D || child is StaticBody3D || child is Node3D)
            {
                RemoveChild(child);
                child.QueueFree();
            }
        }

        _cells = new FieldCell[Columns, Rows];
        _cellMeshes = new MeshInstance3D[Columns, Rows];
        _highlightMeshes = new MeshInstance3D[Columns, Rows];
        _occupants.Clear();

        // Use custom seed if provided, else generate random
        int effectiveSeed = MapSeed == -1 ? (int)GD.Randi() : MapSeed;

        // 1. Noise Setup
        var noise = new FastNoiseLite();
        noise.Seed = effectiveSeed;
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        noise.Frequency = 0.15f; // Scale of hills/lakes

        var obstacleNoise = new FastNoiseLite();
        obstacleNoise.Seed = effectiveSeed + 1; // Offset slightly for different pattern
        obstacleNoise.Frequency = 0.25f; // More granular for trees/rocks

        // 2. Base Terrain & Elevation Pass
        for (int col = 0; col < Columns; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                var pos = new Vector2I(col, row);
                float val = noise.GetNoise2D(col, row);
                
                FieldType type = FieldType.Normal;
                float elevation = 0f;

                // Thresholds for biomes (smooth continuous elevation)
                if (val < -0.25f) 
                {
                    type = FieldType.Water;
                    
                    // The lower the noise, the deeper the water. 
                    // To prevent awkward shorelines poking through the Shader surface (at -0.15f), 
                    // we immediately sink all water to a minimum of -0.5f flat.
                    // From there, it slopes down dynamically to -1.0f based on the noise intensity.
                    float depthPercent = Mathf.Clamp(Mathf.Abs(val + 0.25f) / 0.75f, 0f, 1f);
                    elevation = -0.5f - (0.5f * depthPercent);
                }
                else if (val > 0.1f) 
                {
                    // Map positive noise dynamically to elevation (val is roughly 0.1 to 0.8)
                    elevation = (val - 0.1f) * 5.0f;
                }

                // Obstacle pass (Forest/Rock) - only on relatively flat ground
                if (type == FieldType.Normal && elevation < 0.5f)
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
        // Removed: Continuous float noise is inherently smooth, no need to fill discrete holes.

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
        
        var cell = _cells[grid.X, grid.Y];
        
        // Block rocks
        if (cell.FieldType == FieldType.Rock) return true;
        
        // Block Deep Water, allow Shallow Water
        // Shallow water is anything >= -0.6f (since the new min water depth is -0.5f)
        if (cell.FieldType == FieldType.Water && cell.Elevation < -0.6f) return true;

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
                
                // If it's water, the global shader handles the surface.
                // The actual terrain cell should look like a sandy shore sloping into it!
                var visualType = cell.FieldType == FieldType.Water ? FieldType.Sand : cell.FieldType;
                
                Texture2D tex = GetTextureForType(visualType);
                if (tex != null)
                {
                    material.AlbedoTexture = tex;
                    material.AlbedoColor = Colors.White;
                }
                else
                {
                    material.AlbedoColor = GetColorForType(visualType);
                }

                // 1. Get Corner Heights
                float h00 = GetVertexHeight(col, row);
                float h10 = GetVertexHeight(col + 1, row);
                float h01 = GetVertexHeight(col, row + 1);
                float h11 = GetVertexHeight(col + 1, row + 1);

                // 2. Build Seamless Mesh
                var mi = new MeshInstance3D
                {
                    Mesh = BuildSeamlessCellMesh(h00, h10, h01, h11, col, row),
                    MaterialOverride = material,
                    Position = new Vector3(col * CellSize, 0, row * CellSize),
                    Name = $"Cell_{col}_{row}"
                };
                AddChild(mi);
                _cellMeshes[col, row] = mi;

                // 2b. Add Collision for Raycasts (Runtime only, skip in Editor to avoid clutter)
                if (!Engine.IsEditorHint())
                {
                    mi.CreateTrimeshCollision();
                }

                // 3. Highlight Layer
                var highlightMat = new StandardMaterial3D
                {
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    AlbedoColor = new Color(1, 1, 1, 0.4f),
                    // Disable depth writing so overlapping highlights don't cut each other out (optional, but good for glassy looks)
                    NoDepthTest = false,
                };
                
                var highlightMi = new MeshInstance3D
                {
                    Mesh = BuildHighlightMesh(h00, h10, h01, h11),
                    MaterialOverride = highlightMat,
                    Position = new Vector3(col * CellSize, 0, row * CellSize), // Position at cell origin now
                    Visible = false,
                    Name = $"Highlight_{col}_{row}"
                };
                AddChild(highlightMi); // Parent to board directly to keep scaling simple
                _highlightMeshes[col, row] = highlightMi;

                // 4. Decoration (Tree/Rock)
                SpawnDecoration(cell, center);
            }
        }
        
        // 5. Global Water Plane
        var waterPlane = new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(Columns * CellSize, Rows * CellSize) },
            Position = new Vector3((Columns * CellSize) / 2f, -0.15f, (Rows * CellSize) / 2f),
            Name = "GlobalWaterPlane"
        };
        var waterShader = GD.Load<Shader>("res://Scripts/Battle/Water.gdshader");
        if (waterShader != null)
        {
            waterPlane.MaterialOverride = new ShaderMaterial { Shader = waterShader };
        }
        else
        {
            GD.PrintErr("[BattleBoard] Could not load Water.gdshader!");
        }
        
        // Disable raycast collision against the water surface so clicks hit the ground underneath it
        // MeshInstance3D doesn't have collision natively so we are completely fine.
        AddChild(waterPlane);

        // Ensure the editor redraws the children
        if (Engine.IsEditorHint())
        {
            foreach (var child in GetChildren())
            {
                if (child is Node3D node)
                {
                    node.Owner = GetTree().EditedSceneRoot;
                }
            }
        }
    }

    /// <summary>Calculates elevation at a grid corner by picking the max height of its 4 neighbor cells.</summary>
    private float GetVertexHeight(int x, int z)
    {
        float maxElev = -999f;
        bool hasNeighbor = false;
        
        for (int dx = -1; dx <= 0; dx++)
        {
            for (int dz = -1; dz <= 0; dz++)
            {
                int nx = x + dx;
                int nz = z + dz;
                if (IsInBounds(new Vector2I(nx, nz)))
                {
                    maxElev = Mathf.Max(maxElev, _cells[nx, nz].Elevation);
                    hasNeighbor = true;
                }
            }
        }
        
        if (!hasNeighbor) return 0f;
        return maxElev * ElevationStep;
    }

    /// <summary>Creates a procedural block mesh where the top vertices follow the given corner heights.</summary>
    private Mesh BuildSeamlessCellMesh(float h00, float h10, float h01, float h11, int col, int row)
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
        // Only draw side walls if they are on the absolute boundary of the entire board.
        // This prevents internal walls from rendering, which would otherwise Z-fight or be
        // visible through translucent materials (like water surfaces).

        // North face (z=0) -> Top edge of board
        if (row == 0)
        {
            st.SetNormal(new Vector3(0, 0, -1));
            st.AddVertex(v10); st.AddVertex(v00); st.AddVertex(b00);
            st.AddVertex(v10); st.AddVertex(b00); st.AddVertex(b10);
        }

        // South face (z=CellSize) -> Bottom edge of board
        if (row == Rows - 1)
        {
            st.SetNormal(new Vector3(0, 0, 1));
            st.AddVertex(v01); st.AddVertex(v11); st.AddVertex(b11);
            st.AddVertex(v01); st.AddVertex(b11); st.AddVertex(b01);
        }

        // East face (x=CellSize) -> Right edge of board
        if (col == Columns - 1)
        {
            st.SetNormal(new Vector3(1, 0, 0));
            st.AddVertex(v11); st.AddVertex(v10); st.AddVertex(b10);
            st.AddVertex(v11); st.AddVertex(b10); st.AddVertex(b11);
        }

        // West face (x=0) -> Left edge of board
        if (col == 0)
        {
            st.SetNormal(new Vector3(-1, 0, 0));
            st.AddVertex(v00); st.AddVertex(v01); st.AddVertex(b01);
            st.AddVertex(v00); st.AddVertex(b01); st.AddVertex(b00);
        }

        // Note: Don't call st.GenerateNormals() as we set them manually for flat shading
        st.GenerateTangents(); 
        return st.Commit();
    }

    /// <summary>Creates a terrain-conforming highlight mesh (just the top face, slightly inset and floating).</summary>
    private Mesh BuildHighlightMesh(float h00, float h10, float h01, float h11)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        // Gap/Margin logic: we shrink the highlight by 5% on all sides
        float margin = CellSize * 0.05f;
        float innerSize = CellSize - margin;
        
        // Z-fighting pad: float slightly above ground
        float yPad = 0.05f;

        // Calculate heights for the inset corners using bilinear interpolation
        // Standard formula: h(x,z) ≈ h00*(1-x)*(1-z) + h10*x*(1-z) + h01*(1-x)*z + h11*x*z  (where x,z are 0.0 to 1.0)
        float pctMin = margin / CellSize;
        float pctMax = innerSize / CellSize;

        float GetInterpolatedY(float xPct, float zPct)
        {
            // The underlying mesh is NOT a smooth bilinear surface; it's two flat triangles.
            // Triangle 1: (0,0), (1,0), (1,1)
            // Triangle 2: (0,0), (1,1), (0,1)
            // To prevent clipping (especially on "saddle" shapes), we must calculate the exact 
            // height on the specific flat triangle the point belongs to.
            
            if (xPct >= zPct)
            {
                // Triangle 1 (Bottom Right side of diagonal)
                // Weights based on barycentric coordinates for this specific triangle split
                return h00 * (1 - xPct) + h10 * (xPct - zPct) + h11 * zPct;
            }
            else
            {
                // Triangle 2 (Top Left side of diagonal)
                return h00 * (1 - zPct) + h01 * (zPct - xPct) + h11 * xPct;
            }
        }

        Vector3 v00 = new Vector3(margin,    GetInterpolatedY(pctMin, pctMin) + yPad, margin);
        Vector3 v10 = new Vector3(innerSize, GetInterpolatedY(pctMax, pctMin) + yPad, margin);
        Vector3 v01 = new Vector3(margin,    GetInterpolatedY(pctMin, pctMax) + yPad, innerSize);
        Vector3 v11 = new Vector3(innerSize, GetInterpolatedY(pctMax, pctMax) + yPad, innerSize);

        // Top face normal
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
