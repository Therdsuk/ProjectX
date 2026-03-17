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
    [Export] public Texture2D CliffTexture;
    [Export] public PackedScene TreeScene;
    [Export] public PackedScene RockScene;
    [Export] public PackedScene GroundScene;
    [Export] public PackedScene RampScene;
    [Export] public float GroundThickness { get; set; } = 0.5f;
    [Export] public float ElevationStep { get; set; } = 0.5f;
    [Export] public float HighGroundDensity { get; set; } = 0.1f;
    [Export] public float MaxWalkableSlope { get; set; } = 0.7f;
    [Export] public float JumpGravity { get; set; } = 20.0f; // Custom gravity for the jump arc
    [Export] public float JumpForceMultiplier { get; set; } = 1.25f; // Buffed power to ensure clear flight over obstacles

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

    private AStarGrid2D _astar;
    private MeshInstance3D _trajectoryArcMesh;

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

        // Setup 3D Trajectory Mesh Instance
        _trajectoryArcMesh = new MeshInstance3D
        {
            Name = "TrajectoryArc",
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        _trajectoryArcMesh.Mesh = new ImmediateMesh();
        
        var mat = new StandardMaterial3D
        {
            ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = Colors.White,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            NoDepthTest = true, // See through terrain
        };
        _trajectoryArcMesh.MaterialOverride = mat;
        AddChild(_trajectoryArcMesh);
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

        // --- Pass 1.5: Slope Check & Cliff Gaps ---
        // We create "ramps" or gaps in long cliff walls to ensure playability.
        var cliffNoise = new FastNoiseLite();
        cliffNoise.Seed = (int)GD.Randi();
        cliffNoise.Frequency = 0.2f; // High frequency for frequent gaps

        for (int col = 0; col < Columns; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                var cell = _cells[col, row];
                float h00 = GetVertexHeight(col, row);
                float h10 = GetVertexHeight(col + 1, row);
                float h01 = GetVertexHeight(col, row + 1);
                float h11 = GetVertexHeight(col + 1, row + 1);
 
                float dx1 = Mathf.Abs(h10 - h00);
                float dx2 = Mathf.Abs(h11 - h01);
                float dz1 = Mathf.Abs(h01 - h00);
                float dz2 = Mathf.Abs(h11 - h10);
                
                float maxDiff = Mathf.Max(Mathf.Max(dx1, dx2), Mathf.Max(dz1, dz2));
                float slope = maxDiff / CellSize;

                if (slope > MaxWalkableSlope)
                {
                    // Only mark as cliff if the cliffNoise is above a threshold.
                    // This creates intermittent gaps in long ridges.
                    if (cliffNoise.GetNoise2D(col, row) < 0.3f) 
                    {
                        cell.IsCliff = true;
                    }
                }
            }
        }

        // --- Pass 2: Smoothing (Fill Holes) ---
        // Removed: Continuous float noise is inherently smooth, no need to fill discrete holes.

        GD.Print($"[BattleBoard] Generated {Columns}×{Rows} seamless procedural board.");
        
        SetupAStar();
        DrawBoardVisuals();
    }

    private void SetupAStar()
    {
        _astar = new AStarGrid2D
        {
            Region = new Rect2I(0, 0, Columns, Rows),
            CellSize = new Vector2(1, 1),
            DefaultComputeHeuristic = AStarGrid2D.Heuristic.Manhattan,
            DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Manhattan,
            DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never
        };
        _astar.Update();

        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                var cell = _cells[x, y];
                bool isBlocked = cell.FieldType == FieldType.Rock || cell.IsCliff;
                
                // Block deep water
                if (cell.FieldType == FieldType.Water && cell.Elevation < -0.6f) isBlocked = true;

                _astar.SetPointSolid(new Vector2I(x, y), isBlocked);
            }
        }
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
        return new Vector2I(Mathf.FloorToInt(world.X / CellSize), Mathf.FloorToInt(world.Z / CellSize));
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
        
        // Block rocks and cliffs
        if (cell.FieldType == FieldType.Rock || cell.IsCliff) return true;
        
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

    /// <summary>
    /// Searches outward from a preferred position for the nearest cell that is 
    /// within bounds and NOT occupied/blocked by terrain (cliffs, rocks, deep water).
    /// </summary>
    public Vector2I GetNearestValidCell(Vector2I preferred)
    {
        if (!IsOccupied(preferred)) return preferred;

        // Spiral/Outward search
        for (int dist = 1; dist <= Mathf.Max(Columns, Rows); dist++)
        {
            for (int q = -dist; q <= dist; q++)
            {
                for (int r = -dist; r <= dist; r++)
                {
                    // Only check cells at the current Manhattan distance shell
                    if (Mathf.Abs(q) + Mathf.Abs(r) != dist) continue;

                    Vector2I candidate = preferred + new Vector2I(q, r);
                    if (IsInBounds(candidate) && !IsOccupied(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        return preferred; // Fallback to preferred if somehow the whole board is blocked
    }

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
                
                Texture2D tex = GetTextureForType(visualType, cell.IsCliff);
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

    private Texture2D GetTextureForType(FieldType type, bool isCliff = false)
    {
        if (isCliff && CliffTexture != null) return CliffTexture;

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

    public bool HighlightTrajectoryArc(Vector2I fromCell, Vector2I toCell, float launchSpeed, Color color)
    {
        if (_trajectoryArcMesh == null || !(_trajectoryArcMesh.Mesh is ImmediateMesh im)) return false;

        im.ClearSurfaces();
        
        Vector3 start = CellCentre(fromCell);
        Vector3 end = CellCentre(toCell);
        
        // Offset start/end so logic is above character heads
        start.Y += 1.2f;
        end.Y += 1.2f;
        
        float g = JumpGravity;
        Vector3 diff = end - start;
        float x = new Vector2(diff.X, diff.Z).Length();
        float y = diff.Y;
        float v = launchSpeed;
        float v2 = v * v;
        float v4 = v2 * v2;

        float theta = 0;
        bool reachable = true;

        if (x < 0.01f) // Self target (Straight Up)
        {
            theta = Mathf.Pi / 2f; 
        }
        else
        {
            float determinant = v4 - g * (g * x * x + 2 * y * v2);
            if (determinant < -0.01f) // Added small epsilon for float precision
            {
                reachable = false;
                theta = Mathf.Pi / 4f; // Fallback visualization
            }
            else
            {
                // High arc (+) feels more like a "Jump" card
                theta = Mathf.Atan((v2 + Mathf.Sqrt(Mathf.Max(0, determinant))) / (g * x));
            }
        }

        // Horizontal direction
        Vector3 horizontalDir = diff;
        horizontalDir.Y = 0;
        horizontalDir = horizontalDir.Normalized();

        // Velocities
        float vX = v * Mathf.Cos(theta);
        float vY = v * Mathf.Sin(theta);

        // Calculate flight time
        float totalTime = (x < 0.01f) ? (2 * vY / g) : (x / vX);

        int steps = 30;
        bool blocked = !reachable;

        im.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
        
        Vector3 lastPos = start;
        for (int i = 0; i <= steps; i++)
        {
            float t = totalTime * (i / (float)steps);
            Vector3 pos = start;
            pos.X += horizontalDir.X * vX * t;
            pos.Z += horizontalDir.Z * vX * t;
            pos.Y += vY * t - 0.5f * g * t * t;
            
            // Collison Check: Skip the first 10% and last 10% of the flight path 
            // to avoid hitting the character's own feet or the ground/cliff edge upon landing.
            if (i > steps * 0.1f && i < steps * 0.9f) 
            {
                if (CheckArcCollision(lastPos, pos))
                {
                    blocked = true;
                }
            }
            
            lastPos = pos;
            im.SurfaceAddVertex(pos);
        }
        
        Color arcColor = blocked ? new Color(1, 0, 0, 0.8f) : color;
        ((StandardMaterial3D)_trajectoryArcMesh.MaterialOverride).AlbedoColor = arcColor;
        
        im.SurfaceEnd();
        _trajectoryArcMesh.Visible = true;

        // Landing Marker
        HighlightCell(toCell, blocked ? new Color(1, 0, 0, 0.5f) : new Color(arcColor.R, arcColor.G, arcColor.B, 1.0f)); 
        
        return !blocked;
    }

    private bool CheckArcCollision(Vector3 from, Vector3 to)
    {
        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        var result = spaceState.IntersectRay(query);
        return result.Count > 0;
    }

    public void ClearTrajectory()
    {
        if (_trajectoryArcMesh == null || !(_trajectoryArcMesh.Mesh is ImmediateMesh im)) return;
        im.ClearSurfaces();
        _trajectoryArcMesh.Visible = false;
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
                Vector2I diff = targetCell - playerPos;
                Vector2I dir = Vector2I.Zero;
                
                // Prioritise the cardinal direction of the target relative to caster
                if (Mathf.Abs(diff.X) > Mathf.Abs(diff.Y)) dir = new Vector2I(Mathf.Sign(diff.X), 0);
                else dir = new Vector2I(0, Mathf.Sign(diff.Y));
                
                if (dir == Vector2I.Zero) dir = Vector2I.Right;
                
                // Beam starts AT THE CASTER and goes through the target
                // Length is determined by card range (approximating 5 for now as per previous logic, but ideally we'd pass range)
                for (int i = 1; i <= 5; i++) 
                {
                    var pos = playerPos + dir * i;
                    if (IsInBounds(pos)) cells.Add(pos);
                }
                break;
        }
        
        return cells;
    }

    /// <summary>Check if there is a clear line of sight between two cells (no rocks or cliffs).</summary>
    public bool HasLineOfSight(Vector2I from, Vector2I to)
    {
        if (from == to) return true;

        // Simple grid raycast (Bresenham-lite)
        int x0 = from.X; int y0 = from.Y;
        int x1 = to.X;   int y1 = to.Y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x0 == x1 && y0 == y1) break;

            // Check if CURRENT step is an obstacle (except for the start/end points if desired)
            // But usually, if the cell you are STANDING ON is a rock, you can't see out.
            // If the cell you are TARGETING is a rock, you can't see "into" it perfectly,
            // but for gameplay we usually allow targeting the obstacle itself.
            if ((x0 != from.X || y0 != from.Y) && (x0 != to.X || y0 != to.Y))
            {
                var cell = _cells[x0, y0];
                if (cell.FieldType == FieldType.Rock || cell.IsCliff) return false;
            }

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }

        return true;
    }

    /// <summary>Find all cells reachable from start within range, respecting AStar solid points.</summary>
    public List<Vector2I> GetReachableCells(Vector2I start, int range)
    {
        var reachable = new List<Vector2I>();
        if (_astar == null) SetupAStar();

        // BFS to find all reachable cells within range
        var queue = new Queue<(Vector2I Pos, int Dist)>();
        queue.Enqueue((start, 0));
        var visited = new HashSet<Vector2I> { start };

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();
            reachable.Add(current);

            if (dist < range)
            {
                // Orthogonal neighbors
                Vector2I[] neighbors = {
                    current + Vector2I.Up,
                    current + Vector2I.Down,
                    current + Vector2I.Left,
                    current + Vector2I.Right
                };

                foreach (var next in neighbors)
                {
                    if (IsInBounds(next) && !visited.Contains(next) && !_astar.IsPointSolid(next))
                    {
                        visited.Add(next);
                        queue.Enqueue((next, dist + 1));
                    }
                }
            }
        }

        return reachable;
    }

    /// <summary>Find a path between two cells using AStar.</summary>
    public List<Vector2I> GetPath(Vector2I from, Vector2I to)
    {
        if (_astar == null) SetupAStar();
        var path = _astar.GetIdPath(from, to);
        return new List<Vector2I>(path);
    }

    /// <summary>Returns the length of the A* path between two points. Returns 999 if unreachable.</summary>
    public int GetPathLength(Vector2I from, Vector2I to)
    {
        if (from == to) return 0;
        if (_astar == null) SetupAStar();
        var path = _astar.GetIdPath(from, to);
        if (path.Count == 0) return 999;
        return path.Count - 1; // path includes start point
    }

    /// <summary>Move a unit cleanly from one cell to another, using a Tween sequence for cell-by-cell movement.</summary>
    /// <returns>True if the move was valid and initiated, False if blocked or out of bounds.</returns>
    public bool MoveUnit(Node3D unit, Vector2I from, Vector2I to)
    {
        if (from == to) return true; // Already there is a "success"
        
        if (!IsInBounds(to))
        {
            GD.PrintErr($"[BattleBoard] MoveUnit FAILED: Target {to} out of bounds.");
            return false;
        }

        if (IsOccupied(to))
        {
            GD.PrintErr($"[BattleBoard] MoveUnit FAILED: Target {to} is occupied by {GetOccupant(to)?.Name ?? "Terrain"}.");
            return false;
        }

        GD.Print($"[BattleBoard] MoveUnit: {unit.Name} from {from} to {to}");

        // Update Backend
        if (IsInBounds(from))
        {
             _occupants[from] = null;
        }
        _occupants[to] = unit;

        // Pathfinding
        if (_astar == null) SetupAStar(); // Safety check
        
        // Temporarily clear solid status for the unit's start and end to ensure pathfinding works if they were solid
        // Actually, occupants aren't solid in AStar by default in our setup (only terrain is), 
        // so we just calculate the path.
        var path = _astar.GetIdPath(from, to);
        
        if (path.Count <= 1)
        {
            unit.Position = CellCentre(to);
            return true;
        }

        // Update Physics (Slide smoothly cell-by-cell)
        Tween tween = GetTree().CreateTween();
        
        // Skip the first point as it is the current position
        for (int i = 1; i < path.Count; i++)
        {
            var nextCell = path[i];
            tween.TweenProperty(unit, "position", CellCentre(nextCell), 0.15f)
                 .SetTrans(Tween.TransitionType.Linear);
        }

        return true;
    }

    /// <summary>Instantly teleports a unit from one cell to another without animation or pathfinding. Use for Jump/Blink.</summary>
    public void MoveUnitImmediate(Node3D unit, Vector2I from, Vector2I to)
    {
        if (!IsInBounds(to)) return;

        // Update Backend
        if (IsInBounds(from)) _occupants[from] = null;
        _occupants[to] = unit;

        // Update Physics
        unit.Position = CellCentre(to);
        
        GD.Print($"[BattleBoard] MoveUnitImmediate: {unit.Name} jumped from {from} to {to}");
    }
}
