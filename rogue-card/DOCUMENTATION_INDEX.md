# Documentation Index - Rogue Card

Quick reference to find the right documentation for your needs.

## 📚 Documentation Files

### For New Team Members - START HERE
1. **[README.md](README.md)** - START HERE
   - Project overview
   - Initial setup instructions
   - Git workflow and collaboration guide
   - Development environment setup

2. **[PROJECT_DESIGN.md](PROJECT_DESIGN.md)**
   - Complete game design document
   - Game mechanics and systems
   - Battle system rules (phases, cards, stats)
   - Data structures and design patterns
   - Development roadmap

3. **[AI_CONTEXT.md](AI_CONTEXT.md)**
   - Comprehensive context for AI systems
   - Architecture overview
   - System integration points
   - Development guidelines for AI
   - Key metrics and performance targets

### For Developers - DEEP DIVE
4. **[DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md)**
   - Architecture patterns and philosophy
   - System relationships and dependencies
   - Adding new features (step-by-step)
   - Common coding patterns
   - Debugging and testing approaches
   - Performance considerations

5. **[TASKS.md](TASKS.md)**
   - Complete task breakdown by system
   - Priority levels and dependencies
   - Estimated effort for each task
   - Task assignment by skill level
   - Work stream suggestions for parallel development
   - Weekly checklist template

### For Scene Setup
6. **[BATTLE_SCENE_SETUP.md](BATTLE_SCENE_SETUP.md)**
   - Step-by-step battle scene creation
   - Scene structure and node hierarchy
   - Debug features and controls
   - Troubleshooting guide
   - Tips for collaboration

---

## 🎯 Find What You Need

### I need to...

#### Set up the project
→ Read [README.md](README.md) sections 1-5

#### Understand the game design
→ Read [PROJECT_DESIGN.md](PROJECT_DESIGN.md) sections 1-4

#### Understand the code architecture
→ Read [DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md) sections 1-3

#### Create the battle scene
→ Follow [BATTLE_SCENE_SETUP.md](BATTLE_SCENE_SETUP.md) exactly

#### Know what to work on
→ Check [TASKS.md](TASKS.md) for current phase and pick a task

#### Use git properly
→ Read [README.md](README.md) "Git Workflow" section

#### Debug an issue
→ Check [DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md) "Debugging" section

#### Add a new feature
→ Follow [DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md) "Adding a New Feature"

#### Understand code patterns
→ Read [DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md) "Common Patterns" section

#### Get context as an AI system
→ Read [AI_CONTEXT.md](AI_CONTEXT.md)

---

## 📖 Reading Order

### For First-Time Setup (1-2 hours)
1. README.md - Overview and project setup
2. PROJECT_DESIGN.md - Game concept and features
3. BATTLE_SCENE_SETUP.md - Create the scene
4. Run the battle scene and verify it works

### For Development Start (2-3 hours)
1. DEVELOPER_GUIDE.md - Architecture overview
2. TASKS.md - Choose your first task
3. Relevant system documentation (below)
4. Code comments in system files

### For Understanding Specific Systems

#### Battle System
- Project Design → Battle System section
- Developer Guide → Battle System Architecture
- Code: `Scripts/Battle/BattleManager.cs`

#### Phase System
- Project Design → Battle Phases section
- Developer Guide → Phase Manager Pattern
- Code: `Scripts/Core/PhaseManager.cs`

#### Board System
- Project Design → Board section
- Code: `Scripts/Battle/BoardManager.cs`
- Tasks: TASKS.md → Phase 2B "Board Interactions"

#### Character System
- Project Design → Character System section
- Code: `Scripts/Characters/Character.cs`
- Tasks: TASKS.md → Phase 2 "Character System"

#### Card System
- Project Design → Card System section
- Code: `Scripts/Cards/CardData.cs`
- Tasks: TASKS.md → Phase 2 "Card System"

---

## 🏗️ Project Structure Quick Reference

```
rogue-card/                              # Root project directory
├── README.md                           # Main setup/collaboration guide
├── PROJECT_DESIGN.md                   # Game design specification
├── DEVELOPER_GUIDE.md                  # Architecture and patterns
├── BATTLE_SCENE_SETUP.md              # Scene creation tutorial
├── AI_CONTEXT.md                       # Context for AI systems
├── TASKS.md                            # Development task list
├── .gitignore                          # Git ignore file
├── project.godot                       # Godot project config
│
├── Scripts/                            # C# game code
│   ├── Core/                          # Core systems (enums, phase)
│   ├── Battle/                        # Battle system
│   ├── Cards/                         # Card system
│   ├── Characters/                    # Character system
│   ├── City/                          # City hub system
│   ├── Map/                           # Adventure map system
│   └── Network/                       # Networking system
│
├── Scenes/                            # Godot scene files
│   ├── Battle/
│   │   └── UI/
│   ├── City/
│   ├── Map/
│   └── UI/
│
├── Resources/                         # Game data files
│   ├── Cards/
│   ├── Characters/
│   └── Enemies/
│
├── Assets/                            # Art, audio, fonts
│   ├── Art/
│   ├── Audio/
│   └── Fonts/
│
└── Tests/                             # Test files
```

