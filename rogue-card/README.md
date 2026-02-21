# Rogue Card - Setup and Git Instructions

## Project Overview
Rogue Card is a multiplayer 3D card-based roguelike game built in Godot using C#. This document explains how to set up the project locally and prepare it for team collaboration via Git.

## Initial Setup

### 1. Prerequisites
- **Godot 4.x** with .NET 6+ support installed
- **Git** configured on your system
- **.NET SDK 6.0 or higher** installed
- **Visual Studio Code** or your preferred C# IDE

### 2. Project Structure
The project is organized as follows:

```
rogue-card/
├── PROJECT_DESIGN.md           # Game design document (read this!)
├── BATTLE_SCENE_SETUP.md       # Battle scene setup instructions
├── README.md                   # This file
├── project.godot               # Godot project configuration
├── icon.svg.import             # Project icon
├── rogue-card.csproj           # .NET project file (auto-generated)
├── Scripts/                    # C# game code
│   ├── Core/                   # Core systems
│   ├── Battle/                 # Battle system
│   ├── Cards/                  # Card system
│   ├── Characters/             # Character system
│   ├── City/                   # City/hub systems
│   ├── Map/                    # Adventure map
│   ├── Network/                # Networking
│   └── ...
├── Scenes/                     # Godot scene files
│   ├── Battle/
│   ├── City/
│   ├── Map/
│   ├── UI/
│   └── ...
├── Resources/                  # Game data files (cards, characters, enemies)
├── Assets/                     # Art, audio, fonts
└── Tests/                      # Unit tests
```

### 3. First Time Setup

#### Option A: Opening Existing Project
1. Clone the repository (if not already done):
   ```bash
   git clone <repository-url>
   cd rogue-card
   ```

2. Open in Godot:
   - Launch Godot Editor
   - Click "Open Project"
   - Navigate to the `rogue-card` folder
   - Click "Select Folder"

3. Godot will import the project and generate `.NET` projects

4. Build the .NET project:
   - In Godot: **Project → Tools → Build C# Project**
   - Or from terminal: `dotnet build`

#### Option B: Starting Fresh
1. Create a new Godot project with .NET enabled
2. Copy the project structure from this repository
3. Follow the Build steps above

### 4. Building the Project

#### In Godot Editor
- Go to **Project → Tools → Build C# Project**
- Wait for build to complete (check Output panel)

#### From Command Line
```bash
# Build the project
dotnet build

# Run tests (if implemented)
dotnet test

# Build for export
dotnet build -c Release
```

### 5. Running the Battle Scene

1. Open `Scenes/Battle/BattleScene.tscn` in Godot
2. Press **F5** to run the scene
3. You should see:
   - An 8x8 grid board
   - Phase information at the top
   - Controls panel at the bottom

See [BATTLE_SCENE_SETUP.md](BATTLE_SCENE_SETUP.md) for detailed scene setup.

## Git Workflow for Team Collaboration

### Initial Repository Setup

#### One Team Member (Repository Owner)
1. Initialize git in a central location:
   ```bash
   cd rogue-card
   git init
   git config user.name "Your Name"
   git config user.email "your.email@example.com"
   ```

2. Create initial commit:
   ```bash
   git add .
   git commit -m "Initial project structure and battle scene foundation"
   git branch -M main
   ```

3. Push to remote (GitHub, GitLab, etc.):
   ```bash
   git remote add origin <your-repo-url>
   git push -u origin main
   ```

#### All Other Team Members
1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd rogue-card
   ```

2. Open in Godot and build (see section 3 above)

### Daily Workflow

#### Before Starting Work
```bash
# Get latest changes
git pull origin main

# Create a feature branch for your task
git checkout -b feature/your-feature-name
```

#### During Development
```bash
# Check status
git status

# Stage changes
git add Scripts/Your/Path/YourFile.cs

# Commit regularly with descriptive messages
git commit -m "Add character movement system"

# Push to your branch
git push origin feature/your-feature-name
```

#### Submitting Your Work
1. Push your feature branch to remote
2. Create a **Pull Request** (PR) on GitHub/GitLab
3. Describe your changes in the PR
4. Request review from team members
5. After approval, merge to `main` branch

### Common Git Commands

```bash
# View commit history
git log --oneline --graph

# See what files you've changed
git status

# See detailed changes in a file
git diff Scripts/Your/Path/YourFile.cs

# Undo changes to a file (before commit)
git checkout Scripts/Your/Path/YourFile.cs

# Undo changes to all files (before commit)
git reset --hard HEAD

# Stash current work (temporarily save)
git stash
git stash pop  # Apply stashed changes later

# Switch branches
git checkout main
git checkout feature/your-feature

