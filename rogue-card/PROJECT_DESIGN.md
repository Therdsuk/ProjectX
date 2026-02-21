# Rogue Card - Multiplayer 3D Card-Based Roguelike Game

## Project Overview
Rogue Card is a multiplayer online 3D roguelike game with turn-based card combat on an isometric board. Players progress through randomly generated nodes (Adventure Map), encountering battles, cities, and other events.

## Game Architecture

### 1. Core Game Flow
- **Adventure Map**: Root selection leading to random nodes
- **Node Types**:
  - **Battle**: Card-based tactical combat (8x8 isometric board)
  - **City**: Hub node with randomized services (shop, exchange, quest, heal, revive)
  - **Events**: Random encounters and rewards

### 2. Character System
- **Class-based**: Different character classes define base stats and deck strategy
- **Stats** (influenced by class & accessories):
  - Health (HP)
  - Mana (MT)
  - Energy (EN)
  - Each class has unique stat scaling

### 3. Card System

#### Card Properties
- **Cost**: Resource required to play (energy/mana)
- **Effect**: What the card does
- **Range**: Distance the card can reach (0-8 tiles)
- **Speed**: Determines order of execution
  - **Burst**: Executes first (buffs, debuffs, utility)
  - **Fast**: Normal priority (single target attacks)
  - **Slow**: Executes last (AOE, heavy effects)
- **Type**: Determines when card can be used
  - **Move**: Used during Move Phase
  - **Attack**: Used during Battle Phase
  - **Buff**: Used during Battle Phase
  - **Setup**: Used during Setup Phase (traps, field effects)

#### Card Upgrades
- Cards can be upgraded to increase effectiveness
- Upgrades may improve cost, damage, range, or add effects

#### Deck System
- Each character class has a default deck
- Players can customize deck with acquired cards
- Deck must be optimized for synergy and class playstyle

### 4. Battle System

#### Board
- **8x8 isometric grid**
- Each tile has environmental properties
- Characters occupy single tiles

#### Environmental Field Types
- **Water**: Reduces fire damage, splash damage takes effect, reduced movement
- **Lava**: Continuous damage over time, increases burn damage, slows movement
- **Normal**: No special effects
- **Other Types** (expandable): Ice, Forest, Desert, etc.

#### Battle Phases (Round-based)
Each battle consists of multiple rounds. Each round has 3 phases:

1. **Move Phase**
   - Players move characters on the board
   - Play cards with type "Move"
   - Establish positioning

2. **Battle Phase**
   - Players play attack and buff cards
   - Card execution order determined by Speed stat + Card Speed
   - All players' cards execute simultaneously:
     - **Burst Speed** cards execute first
     - **Fast Speed** cards execute second
     - **Slow Speed** cards execute last
   - Buff cards affect all players and enemies

3. **Setup Phase**
   - Players play setup-type cards
   - Place traps on the board
   - Modify field effects
   - Prepare for next round

#### Combat Flow
1. Initialize battle with player characters and enemies
2. Each round:
   - Announce phase
   - Players take actions
   - Resolve all actions by speed
   - Apply environmental effects
   - Check win/lose conditions
3. Battle ends when all enemies or all players are defeated

### 5. Character Progression
- **Card Unlocks**: Discover new cards through battles and rewards
- **Card Upgrades**: Enhance existing cards
- **Stat Scaling**: Character level affects base stats
- **Accessories**: Equippable items that modify stats

### 6. Multiplayer System
- **Network Architecture**: Player sends actions to server
- **Synchronization**: All players' actions validated and executed server-side
- **Combat Resolution**: Server determines outcome and sends state to all clients
- **Session Management**: Players join/leave adventure maps

## Folder Structure

