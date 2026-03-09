using Godot;
using System.Collections.Generic;

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
        var lobby = SteamManager.Instance?.CurrentLobby;
        
        if (lobby.HasValue)
        {
            GD.Print($"[DebugBattleSpawner] Spawning from Steam Lobby ({lobby.Value.MemberCount} members)...");
            SpawnLobbyUnits(lobby.Value);
        }
        else
        {
            GD.Print($"[DebugBattleSpawner] No Steam Lobby found. Spawning {UnitsToSpawn.Count} test units from Inspector...");
            SpawnInspectorUnits();
        }
    }

    private void SpawnLobbyUnits(Steamworks.Data.Lobby lobby)
    {
        PlayerCharacter localPlayer = null;
        var remotePlayers = new List<PlayerCharacter>();
        var remoteStartPositions = new List<Vector2I>();
        
        int xOffset = 0;

        foreach (var member in lobby.Members)
        {
            var data = new CharacterData 
            { 
                ClassName = member.Name, 
                BaseHp = 100, 
                BaseMana = 3, 
                HandSize = 5 
            };
            AddMockCardsToDeck(data, true);

            var player = new PlayerCharacter { Name = $"Player_{member.Name}", SteamId = member.Id.Value };
            player.InitialiseFromData(data);
            
            var playerMesh = new MeshInstance3D { Mesh = new BoxMesh(), Position = new Vector3(0, 0.5f, 0) };
            player.AddChild(playerMesh);
            
            Board.AddChild(player);
            
            var startPos = new Vector2I(xOffset, 0);
            
            if (member.Id == Steamworks.SteamClient.SteamId)
            {
                localPlayer = player;
                Manager.AddPlayer(localPlayer, startPos);
            }
            else
            {
                remotePlayers.Add(player);
                remoteStartPositions.Add(startPos);
            }
            
            xOffset += 2;
        }

        // Add remote players AFTER local player so the local player is always _players[0] for control
        for (int i = 0; i < remotePlayers.Count; i++)
        {
            Manager.AddPlayer(remotePlayers[i], remoteStartPositions[i]);
        }

        // Spawn one Test Enemy
        var enemyData = new CharacterData { ClassName = "Debug Enemy", BaseHp = 50, BaseMana = 3, HandSize = 5 };
        var enemy = new EnemyCharacter { Name = "TestEnemy_Center" };
        enemy.InitialiseFromData(enemyData);
        var material = new StandardMaterial3D { AlbedoColor = new Color(1.0f, 0.0f, 0.0f) };
        var enemyMesh = new MeshInstance3D { Mesh = new CapsuleMesh { Material = material }, Position = new Vector3(0, 1.0f, 0) };
        enemy.AddChild(enemyMesh);
        
        Board.AddChild(enemy);
        Manager.AddEnemy(enemy, new Vector2I(4, 4));

        if (localPlayer != null)
        {
            Manager.DebugInitializePlayerTurn(localPlayer);
        }
    }

    private void SpawnInspectorUnits()
    {
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