# Fetch latest without merging
git fetch origin
```

### Handling Merge Conflicts

If two team members edit the same file:

1. Pull the latest changes:
   ```bash
   git pull origin main
   ```

2. Look for conflict markers (`<<<<`, `====`, `>>>>`)

3. Edit the file to resolve conflicts

4. Stage and commit:
   ```bash
   git add Scripts/ConflictedFile.cs
   git commit -m "Resolve merge conflict in ConflictedFile.cs"
   git push origin feature/your-branch
   ```

## Development Guidelines

### Code Organization
- **Core Systems**: Generic systems used by multiple features (PhaseManager, BoardManager)
- **System-Specific**: Code scoped to a specific system (BattleManager in Battle/)
- **Data/Resources**: Non-code assets (card data, character stats, etc.)

### Naming Conventions
- **Classes**: PascalCase (e.g., `BattleManager`, `CardEffect`)
- **Methods**: PascalCase (e.g., `AdvancePhase()`, `PlaceCharacter()`)
- **Variables**: camelCase (e.g., `currentPhase`, `boardSize`)
- **Constants**: UPPER_SNAKE_CASE (e.g., `BOARD_SIZE = 8`)
- **Scene Files**: snake_case (e.g., `battle_scene.tscn`)
- **Folders**: PascalCase (e.g., `Scripts/Battle`, `Resources/Cards`)

### Commit Messages
Use clear, descriptive commit messages:
- ✅ Good: `Add phase system with round counter`
- ✅ Good: `Fix board tile occupancy validation`
- ❌ Bad: `fix stuff`
- ❌ Bad: `update`

Format: `[Category] Brief description`
- `[Feature]` - New functionality
- `[Fix]` - Bug fix
- `[Refactor]` - Code restructuring
- `[Docs]` - Documentation updates
- `[Test]` - Test additions

Example:
```bash
git commit -m "[Feature] Add battle phase manager system

- Implement Move, Battle, Setup phases
- Add round counter
- Add phase transition signals
- Create PhaseManager class with debug output"
```

### Branch Naming
- `feature/description` - New features (e.g., `feature/card-system`)
- `fix/description` - Bug fixes (e.g., `fix/phase-timing`)
- `refactor/description` - Code improvements (e.g., `refactor/battle-manager`)
- `docs/description` - Documentation (e.g., `docs/api-reference`)

### Code Review Checklist
Before requesting code review:
- [ ] Code builds without errors
- [ ] Follows naming conventions
- [ ] Includes comments for complex logic
- [ ] No hardcoded values (use constants)
- [ ] Tested locally
- [ ] Commit message is descriptive

## Project Milestones

### Phase 1: Foundation ✅ (Current)
- [x] Project structure
- [x] Battle scene setup
- [x] Phase system
- [ ] Deploy to git

### Phase 2: Core Systems (Next)
- [ ] Character system
- [ ] Card system
- [ ] Board interactions
- [ ] Basic AI

### Phase 3: Gameplay Loop
- [ ] Combat resolution
- [ ] Card execution
- [ ] Field effects
- [ ] Win/lose conditions

### Phase 4: Multiplayer
- [ ] Network architecture
- [ ] Player synchronization
- [ ] Adventure map
- [ ] City hub

### Phase 5: Polish
- [ ] Animations
- [ ] Visual effects
- [ ] Audio
- [ ] UI refinement

## Troubleshooting

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet build

# In Godot: Project → Tools → C# → Build
```

### Git Issues

**Can't push changes:**
```bash
# Make sure you're up to date
git pull origin feature/your-branch
# Then try pushing again
git push origin feature/your-branch
```

**Accidentally committed to wrong branch:**
```bash
# Create a new branch from current HEAD
git branch feature/correct-name
# Reset main to before your commits
git reset --hard origin/main
# Switch to your new branch
git checkout feature/correct-name
```

### Godot Issues

**Scenes not showing:**
- Rebuild .NET project
- Reload the scene (close and reopen)
- Check script errors in Output panel

**Can't see changes in editor:**
- Save the scene (Ctrl+S)
- Build C# project
- Reload scene

## Resources

- [Godot Documentation](https://docs.godotengine.org/)
- [Git Documentation](https://git-scm.com/doc)
- [C# Coding Standards](https://microsoft.github.io/csharp/)
- [Godot .NET Guide](https://docs.godotengine.org/en/stable/tutorial/scripting/gdscript/)

## Getting Help

### For Godot Questions
- Check the official [Godot Documentation](https://docs.godotengine.org/)
- Search [Godot Q&A](https://godotengine.org/community/forums/)
- Ask team members on your communication platform

### For Git Questions
- Check [Pro Git Book](https://git-scm.com/book/en/v2)
- Use `git help <command>` in terminal (e.g., `git help push`)
- Ask a team member with Git experience

### For Project Questions
- Read [PROJECT_DESIGN.md](PROJECT_DESIGN.md) for game design specifics
- Read [BATTLE_SCENE_SETUP.md](BATTLE_SCENE_SETUP.md) for battle scene details
- Check code comments in the relevant systems

## Next Steps

1. **Read** [PROJECT_DESIGN.md](PROJECT_DESIGN.md) to understand game concepts
2. **Review** [BATTLE_SCENE_SETUP.md](BATTLE_SCENE_SETUP.md) for scene setup
3. **Build** the project locally and verify it works
4. **Create** your first feature branch: `git checkout -b feature/your-first-task`
5. **Start developing** - assign tasks and begin implementation!

---

**Last Updated**: February 21, 2026
**Project Status**: Foundation Phase - Ready for Team Development
**Questions?** Check the documentation files or ask your team lead!