```
/Scripts/
  /Core/              # Core game systems
    GameManager.cs
    BattleManager.cs
    PhaseManager.cs
  /Battle/            # Battle-specific systems
    BattleScene.cs
    BoardManager.cs
    FieldManager.cs
  /Cards/             # Card system
    Card.cs
    CardManager.cs
    Deck.cs
    CardEffect.cs
  /Characters/        # Character system
    Character.cs
    CharacterClass.cs
    CharacterStats.cs
  /City/              # City/Hub systems
    CityManager.cs
    Shop.cs
    Healer.cs
  /Map/               # Adventure map
    MapManager.cs
    NodeType.cs
  /Network/           # Multiplayer/Networking
    NetworkManager.cs
    PlayerSync.cs

/Resources/
  /Cards/             # Card data files
    Base/
    Fire/
    Water/
    etc.
  /Characters/        # Character/Class data
    Warrior/
    Mage/
    Rogue/
  /Enemies/           # Enemy data
    Minion/
    Boss/

/Scenes/
  /Battle/
    BattleScene.tscn  # Main battle scene
    Board.tscn
    UI/
  /City/
    CityHub.tscn
    Shop.tscn
  /Map/
    AdventureMap.tscn
  /UI/
    MainMenu.tscn
    HUD.tscn
    CardUI.tscn

/Assets/
  /Art/
    /Cards/           # Card artwork
    /Characters/      # Character models
    /Enemies/         # Enemy models
    /Environment/     # Environmental models
    /Fields/          # Field effect materials
    /Tiles/           # Board tile models (isometric)
    /UI/              # UI elements
  /Audio/
    /Music/
    /SFX/
  /Fonts/

/Tests/               # Unit tests for game systems
```

## Technical Stack
- **Engine**: Godot 4.x with .NET support
- **Language**: C# (GDScript optional for specific systems)
- **Networking**: Godot Multiplayer API
- **3D**: Isometric view with 3D models
- **Database**: Server-side persistence (not in scope for initial MVP)

## Development Phases

### Phase 1: Foundation (Current)
- [x] Project structure setup
- [ ] Basic battle scene with empty board
- [ ] Phase system (Move, Battle, Setup)
- [ ] Player character placement
- [ ] Phase transitions

### Phase 2: Core Battle System
- [ ] Card system implementation
- [ ] Deck management
- [ ] Character stat system
- [ ] Basic AI for enemy turns

### Phase 3: Environmental System
- [ ] Field types
- [ ] Field effects (damage, movement penalties)
- [ ] Board generation with random fields

### Phase 4: Advanced Features
- [ ] Multiplayer networking
- [ ] Adventure map with nodes
- [ ] City hub systems
- [ ] Card upgrades and progression

### Phase 5: Polish
- [ ] Animations and VFX
- [ ] Sound design
- [ ] UI/UX refinement
- [ ] Performance optimization

## Data Structures

### Card Data
```csharp
public class CardData
{
    public string Id;
    public string Name;
    public int Cost;
    public CardSpeed Speed; // Burst, Fast, Slow
    public CardType Type;   // Move, Attack, Buff, Setup
    public int Range;
    public string EffectId;
    public int Damage;
    public int UpgradeLevel;
}
```

### Character Data
```csharp
public class CharacterData
{
    public string Id;
    public string Name;
    public CharacterClass Class;
    public int Level;
    public int Health;
    public int Mana;
    public int Energy;
    public List<CardData> Deck;
}
```

### Board Tile Data
```csharp
public class BoardTile
{
    public Vector2 GridPosition;
    public FieldType FieldType;
    public Character OccupantCharacter;
    public List<CardEffect> ActiveEffects;
}
```

## Next Steps
1. Set up basic battle scene with empty isometric board
2. Implement phase system and phase transitions
3. Add player character spawning and positioning
4. Create card system foundation
5. Begin network integration

## Notes for AI Collaboration
- This is a turn-based system with deterministic gameplay
- All actions must be validated server-side for multiplayer integrity
- Speed determines card execution order (not player order)
- Environmental effects are persistent until cleared
- Cards affecting "all players" means all entities in battle (player characters, allies, enemies)
