# AI Project Context - Rogue Card

This document provides comprehensive context about the Rogue Card project for AI systems to understand the game design, architecture, and development direction.

## Executive Summary

**Project**: Rogue Card - Multiplayer 3D Card-Based Roguelike
**Engine**: Godot Engine (4.x) with C# scripting
**Status**: Foundation Phase - Basic battle scene framework complete
**Goal**: Create a turn-based tactical card game with roguelike progression elements

## Game Concept

### Core Gameplay Loop
1. **Adventure Phase**: Player navigates a roguelike map with random nodes
2. **City Phase**: Visit hubs to purchase cards, heal, and take quests
3. **Battle Phase**: Card-based tactical combat on an 8x8 isometric board
4. **Progression**: Gather cards and upgrades to build stronger decks
5. **Repeat**: Continue until death or victory

### Battle System
- **Turn Structure**: Rounds with 3 sequential phases (Move → Battle → Setup)
- **Board**: 8x8 grid with isometric view and environmental effects
- **Cards**: Class-based decks (Warrior, Mage, Rogue) with upgradeable effects
- **Stats**: Health, Mana, Energy determined by class and equipment
- **Card Execution**: Ordered by Speed (Burst → Fast → Slow), not player turn order
- **Environment**: Field types (Water, Lava, etc.) with gameplay effects

### Roguelike Elements
- Random map generation with node types (Battle, City, Boss, Event)
- Procedurally generated enemies with random decks
- Cards and upgrades found as rewards
- Run-based progression (die = restart)
- Scaling difficulty based on progression

## Architecture Overview

### System Organization

```
Core
├── GameEnums.cs          # All enums used throughout
├── PhaseManager.cs       # Battle phase management
└── [Future] GameManager.cs

Battle
├── BattleManager.cs      # Battle orchestration
├── BoardManager.cs       # 8x8 grid management
├── BattleSceneSetup.cs   # Scene initialization
├── BattleUIManager.cs    # UI display/controls
├── BoardInteraction.cs   # [Future] Click/movement
├── CardPlaySystem.cs     # [Future] Card execution
├── CombatResolver.cs     # [Future] Damage/effects
└── FieldSystem.cs        # [Future] Environmental effects

Characters
├── Character.cs          # Character instances [Partial]
├── CharacterClass.cs     # Class definitions [Partial]
├── CharacterStats.cs     # [Future] Stat calculation
├── StatusEffect.cs       # [Future] Buffs/debuffs
├── Accessory.cs          # [Future] Equipment system
└── EquipmentManager.cs   # [Future] Equip logic

Cards
├── CardData.cs           # Card definitions [Partial]
├── CardEffect.cs         # [Future] Effect system
├── Deck.cs               # [Future] Deck management
├── CardDatabase.cs       # [Future] Card data store
└── CardUI.cs             # [Future] Card display

Map
├── MapManager.cs         # [Future] Map generation
├── MapNode.cs            # [Future] Node data
└── MapGenerator.cs       # [Future] Generation algorithm

City
├── CityManager.cs        # [Future] Hub orchestration
├── Shop.cs               # [Future] Store system
├── Healer.cs             # [Future] Healing service
├── QuestSystem.cs        # [Future] Quest management
└── Exchange.cs           # [Future] Card exchange

Network
├── NetworkManager.cs     # [Future] Connection handling
├── NetworkMessages.cs    # [Future] Message protocol
├── PlayerSync.cs         # [Future] State sync
├── ServerValidation.cs   # [Future] Server-side logic
└── ClientPrediction.cs   # [Future] Client prediction
```

### Data Flow During Battle

```
Player Input
    ↓
BattleManager
    ├→ PhaseManager (validate phase)
    ├→ BoardManager (track positions)
    ├→ Character (check resources)
    └→ CardPlaySystem (execute card)
         ├→ DamageCalculator (compute damage)
         ├→ EffectResolver (apply effects)
         ├→ FieldSystem (apply field effects)
         └→ Character (update state)
              ↓
UI Update (BattleUIManager)
    ↓
Network Broadcast (multiplayer)
```

