# RogueCard — Project Design Document

> **Tech Stack:** Godot 4.x (.NET / C#)  
> **Genre:** Roguelike + Card-Based Tactical RPG  
> **Status:** In Development

---

## 1. Overview

RogueCard is a roguelike card-based tactical RPG built in Godot .NET (C#).  
The player chooses a class, then travels through a procedurally generated node map selecting routes through different encounter types. Combat is played out on a chess-board-style battlefield where cards are played each round to move, attack, buff/debuff, or set up traps.

---

## 2. Map & Node System

The overworld is a directed graph (a "node map") where the player selects their route through different node types.

### Node Types

| Node Type | Description |
|-----------|-------------|
| **Battle**  | Triggers a card-based combat encounter |
| **City**    | A hub node that may contain one or more sub-nodes (see below) |
| **Shop**    | Buy/sell cards and items |
| **Exchange** | Trade cards with the merchant or other effects |
| **Quest**   | Accept or complete a quest for rewards |
| **Heal**    | Restore HP (and possibly Mana/Energy) |
| **Revive**  | Resurrect a fallen character or restore a major resource |

> **Important:** City nodes are the parent container. Each City has a **random subset** of the sub-types above — not every sub-type is guaranteed to appear in every city. Each sub-type can also appear as a **standalone** node on the route (outside a city).

### Map Generation Rules
- *(Deferred — overworld map is not yet implemented. Battle uses a clean flat board.)*
- A **Boss Battle** node always appears at the end of each chapter/act.

---

## 3. Player & Classes

Each player character belongs to a **Class** that determines:
- Starting stats (HP, Mana, Energy)
- Starting card deck composition
- Stat growth per level
- Visual appearance

### Core Stats

| Stat | Description |
|------|-------------|
| **HP** | Health Points — reaches 0 = defeated |
| **Mana** | Spent to play most cards |
| **Energy** | Secondary resource for special cards/abilities |
| **Speed** | Affects card activation order in battle |
| **Defense** | Reduces incoming damage |
| **Attack** | Base damage modifier |

Stats can also be modified by **Accessories** equipped to the character.

---

## 4. Card System

### Card Structure (Data Model)

```
Card {
    Id          : string        // Unique identifier
    Name        : string        // Display name
    Description : string        // Effect description
    CardType    : CardType      // Move | Battle | Setup | Buff | Debuff
    Cost        : int           // Mana cost to play
    Effect      : EffectData    // What the card does
    Range       : int           // Cells affected / range of action
    Speed       : CardSpeed     // Burst | Fast | Slow
    UpgradeLevel: int           // 0 = base, 1+ = upgraded
}
```

### Card Types

| Type | Phase Used | Description |
|------|-----------|-------------|
| **Move**   | Move Phase  | Moves a character or shifts board presence |
| **Battle** | Battle Phase | Attack, skill, or direct damage cards |
| **Buff**   | Battle Phase | Positive status effects for allies |
| **Debuff** | Battle Phase | Negative status effects on enemies |
| **Setup**  | Setup Phase  | Traps, field-change effects, persistent auras |

### Card Speed (Activation Order)

| Speed | Priority | Examples |
|-------|----------|---------|
| **Burst** | 1st (highest) | Buffs, debuffs, instant reactions |
| **Fast**  | 2nd | Normal single-target attacks |
| **Slow**  | 3rd (lowest) | Heavy AoE attacks, powerful effects |

Within the same speed tier, the character's **Speed stat** determines order.  
Cards are collected in a shared activation queue sorted by speed tier → character speed.

### Card Deck
- Each class has a **base deck** of cards matching their archetype.
- The deck is shuffled at the start of each battle.
- Players draw a fixed hand size per round.
- Cards can be **upgraded** (via Shop, Exchange, or Quest rewards) — upgrading changes cost, effect power, range, or speed.

---

## 5. Battle System

### 5.1 Battlefield

The battlefield is a **clean flat grid** (checkerboard pattern) surrounded by decorative **environment art** (trees, rocks, bushes). All playable cells are at the same elevation for tactical clarity.

Field types can be added to specific cells in future milestones:

| Field Type | Effect |
|-----------|--------|
| **Normal**  | No modifier |
| **Water**   | Reduces Fire damage; Thunder damage splashes to adjacent cells |
| **Lava**    | Deals passive damage each round to units standing on it |
| **Forest**  | Increases Defense; reduces Movement range |
| **Ice**     | Chance to Freeze on contact; slippery movement |
| **Sand**    | Reduces Speed; no special damage interaction |

> **Note:** The battlefield is currently all Normal cells. Field types will be added as designed encounters in future milestones.

### 5.2 Battle Initialization

When the player enters a Battle node:
1. The scene switches to the **Battle Scene**.
2. The board is generated (plain grid for now).
3. Player character(s) are placed on one side; enemies on the other.
4. HP / Mana / Energy are set based on class stats + accessories.
5. Both sides draw their opening hands.

### 5.3 Round Structure

Each battle round has **3 phases** executed in order:

---

#### Phase 1 — Move Phase
- Players may **move** their character(s) up to their movement range.
- Players may play **Move-type cards** (e.g., dash, teleport, swap).
- Enemies also execute movement AI.

---

#### Phase 2 — Battle Phase
- Players play **Battle, Buff, or Debuff cards** from their hand.
- Enemies play their cards (AI-driven).
- All played cards go into a **Activation Queue**, sorted by:
  1. Speed tier (Burst → Fast → Slow)
  2. Character Speed stat (higher goes first within same tier)
- Cards resolve one by one from the queue.

---

#### Phase 3 — Setup Phase
- Players play **Setup-type cards** (traps, field changes, auras).
- Setup cards persist on the battlefield and trigger under defined conditions.
- Players draw new cards to refill hand (up to max hand size).
- Status effects tick down.

---

### 5.4 Battle End Conditions
- **Victory:** All enemies defeated.
- **Defeat:** All player characters at 0 HP.

On victory: reward screen (cards, gold, etc.), return to the node map.

---

## 6. Project Architecture

```
rogue-card/
├── PROJECT.md                  ← This document
├── project.godot
├── Scenes/
│   ├── Battle/
│   │   ├── BattleScene.tscn    ← Main battle scene
│   │   ├── BattleBoard.tscn    ← Grid/board component
│   │   ├── BattleHUD.tscn      ← Phase UI, HP bars, hand display
│   │   └── CardDisplay.tscn    ← Individual card in hand
│   ├── Map/
│   │   ├── MapScene.tscn       ← Overworld node map
│   │   └── NodeIcon.tscn       ← Individual map node icon
│   ├── City/
│   │   ├── CityScene.tscn
│   │   ├── ShopScene.tscn
│   │   ├── ExchangeScene.tscn
│   │   ├── QuestScene.tscn
│   │   ├── HealScene.tscn
│   │   └── ReviveScene.tscn
│   └── UI/
│       ├── MainMenu.tscn
│       └── GameOver.tscn
├── Scripts/
│   ├── Battle/
│   │   ├── BattleManager.cs    ← Round/phase state machine
│   │   ├── BattleBoard.cs      ← Grid generation & cell management
│   │   ├── BattleHUD.cs        ← UI updates for phase/stats
│   │   ├── ActivationQueue.cs  ← Card speed-sort & activation
│   │   └── FieldCell.cs        ← Individual cell logic & field type
│   ├── Cards/
│   │   ├── CardData.cs         ← Card data model (resource)
│   │   ├── CardDeck.cs         ← Deck / draw / discard logic
│   │   ├── CardHand.cs         ← Player hand management
│   │   └── CardEffect.cs       ← Effect resolution base class
│   ├── Characters/
│   │   ├── CharacterData.cs    ← Stats, class definition (resource)
│   │   ├── PlayerCharacter.cs  ← Player unit on the board
│   │   └── EnemyCharacter.cs   ← Enemy unit + basic AI
│   ├── Map/                    ← (Deferred — overworld map not yet implemented)
│   ├── City/
│   │   ├── CityGenerator.cs    ← Randomly picks sub-nodes for a city
│   │   └── ShopManager.cs
│   └── Core/
│       ├── GameManager.cs      ← Global singleton, scene switching
│       ├── SaveSystem.cs       ← Save/load run state
│       └── EventBus.cs         ← Decoupled event system (signals)
├── Resources/
│   ├── Cards/                  ← .tres CardData resource files
│   ├── Characters/             ← .tres CharacterData resource files
│   └── Enemies/                ← .tres EnemyData resource files
├── Assets/
│   ├── Art/
│   │   ├── Cards/
│   │   ├── Characters/
│   │   ├── Enemies/
│   │   ├── UI/
│   │   └── Tiles/
│   ├── Audio/
│   │   ├── SFX/
│   │   └── Music/
│   └── Fonts/
└── Tests/                      ← Unit tests for game logic
```

---

## 7. Enums & Constants Reference

```csharp
// CardType.cs
enum CardType { Move, Battle, Buff, Debuff, Setup }

// CardSpeed.cs
enum CardSpeed { Burst = 0, Fast = 1, Slow = 2 }  // lower = activates first

// BattlePhase.cs
enum BattlePhase { MovePhase, BattlePhase, SetupPhase }

// FieldType.cs
enum FieldType { Normal, Water, Lava, Forest, Ice, Sand }

// NodeType.cs
enum NodeType { Battle, City, Shop, Exchange, Quest, Heal, Revive, Boss }
```

---

## 8. Development Milestones

| Milestone | Goal | Status |
|-----------|------|--------|
| **M0** | Repo setup, project docs, folder structure | ✅ Done |
| **M1** | Base battle scene — plain grid, character placement, phase cycling | 🔄 In Progress |
| **M2** | Card data model, deck/hand system, basic card play | ⬜ |
| **M3** | Field types, card effects, activation queue | ⬜ |
| **M4** | Enemy AI, battle victory/defeat flow | ⬜ |
| **M5** | Node map generation (deferred), city sub-nodes, scene transitions | ⬜ |
| **M6** | Shop, Exchange, Quest, Heal, Revive scenes | ⬜ |
| **M7** | Save/load, full run loop | ⬜ |
| **M8** | Polish, SFX, music, art pass | ⬜ |

---

## 9. Coding Conventions

- **Language:** C# (.NET) via Godot .NET
- **Naming:** PascalCase for classes/methods, camelCase for local vars, `_camelCase` for private fields
- **Scenes:** Each logical component has its own `.tscn` + paired `.cs` script
- **Resources:** Game data (cards, characters) stored as Godot `Resource` (`.tres`) files for easy editing
- **Signals:** Use Godot signals (via `EventBus.cs`) for decoupled communication between systems
- **No magic numbers:** All constants in dedicated `const` or `enum` files

---

## 10. Git Workflow

- `main` — stable, demo-ready
- `develop` — integration branch
- `feature/<name>` — individual feature branches
- PR into `develop`, merge to `main` when milestone is complete