---

## ❓ FAQ - Quick Answers

**Q: Where do I start?**
A: Read README.md first, then follow BATTLE_SCENE_SETUP.md

**Q: How do I know what to work on?**
A: Check TASKS.md for current phase and pick based on your skill level

**Q: How do I submit my work?**
A: Read README.md "Git Workflow for Team Collaboration"

**Q: What's the game about?**
A: Read PROJECT_DESIGN.md overview section

**Q: How does the architecture work?**
A: Read DEVELOPER_GUIDE.md architecture section

**Q: How do I add a new system?**
A: Follow steps in DEVELOPER_GUIDE.md "Adding a New Feature"

**Q: I found a bug, now what?**
A: Check DEVELOPER_GUIDE.md "Debugging" section

**Q: How do I debug code?**
A: See DEVELOPER_GUIDE.md "Debugging" section

**Q: What coding style should I use?**
A: See README.md "Development Guidelines" → Naming Conventions

---

## 🔗 Cross-References

### Systems Mentioned in Multiple Docs

#### Battle System
- Complete spec: PROJECT_DESIGN.md § 4
- Architecture: DEVELOPER_GUIDE.md § Battle System Architecture
- Scene setup: BATTLE_SCENE_SETUP.md
- Tasks: TASKS.md § Phase 2B: Battle Mechanics
- Code: Scripts/Battle/

#### Card System
- Game mechanics: PROJECT_DESIGN.md § 3
- Architecture patterns: DEVELOPER_GUIDE.md § Example: Adding Card Effects
- Tasks: TASKS.md § Phase 2: Card System
- Code: Scripts/Cards/

#### Character System
- Game mechanics: PROJECT_DESIGN.md § 2
- Tasks: TASKS.md § Phase 2: Character System
- Code: Scripts/Characters/

#### Phase System
- Game mechanics: PROJECT_DESIGN.md § 4 (Battle Phases)
- Architecture: DEVELOPER_GUIDE.md § Common Patterns
- Implementation: Scripts/Core/PhaseManager.cs
- Tasks: TASKS.md § Phase 2B: Phase Manager Enhancement

---

## 📞 Getting Help

### If you have questions about...

**Game Design** → PROJECT_DESIGN.md
**Code Architecture** → DEVELOPER_GUIDE.md
**Specific Features** → TASKS.md or README.md
**Scene Creation** → BATTLE_SCENE_SETUP.md
**Git/Workflow** → README.md
**AI Integration** → AI_CONTEXT.md

### Still stuck?
1. Search all documentation files
2. Check code comments in relevant files
3. Ask a team member
4. Create an issue if it's a bug

---

## 📋 Useful Checklists

### First-Time Developer Setup Checklist
- [ ] Read README.md
- [ ] Read PROJECT_DESIGN.md
- [ ] Install Godot 4.x with .NET
- [ ] Clone repository and build project
- [ ] Follow BATTLE_SCENE_SETUP.md
- [ ] Run battle scene and verify
- [ ] Pick a task from TASKS.md
- [ ] Create feature branch
- [ ] Ask questions if needed

### Before Committing Code Checklist
- [ ] Code builds without errors
- [ ] Follows naming conventions (see README.md)
- [ ] Has comments for complex logic
- [ ] Tested locally
- [ ] Commit message is descriptive
- [ ] Updated relevant documentation
- [ ] Checked related code for patterns

### Before Creating Pull Request Checklist
- [ ] All commits are pushed
- [ ] Commit messages follow format (see README.md)
- [ ] No merge conflicts
- [ ] Code review checklist passed (README.md)
- [ ] Documentation is updated
- [ ] Changes are tested

---

## 📖 How to Use This Index

1. **Find your topic** in "Find What You Need" section
2. **Follow the arrow** → to the relevant documentation
3. **Read the section** indicated
4. **If still confused** answer may be in a cross-referenced section
5. **Still stuck?** Ask your team

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2/21/26 | 1.0 | Initial documentation suite created |
| | | - 6 main documentation files |
| | | - Complete architecture and design specs |
| | | - Task breakdown for team development |
| | | - Git and collaboration workflows |

---

## Document Maintenance

Each documentation file should be updated when:
- New systems are added
- Architecture changes significantly
- Development processes change
- New documentation files are created
- Important lessons are learned

Last Updated: **February 21, 2026**
Maintainer: **Project Lead**
Status: **Active Development**

---

**Tip**: Bookmark this page and reference it often. It's your map to the entire project documentation!