## Key Game Systems

### Phase System
The battle is organized into phases with specific allowed actions:

| Phase | Purpose | Allowed Actions | Card Types |
|-------|---------|-----------------|-----------|
| Move | Positioning | Move characters, play move cards | Move |
| Battle | Combat | Attack, buff/debuff | Attack, Buff |
| Setup | Preparation | Place traps, change fields | Setup |

Actions are executed based on card speed (Burst fastest, Slow slowest).

### Card System
Cards are core to gameplay:

**Card Properties**:
- Cost (Energy/Mana to play)
- Speed (Burst/Fast/Slow = execution order)
- Type (Move/Attack/Buff/Setup)
- Range (tiles from caster)
- Effect (what it does)

**Card Upgrades**:
- Enhance damage
- Reduce cost
- Increase range
- Add secondary effects

**Deck Management**:
- Start with class deck
- Can add up to 30 cards total
- Max 3 copies of each card

### Character System
Characters have class-based stats and can be customized:

**Base Classes**:
- Warrior: High health, low mana
- Mage: High mana, low health  
- Rogue: Balanced, high speed

**Stats** (affected by level, equipment):
- Health (HP)
- Mana (MT) 
- Energy (EN per round)

**Progression**:
- Level up to increase stats
- Equip accessories for bonuses
- Unlock new cards through rewards

### Board System
8x8 isometric grid for tactical positioning:

**Features**:
- Character occupancy (only one per tile)
- Field types (Water, Lava, Ice, etc.)
- Environmental effects (damage reduction, damage increases)
- Range calculation for abilities
- Pathfinding for character movement

## Current Status

### Implemented ✅
- [x] Core system structure and organization
- [x] Godot project setup with .NET support
- [x] Phase manager (Move → Battle → Setup cycles)
- [x] Board manager (8x8 grid, occupancy tracking)
- [x] Battle UI framework (phase display, controls)
- [x] Placeholder scripts for all major systems
- [x] Comprehensive documentation

### In Development 🔄
- [ ] Battle scene setup in Godot editor
- [ ] Character system implementation
- [ ] Card system with effects
- [ ] Board interactions (click, movement)
- [ ] Combat resolution

### Planned 📋
- [ ] Field type effects
- [ ] Enemy AI
- [ ] Multiplayer networking
- [ ] Adventure map generation
- [ ] City hub systems
- [ ] Animations and VFX
- [ ] Animation and polish

## Development Guidelines

### Code Structure
- Use C# for game logic (performance, type safety)
- Encapsulate systems into clear classes
- Use Godot signals for system communication
- Keep systems loosely coupled through interfaces

### Naming Conventions
- Classes: PascalCase (`BattleManager`)
- Methods: PascalCase (`AdvancePhase()`)
- Variables: camelCase (`currentPhase`)
- Constants: UPPER_SNAKE_CASE (`BOARD_SIZE = 8`)
- Scenes: snake_case (`battle_scene.tscn`)

### Important Patterns
- Use **enums** for fixed values (phases, card types, speeds)
- Use **signals** to communicate between systems asynchronously
- Use **data classes** to transfer information between systems
- Use **managers** to orchestrate related functionality
- Document complex algorithms with comments

### Performance Focus Areas
- Minimize allocations in `_Process()` methods
- Cache frequently accessed references
- Use object pooling for rapid creation/destruction
- Profile with Godot profiler to identify bottlenecks

## Integration Points for AI

### When Implementing New Features

1. **Understand the existing system**
   - Review the system's manager class
   - Check signal definitions
   - Review data structures

2. **Follow established patterns**
   - Use same naming conventions
   - Mirror signal structure
   - Keep similar code organization

3. **Minimize coupling**
   - Work through interfaces, not direct dependencies
   - Use signals, not direct method calls when possible
   - Keep systems self-contained

