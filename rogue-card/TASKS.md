# Development Tasks - Rogue Card

## Overview
This document outlines the key tasks needed to bring Rogue Card from foundation to playable MVP. Tasks are organized by system and priority.

## Legend
- 🔴 **High Priority** - Blocks other work
- 🟡 **Medium Priority** - Should do soon
- 🟢 **Low Priority** - Nice to have
- ✅ **Completed** - Finished task
- 🔄 **In Progress** - Currently being worked on

---

## Phase 1: Foundation (CURRENT)
**Status**: ~40% Complete
**Goal**: Establish project infrastructure and basic battle scene

### Completed ✅
- [x] Project structure and folder organization
- [x] Game design documentation (PROJECT_DESIGN.md)
- [x] Battle scene setup guide (BATTLE_SCENE_SETUP.md)
- [x] Phase system implementation
- [x] Board manager (8x8 grid)
- [x] Battle UI framework
- [x] README and git workflow documentation

### In Progress 🔄
- [ ] Create actual battle scene in Godot (*Currently waiting for co-worker*)

### Remaining 🔴
- [ ] Build project in Godot and verify no errors
- [ ] Test battle scene - verify board displays correctly
- [ ] Test phase advancement - verify all phases work
- [ ] Push to git repository

---

## Phase 2: Core Systems (NEXT)

### Character System 🔴
**Priority**: High (needed for battles)
**Estimated Time**: 3-4 days
**Depends On**: Core systems

#### Tasks:
- [ ] Implement character class initialization (Warrior, Mage, Rogue)
- [ ] Implement base stat calculation system
- [ ] Create character stat modifiers (accessories, level-based)
- [ ] Implement status effect system (buffs, debuffs, duration)
- [ ] Create character persistence (save/load)
- [ ] Add character visual representation on board
- [ ] Connect character to board occupancy system

**Files to Modify/Create**:
- Character.cs - Complete implementation
- CharacterStats.cs - New file for stat calculations
- Scripts/Characters/StatusEffect.cs - New file for effects

### Card System 🔴
**Priority**: High (core gameplay)
**Estimated Time**: 5-6 days
**Depends On**: Character system (partially)

#### Tasks:
- [ ] Define card effect interface and base classes
- [ ] Implement card properties and upgrades
- [ ] Create deck management system
- [ ] Implement deck validation (max copies, total cards)
- [ ] Create card database structure
- [ ] Implement card UI display
- [ ] Create sample cards (5-10 for each class)
- [ ] Connect deck to character class

**Files to Modify/Create**:
- CardData.cs - Complete with card effects
- Scripts/Cards/CardEffect.cs - New file
- Scripts/Cards/CardDatabase.cs - New file
- Scripts/Cards/CardUI.cs - New file

---

## Phase 2B: Battle Mechanics (Parallel with Phase 2)

### Phase Manager Enhancement 🟡
**Priority**: Medium (needed before combat)
**Estimated Time**: 1-2 days
**Depends On**: Phase system (done)

#### Tasks:
- [ ] Add phase timer/timeout system
- [ ] Implement auto-phase advancement
- [ ] Add phase pause/reset functionality
- [ ] Create phase summary system

**Files to Modify**:
- Scripts/Core/PhaseManager.cs

### Board Interactions 🔴
**Priority**: High (needed for movement)
**Estimated Time**: 3-4 days
**Depends On**: Character system, Board system, Phase system

#### Tasks:
- [ ] Implement click detection on board tiles
- [ ] Implement character movement validation
- [ ] Implement pathfinding (A* or similar)
- [ ] Implement movement range display
- [ ] Connect board to phase system (movement in Move phase only)
- [ ] Add visual feedback (highlight valid tiles, movement path)

**Files to Create**:
- Scripts/Battle/BoardInteraction.cs - New file
- Scripts/Battle/PathFinding.cs - New file

### Card Playing System 🔴
**Priority**: High (core gameplay)
**Estimated Time**: 4-5 days
**Depends On**: Card system, Phase system, Character system

#### Tasks:
- [ ] Implement card play validation (cost, phase, range)
- [ ] Implement card targeting system
- [ ] Create card execution queue system
- [ ] Implement speed-based card ordering (Burst → Fast → Slow)
- [ ] Create card resolution system
- [ ] Connect card plays to phase system

**Files to Create**:
- Scripts/Battle/CardPlaySystem.cs - New file
- Scripts/Battle/CardExecutionQueue.cs - New file

