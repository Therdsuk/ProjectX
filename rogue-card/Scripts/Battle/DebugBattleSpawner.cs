using Godot;

/// <summary>
/// A playground script for debugging the BattleScene.
/// Attaches to a standalone DebugBattleSpawner node in BattleScene.tscn.
/// Spawns mock characters and decks if enabled.
/// </summary>
public partial class DebugBattleSpawner : Node
{
    [Export] public bool EnableDebugSpawn { get; set; } = true;

    [ExportGroup("Dependencies")]
    [Export] public BattleManager Manager { get; set; }
    [Export] public BattleBoard Board { get; set; }

    [ExportGroup("Units")]
    [Export] public Godot.Collections.Array<DebugUnitSetup> UnitsToSpawn { get; set; } = new();

    public override void _Ready()
    {
        if (!EnableDebugSpawn) return;
        
        // Ensure dependencies are assigned, else try to find them
        if (Manager == null) Manager = GetNodeOrNull<BattleManager>("../BattleManager");
        if (Board == null) Board = GetNodeOrNull<BattleBoard>("../BattleBoard");

        if (Manager == null || Board == null)
        {
            GD.PrintErr("[DebugBattleSpawner] Cannot spawn test units: Missing BattleManager or BattleBoard.");
            return;
        }

        // Delay spawning slightly to ensure the BattleBoard grid is fully instanced
        CallDeferred(MethodName.SpawnTestUnits);
    }

    private void SpawnTestUnits()
    {
        GD.Print($"[DebugBattleSpawner] Spawning {UnitsToSpawn.Count} test units...");

        PlayerCharacter firstPlayer = null;

        foreach (var setup in UnitsToSpawn)
        {
            var data = setup.OptionalDataOverride;
            
            // If no data resource was provided in inspector, stub one out
            if (data == null)
            {
                data = new CharacterData 
                { 
                    ClassName = setup.IsPlayer ? "Debug Player" : "Debug Enemy", 
                    BaseHp = setup.BaseHp, 
                    BaseMana = setup.BaseMana, 
                    HandSize = 5 
                };
                AddMockCardsToDeck(data, setup.IsPlayer);
            }

            if (setup.IsPlayer)
            {
                var player = new PlayerCharacter { Name = $"TestPlayer_{setup.StartPos}" };
                player.InitialiseFromData(data);
                
                var playerMesh = new MeshInstance3D { Mesh = new BoxMesh(), Position = new Vector3(0, 0.5f, 0) };
                player.AddChild(playerMesh);
                
                Board.AddChild(player);
                Manager.AddPlayer(player, setup.StartPos);

                if (firstPlayer == null) firstPlayer = player;
            }
            else
            {
                var enemy = new EnemyCharacter { Name = $"TestEnemy_{setup.StartPos}" };
                enemy.InitialiseFromData(data);
                
                var material = new StandardMaterial3D { AlbedoColor = new Color(1.0f, 0.0f, 0.0f) };
                var enemyMesh = new MeshInstance3D { Mesh = new CapsuleMesh { Material = material }, Position = new Vector3(0, 1.0f, 0) };
                enemy.AddChild(enemyMesh);
                
                Board.AddChild(enemy);
                Manager.AddEnemy(enemy, setup.StartPos);
            }
        }

        // --- Initialise Turn ---
        if (firstPlayer != null)
        {
            Manager.DebugInitializePlayerTurn(firstPlayer);
        }
    }

    private void AddMockCardsToDeck(CharacterData data, bool isPlayer)
    {
        data.StartingDeck = new Godot.Collections.Array<CardData>();
        
        var strikeCard = new CardData { 
            Id = "strike", Name = "Strike", Cost = 1, CardType = CardType.Battle, 
            Target = TargetType.SingleEnemy, Range = 1, AoeShape = AreaOfEffect.SingleNode, BaseDamage = 10
        };
        
        var shieldCard = new CardData { 
            Id = "shield", Name = "Defend", Cost = 1, CardType = CardType.Battle, 
            Target = TargetType.Self, Range = 0, AoeShape = AreaOfEffect.SingleNode, BaseHealing = 5
        };
        
        var heavyCard  = new CardData { 
            Id = "heavy", Name = "Heavy Blow", Cost = 2, CardType = CardType.Battle,
            Target = TargetType.AnyTile, Range = 3, AoeShape = AreaOfEffect.Square3x3, BaseDamage = 15
        };

        var fireballCard = new CardData {
            Id = "fireball", Name = "Fireball", Cost = 3, CardType = CardType.Battle,
            Target = TargetType.AnyTile, Range = 5, AoeShape = AreaOfEffect.Cross, BaseDamage = 20
        };

        data.StartingDeck.Add(strikeCard);
        data.StartingDeck.Add(shieldCard);
        data.StartingDeck.Add(heavyCard);
        data.StartingDeck.Add(fireballCard);
        data.StartingDeck.Add(strikeCard);
        data.StartingDeck.Add(fireballCard);
        data.StartingDeck.Add(heavyCard);
    }
}