4. **Document thoroughly**
   - Add XML comments to public methods
   - Explain complex algorithms
   - Note important signals
   - Document any assumptions

5. **Provide debug output**
   - Use `GD.Print()` for important events
   - Use `GD.PrintErr()` for errors
   - Enable debug mode when developing

## Common AI Tasks

### Adding a New Card Type
1. Define card in `CardDatabase.cs`
2. Create effect class in `Cards/Effects/`
3. Implement ICardEffect interface
4. Register effect in `CardPlaySystem.cs`
5. Test with specific phase requirements

### Adding a New Field Type
1. Add to `FieldType` enum in `GameEnums.cs`
2. Create effect class in `Battle/FieldEffects/`
3. Implement in `FieldSystem.cs`
4. Add visual representation
5. Test gameplay impact

### Adding a New Character Class
1. Define in `CharacterClass.cs`
2. Create starting deck in `CardDatabase.cs`
3. Set base stats in `CharacterClass.cs`
4. Implement class-specific bonuses in `CharacterStats.cs`
5. Create character visuals

### Implementing a System Feature
1. Define data structures (use existing patterns)
2. Create manager class
3. Add initialization in `_Ready()`
4. Add signal definitions
5. Implement core logic
6. Add debug output
7. Write comments for complex parts
8. Test with sample data
9. Commit with clear message

## Testing Approach

### Manual Testing
- Run `Scenes/Battle/BattleScene.tscn` 
- Use Phase controls to advance through battle
- Check console output for debug messages
- Verify board visuals and phase transitions

### Automated Testing (Future)
- Unit tests for calculators (damage, stats)
- Integration tests for phase transitions
- Battle scenario tests

## Collaboration Notes

### Before Starting Work
1. Check TASKS.md for available tasks
2. Review task dependencies
3. Read relevant system documentation
4. Ask if unclear about requirements

### During Development
1. Build and test locally first
2. Create feature branch for changes
3. Commit frequently with descriptive messages
4. Test multiplayer considerations early

### When Complete
1. Verify no build errors
2. Update documentation if needed
3. Create pull request with description
4. Request code review before merging

## Performance Targets

- Battle scene load: < 2 seconds
- Phase transition: Instant (< 50ms)
- Card execution: < 100ms per card
- Board interaction (click): < 50ms response
- Network update: < 100ms round trip

## Security Considerations

### Client-Side Cheats to Prevent
- Prevent invalid phase transitions
- Validate card plays server-side
- Verify character positions
- Validate resource consumption
- Check action timing

### Network Security
- Validate all player actions server-side
- Use checksums for critical data
- Implement anti-cheat detection
- Rate-limit player inputs

## References

- **PROJECT_DESIGN.md**: Complete game design specification
- **DEVELOPER_GUIDE.md**: Architecture and development patterns
- **BATTLE_SCENE_SETUP.md**: Scene creation instructions
- **README.md**: Setup and git workflow
- **TASKS.md**: Development task breakdown

## Key Metrics for Success

- **Code Quality**: Follows naming conventions, well-documented
- **Performance**: Maintains 60 FPS during battle
- **Usability**: Intuitive UI for card playing
- **Stability**: No crashes during normal play
- **Balance**: Cards feel equally strong for their cost
- **Engagement**: Combat feels tactical and rewarding

## Next Priorities

1. **Immediate**: Complete battle scene in Godot editor
2. **This Week**: Character system implementation  
3. **Next Week**: Card system with basic effects
4. **Following Week**: Combat resolution and board interactions

## Contact/Questions

- For game design: See PROJECT_DESIGN.md
- For technical: See DEVELOPER_GUIDE.md  
- For setup: See README.md
- For tasks: See TASKS.md

---

**Project Created**: February 21, 2026
**Last Updated**: February 21, 2026
**Intended Audience**: AI systems and human developers
**Status**: Foundation Phase - Ready for Active Development