---

## Phase 3: Gameplay Loop (LATER)

### Combat Resolution System 🔴
**Priority**: High
**Estimated Time**: 4-5 days
**Depends On**: Card system, Character system, Board interactions

**Tasks**:
- [ ] Implement damage calculation
- [ ] Implement effect resolution
- [ ] Implement buff/debuff application during battle
- [ ] Implement field-based damage (lava, water, etc.)
- [ ] Create win/lose condition detection
- [ ] Implement battle summary/rewards

**Files to Create**:
- Scripts/Battle/CombatResolver.cs - New file
- Scripts/Battle/DamageCalculator.cs - New file
- Scripts/Battle/EffectResolver.cs - New file

### Field System 🟡
**Priority**: Medium
**Estimated Time**: 2-3 days
**Depends On**: Board system

**Tasks**:
- [ ] Implement field types (Water, Lava, Ice, etc.)
- [ ] Implement field effects (damage, movement penalties)
- [ ] Create field transition effects
- [ ] Implement field visual representation
- [ ] Add field type to board generation

**Files to Create**:
- Scripts/Battle/FieldSystem.cs - New file
- Scripts/Battle/FieldEffects.cs - New file

### Equipment/Accessory System 🟡
**Priority**: Medium
**Estimated Time**: 2-3 days
**Depends On**: Character system

**Tasks**:
- [ ] Define accessory data structure
- [ ] Implement equipment slots
- [ ] Implement stat modification from accessories
- [ ] Create equipment UI
- [ ] Implement equipment save/load

**Files to Create**:
- Scripts/Characters/Accessory.cs - New file
- Scripts/Characters/EquipmentManager.cs - New file

---

## Phase 4: Content Systems (LATER)

### Adventure Map System 🟡
**Priority**: Medium
**Estimated Time**: 3-4 days
**Depends On**: Battle system (basic), City system (basic)

**Tasks**:
- [ ] Implement map generation algorithm
- [ ] Create node types and spawning
- [ ] Implement node transitions
- [ ] Create map visualization
- [ ] Implement node difficulty scaling

**Files to Create/Modify**:
- Scripts/Map/MapManager.cs - Implement core logic
- Scripts/Map/MapNode.cs - New file
- Scripts/Map/MapGenerator.cs - New file

### City Hub System 🟡
**Priority**: Medium
**Estimated Time**: 4-5 days
**Depends On**: Card system, Character system

**Tasks**:
- [ ] Implement shop system with card purchasing
- [ ] Implement card exchange/crafting
- [ ] Implement healing service
- [ ] Implement revive service
- [ ] Implement quest system (basic)
- [ ] Create city UI
- [ ] Randomize services availability (not all always present)

**Files to Create/Modify**:
- Scripts/City/CityManager.cs - Implement
- Scripts/City/Shop.cs - Implement
- Scripts/City/Healer.cs - New file
- Scripts/City/QuestSystem.cs - New file

### Procedural Generation 🟡
**Priority**: Medium
**Estimated Time**: 2-3 days
**Depends On**: Card system, Map system

**Tasks**:
- [ ] Generate random enemy decks
- [ ] Generate random equipment/accessories
- [ ] Generate random card rewards
- [ ] Implement difficulty scaling

**Files to Create**:
- Scripts/Core/ProcGen.cs - Procedural generation utilities
- Scripts/Core/RewardGenerator.cs - New file

---

## Phase 5: Multiplayer (LATER)

### Network Foundation 🔴
**Priority**: High (design before implementation)
**Estimated Time**: 5-7 days
**Depends On**: All battle systems

**Design Tasks (Do First)**:
- [ ] Design network message protocol
- [ ] Design game state synchronization strategy
- [ ] Design server validation architecture
- [ ] Design client-side prediction strategy

**Implementation Tasks**:
- [ ] Implement network manager base system
- [ ] Implement player connection/disconnection handling
- [ ] Implement basic state synchronization
- [ ] Implement action validation server-side

**Files to Create/Modify**:
- Scripts/Network/NetworkManager.cs - Implement
- Scripts/Network/NetworkMessages.cs - New file
- Scripts/Network/PlayerSync.cs - Implement
- Scripts/Network/ServerValidation.cs - New file

### Multiplayer Integration 🟡
**Priority**: Medium
**Estimated Time**: 3-4 days
**Depends On**: Network foundation, Battle system

