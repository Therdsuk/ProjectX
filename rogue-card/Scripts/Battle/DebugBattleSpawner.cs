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
            // Read the class the player selected in the Main Menu
            string classId = lobby.GetMemberData(member, "class");
            var data = ClassRegistry.Get(classId);  // Defaults to Warrior if empty
            AddMockCardsToDeck(data, true);

            var player = new PlayerCharacter { Name = $"Player_{member.Name}", SteamId = member.Id.Value };
            player.InitialiseFromData(data);
            
            Board.AddChild(player);
            
            var preferredStart = new Vector2I(xOffset, 0);
            var actualStart = Board.GetNearestValidCell(preferredStart);
            
            if (member.Id == Steamworks.SteamClient.SteamId)
            {
                localPlayer = player;
                Manager.AddPlayer(localPlayer, actualStart);
            }
            else
            {
                remotePlayers.Add(player);
                remoteStartPositions.Add(actualStart);
            }
            
            xOffset += 2;
        }

        // Add remote players AFTER local player so the local player is always _players[0] for control
        for (int i = 0; i < remotePlayers.Count; i++)
        {
            Manager.AddPlayer(remotePlayers[i], remoteStartPositions[i]);
        }

        // Spawn enemies from the Inspector UnitsToSpawn list (entries where IsPlayer = false)
        var enemySetups = new List<DebugUnitSetup>();
        foreach (var setup in UnitsToSpawn)
        {
            if (!setup.IsPlayer) enemySetups.Add(setup);
        }

        // If no enemies configured in Inspector, fall back to one default enemy
        if (enemySetups.Count == 0)
        {
            // Default to a Goblin if no enemies configured
            var enemyData = EnemyRegistry.Get(EnemyRegistry.EnemyType.Goblin);
            SafeSpawnEnemy("Goblin", enemyData, new Vector2I(4, 4));
        }
        else
        {
            foreach (var setup in enemySetups)
            {
                var enemyData = ResolveEnemyData(setup);
                SafeSpawnEnemy(setup.EnemyType.ToString(), enemyData, setup.StartPos);
            }
        }

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
            if (setup.IsPlayer)
            {
                // Players: use OptionalDataOverride or a generic stub
                var data = setup.OptionalDataOverride;
                if (data == null)
                {
                    data = new CharacterData { ClassName = "Debug Player", BaseHp = setup.BaseHp, BaseMana = setup.BaseMana, HandSize = 5 };
                    AddMockCardsToDeck(data, true);
                }

                var player = new PlayerCharacter { Name = $"TestPlayer_{setup.StartPos}" };
                player.InitialiseFromData(data);
                Board.AddChild(player);
                
                var actualStart = Board.GetNearestValidCell(setup.StartPos);
                Manager.AddPlayer(player, actualStart);

                if (firstPlayer == null) firstPlayer = player;
            }
            else
            {
                // Enemies: resolve from EnemyTypeId > OptionalDataOverride > generic stub
                var enemyData = ResolveEnemyData(setup);
                SafeSpawnEnemy(setup.EnemyType.ToString(), enemyData, setup.StartPos);
            }
        }

        // --- Initialise Turn ---
        if (firstPlayer != null)
        {
            Manager.DebugInitializePlayerTurn(firstPlayer);
        }
    }

    /// <summary>
    /// Spawns an enemy at the requested cell. If that cell is already occupied,
    /// searches outward for the nearest free cell so multiple enemies with the
    /// same default StartPos (0,0) never silently fail.
    /// </summary>
    private void SafeSpawnEnemy(string label, CharacterData data, Vector2I preferredPos)
    {
        Vector2I spawnPos = Board.GetNearestValidCell(preferredPos);

        if (spawnPos != preferredPos)
        {
            GD.PushWarning($"[DebugBattleSpawner] '{label}': {preferredPos} was occupied/blocked → spawned at {spawnPos} instead.");
        }

        var enemy = new EnemyCharacter { Name = $"{label}_{spawnPos}", Data = data };
        Board.AddChild(enemy);   // _Ready fires here → calls InitialiseFromData exactly once
        Manager.AddEnemy(enemy, spawnPos);
    }

    /// <summary>
    /// Resolves CharacterData for an enemy setup entry.
    /// Priority: OptionalDataOverride > EnemyType (registry lookup) > anonymous stub.
    /// </summary>
    private CharacterData ResolveEnemyData(DebugUnitSetup setup)
    {
        if (setup.OptionalDataOverride != null)
            return setup.OptionalDataOverride;

        // Use the enum — always valid, no string parsing needed
        var data = EnemyRegistry.Get(setup.EnemyType);
        GD.Print($"[DebugBattleSpawner] Loaded enemy from registry: '{setup.EnemyType}'");
        return data;
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

        var jumpCard = new CardData {
            Id = "jump", Name = "Jump", Cost = 2, CardType = CardType.Move,
            Target = TargetType.AnyTileNoLoS, Range = 4, AoeShape = AreaOfEffect.SingleNode,
            RangeScalesWithStrength = true
        };

        data.StartingDeck.Add(strikeCard);
        data.StartingDeck.Add(shieldCard);
        data.StartingDeck.Add(heavyCard);
        data.StartingDeck.Add(fireballCard);
        data.StartingDeck.Add(jumpCard);
        data.StartingDeck.Add(strikeCard);
        data.StartingDeck.Add(fireballCard);
        data.StartingDeck.Add(heavyCard);
        data.StartingDeck.Add(jumpCard);
    }
}
