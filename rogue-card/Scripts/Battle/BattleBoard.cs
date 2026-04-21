using Godot;
using System.Collections.Generic;

/// <summary>
/// Generates and manages the battle grid (board).
///
/// Clean flat board with environment art border:
///   - All playable cells are FieldType.Normal at elevation 0
///   - Subtle checkerboard visual pattern for tactical clarity
///   - Decorative environment ring around the playable area
///   - Preserves all gameplay API (movement, pathfinding, highlighting, targeting)
///
/// Attach to a Node3D in BattleScene.tscn called "BattleBoard".
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

    [ExportGroup("Visuals")]
    [Export] public Color CellColorA { get; set; } = new Color(0.22f, 0.24f, 0.30f); // Dark slate
    [Export] public Color CellColorB { get; set; } = new Color(0.26f, 0.28f, 0.34f); // Slightly lighter slate
    [Export] public Color GridLineColor { get; set; } = new Color(0.35f, 0.38f, 0.45f, 0.6f); // Subtle grey lines
    [Export] public float GridLineWidth { get; set; } = 0.03f;
    [Export] public int EnvironmentBorderSize { get; set; } = 3; // Cells of decoration around the grid
    [Export] public Color GroundColor { get; set; } = new Color(0.28f, 0.42f, 0.18f); // Forest green ground

    [ExportGroup("Environment")]
    [Export] public PackedScene TreeScene;
    [Export] public PackedScene RockScene;
    [Export] public PackedScene BushScene;
    [Export] public PackedScene GrassScene;
    [Export] public int TreeCount { get; set; } = 18;
    [Export] public int RockCount { get; set; } = 10;
    [Export] public int BushCount { get; set; } = 14;
    [Export] public int GrassCount { get; set; } = 20;
    [Export] public float DecorationHideDistance { get; set; } = 12.0f; // Decorations closer than this to camera get hidden

    [ExportGroup("Jump Physics")]
    [Export] public float JumpGravity { get; set; } = 20.0f;
    [Export] public float JumpForceMultiplier { get; set; } = 1.25f;

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

    /// <summary>2-D array of 3D meshes for cell visuals.</summary>
    private MeshInstance3D[,] _cellMeshes;

    /// <summary>A second layer of meshes for dynamic highlights (overlay).</summary>
    private MeshInstance3D[,] _highlightMeshes;

    /// <summary>Mapping from grid position → occupying character node (null if empty).</summary>
    private readonly Dictionary<Vector2I, Node3D> _occupants = new();

    private AStarGrid2D _astar;
    private MeshInstance3D _trajectoryArcMesh;

    /// <summary>All spawned environment decorations (for camera occlusion).</summary>
    private readonly List<Node3D> _decorations = new();

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
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
            NoDepthTest = true,
        };
        _trajectoryArcMesh.MaterialOverride = mat;
        AddChild(_trajectoryArcMesh);
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint()) return;
        UpdateDecorationVisibility();
    }

    /// <summary>Fade decorations that are between the camera and the board.</summary>
    private void UpdateDecorationVisibility()
    {
        var camera = GetViewport()?.GetCamera3D();
        if (camera == null) return;

        Vector3 camPos = camera.GlobalPosition;
        Vector3 boardCenter = new Vector3(Columns * CellSize / 2f, 0, Rows * CellSize / 2f);

        // Direction from board center to camera
        Vector3 camDir = (camPos - boardCenter).Normalized();

        foreach (var deco in _decorations)
        {
            if (!IsInstanceValid(deco)) continue;

            // Check if decoration is on the camera side of the board
            Vector3 decoDir = (deco.GlobalPosition - boardCenter).Normalized();
            float dot = camDir.Dot(decoDir);

            float distToCamera = deco.GlobalPosition.DistanceTo(camPos);

            // Calculate target alpha: fade out when on camera side AND close
            float targetAlpha = 1.0f;
            if (dot > 0.3f && distToCamera < DecorationHideDistance)
            {
                // Smooth gradient: fully transparent at close range, fade in toward the threshold
                float fadeStart = DecorationHideDistance;
                float fadeEnd = DecorationHideDistance * 0.4f; // Fully transparent at 40% of threshold
                targetAlpha = Mathf.Clamp((distToCamera - fadeEnd) / (fadeStart - fadeEnd), 0.1f, 1.0f);
            }

            SetNodeAlpha(deco, targetAlpha);
        }
    }

    /// <summary>Recursively set transparency on all visual descendants (MeshInstance3D + Sprite3D).</summary>
    private void SetNodeAlpha(Node node, float alpha)
    {
        if (node is Sprite3D sprite)
        {
            var c = sprite.Modulate;
            sprite.Modulate = new Color(c.R, c.G, c.B, alpha);
        }
        else if (node is MeshInstance3D mesh)
        {
            if (mesh.MaterialOverride is StandardMaterial3D mat)
            {
                if (alpha < 0.99f)
                {
                    mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                    var c = mat.AlbedoColor;
                    mat.AlbedoColor = new Color(c.R, c.G, c.B, alpha);
                }
                else
                {
                    mat.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
                    var c = mat.AlbedoColor;
                    mat.AlbedoColor = new Color(c.R, c.G, c.B, 1.0f);
                }
            }
        }

        foreach (var child in node.GetChildren())
        {
            SetNodeAlpha(child, alpha);
        }
    }

    // -------------------------------------------------------------------------
    // Board Generation — Clean Flat Grid
    // -------------------------------------------------------------------------

    /// <summary>Creates a clean flat board with environment decoration border.</summary>
    public void GenerateBoard()
    {
        // Clear existing visuals
        var children = GetChildren();
        foreach (var child in children)
        {
            if (child is Node3D)
            {
                RemoveChild(child);
                child.QueueFree();
            }
        }

        _cells = new FieldCell[Columns, Rows];
        _cellMeshes = new MeshInstance3D[Columns, Rows];
        _highlightMeshes = new MeshInstance3D[Columns, Rows];
        _occupants.Clear();
        _decorations.Clear();

        // All cells are flat Normal
        for (int col = 0; col < Columns; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                var pos = new Vector2I(col, row);
                _cells[col, row] = new FieldCell(pos, FieldType.Normal);
                _occupants[pos] = null;
            }
        }

        GD.Print($"[BattleBoard] Generated {Columns}×{Rows} flat board.");
        
        SetupAStar();
        DrawBoardVisuals();
        DrawEnvironmentBorder();
        SetupLighting();
    }

    /// <summary>Adds a directional light from the camera direction + ambient fill.</summary>
    private void SetupLighting()
    {
        // Directional light — matches the isometric camera angle (upper-right)
        var dirLight = new DirectionalLight3D
        {
            Name = "BoardDirectionalLight",
            // Side light — comes from the right, pitched down 35°
            RotationDegrees = new Vector3(-35f, 90f, 0f),
            LightColor = new Color(1.0f, 0.96f, 0.90f), // Warm sunlight
            LightEnergy = 1.0f,
            ShadowEnabled = true,
            DirectionalShadowMode = DirectionalLight3D.ShadowMode.Orthogonal,
            ShadowNormalBias = 2.0f,
            ShadowBias = 0.1f,
        };
        AddChild(dirLight);

        // Ambient environment so shadows aren't pitch black
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.12f, 0.14f, 0.18f), // Dark blue-grey sky
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.5f, 0.55f, 0.65f), // Cool blue-ish fill
            AmbientLightEnergy = 0.4f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
        };
        var worldEnv = new WorldEnvironment
        {
            Name = "BoardWorldEnvironment",
            Environment = env,
        };
        AddChild(worldEnv);
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

        // On a flat board, no cells are solid by default
        // (Field type effects like Rock blocking can be added later via M3)
    }

    // -------------------------------------------------------------------------
    // Coordinate Helpers
    // -------------------------------------------------------------------------

    /// <summary>Convert a grid position to world pos.</summary>
    public Vector3 GridToWorld(Vector2I grid)
    {
        return new Vector3(grid.X * CellSize, 0, grid.Y * CellSize);
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
    /// within bounds and NOT occupied.
    /// </summary>
    public Vector2I GetNearestValidCell(Vector2I preferred)
    {
        if (!IsOccupied(preferred)) return preferred;

        for (int dist = 1; dist <= Mathf.Max(Columns, Rows); dist++)
        {
            for (int q = -dist; q <= dist; q++)
            {
                for (int r = -dist; r <= dist; r++)
                {
                    if (Mathf.Abs(q) + Mathf.Abs(r) != dist) continue;

                    Vector2I candidate = preferred + new Vector2I(q, r);
                    if (IsInBounds(candidate) && !IsOccupied(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        return preferred;
    }

    // -------------------------------------------------------------------------
    // Board Visuals — Flat Checkerboard Grid
    // -------------------------------------------------------------------------

    private void DrawBoardVisuals()
    {
        for (int col = 0; col < Columns; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                // Checkerboard pattern
                bool isDark = (col + row) % 2 == 0;
                Color cellColor = isDark ? CellColorA : CellColorB;

                var material = new StandardMaterial3D
                {
                    AlbedoColor = cellColor,
                    Roughness = 0.85f,
                };

                // Simple flat quad for each cell
                var planeMesh = new PlaneMesh
                {
                    Size = new Vector2(CellSize, CellSize),
                };

                var mi = new MeshInstance3D
                {
                    Mesh = planeMesh,
                    MaterialOverride = material,
                    Position = new Vector3(col * CellSize + CellSize / 2f, 0, row * CellSize + CellSize / 2f),
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                    Name = $"Cell_{col}_{row}"
                };
                AddChild(mi);
                _cellMeshes[col, row] = mi;

                // Collision for raycasts (runtime only)
                if (!Engine.IsEditorHint())
                {
                    mi.CreateTrimeshCollision();
                }

                // Grid lines — thin raised quads on cell edges
                DrawGridLines(col, row);

                // Highlight layer
                var highlightMat = new StandardMaterial3D
                {
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    AlbedoColor = new Color(1, 1, 1, 0.4f),
                    ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded,
                };
                
                float highlightInset = CellSize * 0.05f;
                var highlightMi = new MeshInstance3D
                {
                    Mesh = new PlaneMesh { Size = new Vector2(CellSize - highlightInset * 2, CellSize - highlightInset * 2) },
                    MaterialOverride = highlightMat,
                    Position = new Vector3(col * CellSize + CellSize / 2f, 0.02f, row * CellSize + CellSize / 2f),
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                    Visible = false,
                    Name = $"Highlight_{col}_{row}"
                };
                AddChild(highlightMi);
                _highlightMeshes[col, row] = highlightMi;
            }
        }

        // Board base (slightly below to give depth)
        var baseMat = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.15f, 0.16f, 0.20f),
            Roughness = 0.95f,
        };
        var basePlane = new MeshInstance3D
        {
            Mesh = new BoxMesh 
            { 
                Size = new Vector3(Columns * CellSize + 0.2f, 0.15f, Rows * CellSize + 0.2f) 
            },
            MaterialOverride = baseMat,
            Position = new Vector3(
                Columns * CellSize / 2f, 
                -0.095f, // Top face at -0.02, clearly below cells at Y=0
                Rows * CellSize / 2f
            ),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Name = "BoardBase"
        };
        AddChild(basePlane);

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

    private void DrawGridLines(int col, int row)
    {
        var lineMat = new StandardMaterial3D
        {
            AlbedoColor = GridLineColor,
            ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };

        float y = 0.005f; // Slight float above cell surface

        // Bottom edge of cell
        if (row == Rows - 1 || true) // Draw all horizontal lines
        {
            var hLine = new MeshInstance3D
            {
                Mesh = new PlaneMesh { Size = new Vector2(CellSize, GridLineWidth) },
                MaterialOverride = lineMat,
                Position = new Vector3(col * CellSize + CellSize / 2f, y, row * CellSize),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Name = $"GridH_{col}_{row}"
            };
            AddChild(hLine);
        }

        // Right edge of cell 
        if (col == Columns - 1 || true) // Draw all vertical lines
        {
            var vLine = new MeshInstance3D
            {
                Mesh = new PlaneMesh { Size = new Vector2(GridLineWidth, CellSize) },
                MaterialOverride = lineMat,
                Position = new Vector3(col * CellSize, y, row * CellSize + CellSize / 2f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Name = $"GridV_{col}_{row}"
            };
            AddChild(vLine);
        }

        // Close the grid on bottom and right borders
        if (row == Rows - 1)
        {
            var hLineEnd = new MeshInstance3D
            {
                Mesh = new PlaneMesh { Size = new Vector2(CellSize, GridLineWidth) },
                MaterialOverride = lineMat,
                Position = new Vector3(col * CellSize + CellSize / 2f, y, (row + 1) * CellSize),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Name = $"GridHEnd_{col}"
            };
            AddChild(hLineEnd);
        }
        if (col == Columns - 1)
        {
            var vLineEnd = new MeshInstance3D
            {
                Mesh = new PlaneMesh { Size = new Vector2(GridLineWidth, CellSize) },
                MaterialOverride = lineMat,
                Position = new Vector3((col + 1) * CellSize, y, row * CellSize + CellSize / 2f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Name = $"GridVEnd_{row}"
            };
            AddChild(vLineEnd);
        }
    }

    // -------------------------------------------------------------------------
    // Environment Border — Decorative Ring Around the Board
    // -------------------------------------------------------------------------

    private void DrawEnvironmentBorder()
    {
        int border = EnvironmentBorderSize;
        float boardW = Columns * CellSize;
        float boardH = Rows * CellSize;
        float totalW = boardW + border * CellSize * 2;
        float totalH = boardH + border * CellSize * 2;

        // Large ground plane extending under and around the board
        var groundMat = new StandardMaterial3D
        {
            AlbedoColor = GroundColor,
            Roughness = 0.95f,
        };
        var groundPlane = new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(totalW + 8f, totalH + 8f) },
            MaterialOverride = groundMat,
            Position = new Vector3(boardW / 2f, -0.01f, boardH / 2f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Name = "EnvironmentGround"
        };
        AddChild(groundPlane);

        // Spawn decorative objects around the border
        var rng = new RandomNumberGenerator();
        rng.Seed = 42; // Deterministic for consistency

        SpawnBorderDecorations(rng, boardW, boardH, border);
    }

    private void SpawnBorderDecorations(RandomNumberGenerator rng, float boardW, float boardH, int border)
    {
        float borderWorldSize = border * CellSize;

        // Define the border zone (outside the playable grid)
        float minX = -borderWorldSize;
        float maxX = boardW + borderWorldSize;
        float minZ = -borderWorldSize;
        float maxZ = boardH + borderWorldSize;

        // Trees
        for (int i = 0; i < TreeCount; i++)
        {
            Vector3 pos = GetRandomBorderPosition(rng, minX, maxX, minZ, maxZ, boardW, boardH, 1.5f);
            var deco = SpawnTree(pos);
            if (deco != null) _decorations.Add(deco);
        }

        // Rocks
        for (int i = 0; i < RockCount; i++)
        {
            Vector3 pos = GetRandomBorderPosition(rng, minX, maxX, minZ, maxZ, boardW, boardH, 0.5f);
            var deco = SpawnRock(pos);
            if (deco != null) _decorations.Add(deco);
        }

        // Bushes
        for (int i = 0; i < BushCount; i++)
        {
            Vector3 pos = GetRandomBorderPosition(rng, minX, maxX, minZ, maxZ, boardW, boardH, 0.8f);
            var deco = SpawnBush(pos);
            if (deco != null) _decorations.Add(deco);
        }

        // Grass patches
        for (int i = 0; i < GrassCount; i++)
        {
            Vector3 pos = GetRandomBorderPosition(rng, minX, maxX, minZ, maxZ, boardW, boardH, 0.2f);
            var deco = SpawnGrassPatch(pos);
            if (deco != null) _decorations.Add(deco);
        }
    }

    /// <summary>Returns a random position within the border zone (NOT inside the playable grid).</summary>
    private Vector3 GetRandomBorderPosition(RandomNumberGenerator rng, float minX, float maxX, float minZ, float maxZ, float boardW, float boardH, float padding)
    {
        Vector3 pos;
        int attempts = 0;
        do
        {
            pos = new Vector3(
                rng.RandfRange(minX, maxX),
                0,
                rng.RandfRange(minZ, maxZ)
            );
            attempts++;
        }
        while (pos.X > -padding && pos.X < boardW + padding &&
               pos.Z > -padding && pos.Z < boardH + padding &&
               attempts < 50);

        return pos;
    }

    private Node3D SpawnTree(Vector3 position)
    {
        if (TreeScene != null)
        {
            var tree = TreeScene.Instantiate<Node3D>();
            tree.Position = position;
            AddChild(tree);
            return tree;
        }

        // Placeholder: Trunk + Canopy grouped under one Node3D
        var treeGroup = new Node3D { Name = "Tree", Position = position };

        var trunkMat = new StandardMaterial3D { AlbedoColor = new Color(0.40f, 0.26f, 0.13f), Roughness = 0.9f };
        var canopyMat = new StandardMaterial3D { AlbedoColor = new Color(0.15f, 0.5f, 0.15f), Roughness = 0.85f };

        var trunk = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.15f, BottomRadius = 0.2f, Height = 1.8f },
            MaterialOverride = trunkMat,
            Position = new Vector3(0, 0.9f, 0),
            Name = "Tree_Trunk"
        };
        var canopy = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.8f, Height = 1.2f },
            MaterialOverride = canopyMat,
            Position = new Vector3(0, 2.2f, 0),
            Name = "Tree_Canopy"
        };
        treeGroup.AddChild(trunk);
        treeGroup.AddChild(canopy);
        AddChild(treeGroup);
        return treeGroup;
    }

    private Node3D SpawnRock(Vector3 position)
    {
        if (RockScene != null)
        {
            var rockInstance = RockScene.Instantiate<Node3D>();
            rockInstance.Position = position;
            AddChild(rockInstance);
            return rockInstance;
        }

        // Placeholder: Grey box with slight random scale
        var rockMat = new StandardMaterial3D { AlbedoColor = new Color(0.45f, 0.43f, 0.40f), Roughness = 0.95f };
        var rng = new RandomNumberGenerator();
        float s = rng.RandfRange(0.4f, 0.9f);

        var rock = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(s * 1.2f, s * 0.7f, s) },
            MaterialOverride = rockMat,
            Position = position + new Vector3(0, s * 0.35f, 0),
            Name = "Rock"
        };
        AddChild(rock);
        return rock;
    }

    private Node3D SpawnBush(Vector3 position)
    {
        if (BushScene != null)
        {
            var bushInstance = BushScene.Instantiate<Node3D>();
            bushInstance.Position = position;
            AddChild(bushInstance);
            return bushInstance;
        }

        // Placeholder: Small green sphere close to the ground
        var bushMat = new StandardMaterial3D { AlbedoColor = new Color(0.20f, 0.55f, 0.20f), Roughness = 0.85f };

        var bush = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.45f, Height = 0.6f },
            MaterialOverride = bushMat,
            Position = position + new Vector3(0, 0.25f, 0),
            Name = "Bush"
        };
        AddChild(bush);
        return bush;
    }

    private Node3D SpawnGrassPatch(Vector3 position)
    {
        if (GrassScene != null)
        {
            var grassInstance = GrassScene.Instantiate<Node3D>();
            grassInstance.Position = position;
            AddChild(grassInstance);
            return grassInstance;
        }

        // Placeholder: Slightly lighter green flat disc
        var grassMat = new StandardMaterial3D 
        { 
            AlbedoColor = new Color(0.32f, 0.52f, 0.22f), 
            Roughness = 0.9f,
        };

        var grass = new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(0.8f, 0.8f) },
            MaterialOverride = grassMat,
            Position = position + new Vector3(0, 0.005f, 0),
            Name = "GrassPatch"
        };
        AddChild(grass);
        return grass;
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
            mat.AlbedoColor = new Color(color.R, color.G, color.B, 0.6f);
            mesh.Visible = true;
        }
    }

    public bool HighlightTrajectoryArc(Vector2I fromCell, Vector2I toCell, float launchSpeed, Color color)
    {
        if (_trajectoryArcMesh == null || !(_trajectoryArcMesh.Mesh is ImmediateMesh im)) return false;

        im.ClearSurfaces();
        
        Vector3 start = CellCentre(fromCell);
        Vector3 end = CellCentre(toCell);
        
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

        if (x < 0.01f)
        {
            theta = Mathf.Pi / 2f; 
        }
        else
        {
            float determinant = v4 - g * (g * x * x + 2 * y * v2);
            if (determinant < -0.01f)
            {
                reachable = false;
                theta = Mathf.Pi / 4f;
            }
            else
            {
                theta = Mathf.Atan((v2 + Mathf.Sqrt(Mathf.Max(0, determinant))) / (g * x));
            }
        }

        Vector3 horizontalDir = diff;
        horizontalDir.Y = 0;
        horizontalDir = horizontalDir.Normalized();

        float vX = v * Mathf.Cos(theta);
        float vY = v * Mathf.Sin(theta);

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
                Vector2I lineDiff = targetCell - playerPos;
                Vector2I dir = Vector2I.Zero;
                
                if (Mathf.Abs(lineDiff.X) > Mathf.Abs(lineDiff.Y)) dir = new Vector2I(Mathf.Sign(lineDiff.X), 0);
                else dir = new Vector2I(0, Mathf.Sign(lineDiff.Y));
                
                if (dir == Vector2I.Zero) dir = Vector2I.Right;
                
                for (int i = 1; i <= 5; i++) 
                {
                    var pos = playerPos + dir * i;
                    if (IsInBounds(pos)) cells.Add(pos);
                }
                break;
        }
        
        return cells;
    }

    /// <summary>Check if there is a clear line of sight between two cells.</summary>
    public bool HasLineOfSight(Vector2I from, Vector2I to)
    {
        // On a flat board with no obstacles, line of sight is always clear
        if (from == to) return true;
        if (!IsInBounds(from) || !IsInBounds(to)) return false;
        return true;
    }

    /// <summary>Find all cells reachable from start within range, respecting AStar solid points.</summary>
    public List<Vector2I> GetReachableCells(Vector2I start, int range)
    {
        var reachable = new List<Vector2I>();
        if (_astar == null) SetupAStar();

        var queue = new Queue<(Vector2I Pos, int Dist)>();
        queue.Enqueue((start, 0));
        var visited = new HashSet<Vector2I> { start };

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();
            reachable.Add(current);

            if (dist < range)
            {
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
        return path.Count - 1;
    }

    /// <summary>Move a unit cleanly from one cell to another, using a Tween sequence for cell-by-cell movement.</summary>
    /// <returns>True if the move was valid and initiated, False if blocked or out of bounds.</returns>
    public bool MoveUnit(Node3D unit, Vector2I from, Vector2I to)
    {
        if (from == to) return true;
        
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

        if (IsInBounds(from))
        {
             _occupants[from] = null;
        }
        _occupants[to] = unit;

        if (_astar == null) SetupAStar();
        
        var path = _astar.GetIdPath(from, to);
        
        if (path.Count <= 1)
        {
            unit.Position = CellCentre(to);
            return true;
        }

        Tween tween = GetTree().CreateTween();
        
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

        if (IsInBounds(from)) _occupants[from] = null;
        _occupants[to] = unit;

        unit.Position = CellCentre(to);
        
        GD.Print($"[BattleBoard] MoveUnitImmediate: {unit.Name} jumped from {from} to {to}");
    }
}