**Tasks**:
- [ ] Connect battle system to network
- [ ] Implement turn order for multiple players
- [ ] Implement simultaneous action validation
- [ ] Implement reconnection handling

**Files to Modify**:
- Scripts/Battle/BattleManager.cs - Network integration
- Scripts/Core/PhaseManager.cs - Network sync

---

## Phase 6: Polish (LATER)

### Animation System 🟢
**Priority**: Low (can wait)
**Estimated Time**: 4-5 days

**Tasks**:
- [ ] Create character animation controller
- [ ] Implement move animations
- [ ] Implement attack animations
- [ ] Implement spell/buff animations
- [ ] Implement death animation

### Visual Effects 🟢
**Priority**: Low (can wait)
**Estimated Time**: 3-4 days

**Tasks**:
- [ ] Create damage number system
- [ ] Create spell effect VFX
- [ ] Create field effect visuals
- [ ] Create collision/hit feedback

### Audio System 🟢
**Priority**: Low (can wait)
**Estimated Time**: 2-3 days

**Tasks**:
- [ ] Implement music system
- [ ] Implement SFX system
- [ ] Create sound cues for actions
- [ ] Implement audio settings

### UI Polish 🟢
**Priority**: Low (can wait)
**Estimated Time**: 2-3 days

**Tasks**:
- [ ] Create main menu
- [ ] Create pause menu
- [ ] Create settings menu
- [ ] Polish battle UI
- [ ] Add tooltips and help text

---

## Task Assignment Guide

### For Different Skill Levels

#### Beginners 🟢
Start with these tasks to learn the system:
- [ ] Create 5 new sample cards
- [ ] Implement simple field effects
- [ ] Add visual tweaks to UI
- [ ] Write documentation/comments

#### Intermediate 🟡
Good tasks for experienced developers:
- [ ] Implement card effects system
- [ ] Create board interaction system
- [ ] Implement character stats calculator
- [ ] Create equipment system

#### Advanced 🔴
Complex systems requiring deep knowledge:
- [ ] Implement network synchronization
- [ ] Design and implement procedural generation
- [ ] Optimize battle performance
- [ ] Design multiplayer architecture

---

## Task Dependencies Map

```
Core Systems ✅
  ↓
Character System → Card System
  ↓                    ↓
Board Interactions → Card Playing System
  ↓
Combat Resolution → Field System
  ↓
Equipment System
  ↓
Adventure Map → City System
  ↓
Network Foundation
  ↓
Multiplayer Integration
  ↓
Animation/VFX/Audio
```

---

## Suggested Work Streams (Parallel Development)

### Stream 1: Battle Core (3+ developers)
1. Complete character system
2. Complete card system
3. Implement card playing
4. Implement combat resolution
5. Test battle system

### Stream 2: Content (2+ developers)
1. Create base enemy encounters
2. Create starting decks per class
3. Create accessory/equipment data
4. Create sample campaigns/maps

### Stream 3: Infrastructure (1-2 developers)
1. Design network protocol
2. Implement network manager skeleton
3. Set up multiplayer testing framework
4. Create server validation architecture

### Stream 4: Polish (1-2 developers)
1. Create animations for actions
2. Add VFX
3. Polish UI
4. Add sound

---

## Weekly Checklist Template

```
Week of: [Date]

Character System:
- [ ] Task 1 - Status: In Progress
- [ ] Task 2 - Status: Not Started

Card System:
- [ ] Task 1 - Status: Completed ✅
- [ ] Task 2 - Status: In Progress

Combat:
- [ ] Task 1 - Status: Blocked (waiting on cards)

Issues/Blockers:
- Issue 1: [Description]
- Blocker 1: [What's blocking and solution]

Commits:
- [commit message 1]
- [commit message 2]
```

---

## Quick Start for New Team Members

1. Read **PROJECT_DESIGN.md** - Understand the game
2. Read **DEVELOPER_GUIDE.md** - Understand architecture
3. Build the project - Follow README.md
4. Run the battle scene - See what exists
5. Pick a task from "Phase 2" based on your skill level
6. Ask questions - We're all learning!

---

## Questions?

- **Architecture Questions**: Check DEVELOPER_GUIDE.md
- **Game Design Questions**: Check PROJECT_DESIGN.md
- **Technical Issues**: Check BATTLE_SCENE_SETUP.md
- **Git/Workflow Questions**: Check README.md
- **Not answered?**: Ask the team lead!

---

Last Updated: February 21, 2026
Ready for team collaboration!
