using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages the 8x8 battle board and tile data
/// </summary>
public partial class BoardManager : Node3D
{
    private const int BOARD_SIZE = 8;
    private BoardTile[,] _tiles;
    private Node3D _boardVisuals;

    [Export]
    public float TileSize { get; set; } = 1.0f;

    [Export]
    public Material TileMaterial { get; set; }

    public int BoardSize => BOARD_SIZE;

    public override void _Ready()
    {
        InitializeBoard();
        CreateBoardVisuals();
        GD.Print("BoardManager: 8x8 Battle board initialized");
    }

    /// <summary>
    /// Initialize the tile data structure
    /// </summary>
    private void InitializeBoard()
    {
        _tiles = new BoardTile[BOARD_SIZE, BOARD_SIZE];

        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int z = 0; z < BOARD_SIZE; z++)
            {
                _tiles[x, z] = new BoardTile
                {
                    GridPosition = new Vector2I(x, z),
                    FieldType = FieldType.Normal,
                    OccupantCharacter = null,
                    ActiveEffects = new List<string>()
                };
            }
        }
    }

    /// <summary>
    /// Create visual representation of the board
    /// </summary>
    private void CreateBoardVisuals()
    {
        _boardVisuals = new Node3D();
        _boardVisuals.Name = "BoardVisuals";
        AddChild(_boardVisuals);

        // Create a simple plane grid for the board
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int z = 0; z < BOARD_SIZE; z++)
            {
                var tile = new MeshInstance3D();
                tile.Name = $"Tile_{x}_{z}";
                
                // Create a simple plane mesh
                var planeMesh = new PlaneMesh();
                planeMesh.Size = new Vector2(TileSize, TileSize);
                tile.Mesh = planeMesh;

                // Create a simple material (checkerboard pattern)
                var material = new StandardMaterial3D();
                if ((x + z) % 2 == 0)
                {
                    material.AlbedoColor = new Color(0.7f, 0.7f, 0.7f);
                }
                else
                {
                    material.AlbedoColor = new Color(0.5f, 0.5f, 0.5f);
                }
                tile.SetSurfaceOverrideMaterial(0, material);

                // Position tile (isometric-like layout)
                float posX = x * TileSize;
                float posZ = z * TileSize;
                tile.Position = new Vector3(posX, 0, posZ);

                _boardVisuals.AddChild(tile);
            }
        }

        // Create a collider for the board to detect clicks
        var staticBody = new StaticBody3D();
        staticBody.Name = "BoardCollider";
        
        var shape = new BoxShape3D();
        shape.Size = new Vector3(BOARD_SIZE * TileSize, 0.1f, BOARD_SIZE * TileSize);
        
        var collider = new CollisionShape3D();
        collider.Shape = shape;
        staticBody.AddChild(collider);
        
        // Center the collider
        staticBody.Position = new Vector3((BOARD_SIZE * TileSize) / 2, -0.05f, (BOARD_SIZE * TileSize) / 2);
        _boardVisuals.AddChild(staticBody);
    }

    /// <summary>
    /// Get tile at grid position
    /// </summary>
    public BoardTile GetTile(Vector2I gridPosition)
    {
        if (IsValidPosition(gridPosition))
        {
            return _tiles[gridPosition.X, gridPosition.Y];
        }
        return null;
    }

    /// <summary>
    /// Check if a grid position is valid
    /// </summary>
    public bool IsValidPosition(Vector2I gridPosition)
    {
        return gridPosition.X >= 0 && gridPosition.X < BOARD_SIZE &&
               gridPosition.Y >= 0 && gridPosition.Y < BOARD_SIZE;
    }

    /// <summary>
    /// Place a character on a tile
    /// </summary>
    public bool PlaceCharacter(Vector2I gridPosition, string characterId)
    {
        if (!IsValidPosition(gridPosition))
            return false;

        if (_tiles[gridPosition.X, gridPosition.Y].OccupantCharacter != null)
            return false; // Tile already occupied

        _tiles[gridPosition.X, gridPosition.Y].OccupantCharacter = characterId;
        GD.Print($"BoardManager: Character {characterId} placed at {gridPosition}");
        return true;
    }

    /// <summary>
    /// Remove a character from a tile
    /// </summary>
    public bool RemoveCharacter(Vector2I gridPosition)
    {
        if (!IsValidPosition(gridPosition))
            return false;

        _tiles[gridPosition.X, gridPosition.Y].OccupantCharacter = null;
        return true;
    }

    /// <summary>
    /// Convert world position to grid position (simplified)
    /// </summary>
    public Vector2I WorldToGrid(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.X / TileSize);
        int z = Mathf.RoundToInt(worldPosition.Z / TileSize);
        return new Vector2I(x, z);
    }

    /// <summary>
    /// Convert grid position to world position
    /// </summary>
    public Vector3 GridToWorld(Vector2I gridPosition)
    {
        return new Vector3(gridPosition.X * TileSize, 0, gridPosition.Y * TileSize);
    }

    /// <summary>
    /// Set field type for a tile
    /// </summary>
    public void SetFieldType(Vector2I gridPosition, FieldType fieldType)
    {
        if (IsValidPosition(gridPosition))
        {
            _tiles[gridPosition.X, gridPosition.Y].FieldType = fieldType;
            GD.Print($"BoardManager: Field at {gridPosition} set to {fieldType}");
        }
    }
}
