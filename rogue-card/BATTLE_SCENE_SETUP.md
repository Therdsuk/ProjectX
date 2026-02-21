# Battle Scene Setup Guide

## Overview
This guide explains how to set up the initial battle scene in Godot. The scene provides a basic 8x8 isometric board with phase management and UI controls.

## Prerequisites
- Godot 4.x with .NET support (C# enabled)
- The project structure and scripts already created

## Creating the Battle Scene

### Step 1: Create the Scene File
1. In Godot Editor, go to **Scene** menu
2. Click **New Scene**
3. Select **Node3D** as the root node
4. Rename it to `BattleScene`
5. Save as `Scenes/Battle/BattleScene.tscn`

### Step 2: Attach the Setup Script
1. Select the `BattleScene` root node
2. In the **Inspector**, go to the **Script** section
3. Click **Attach Script** button
4. Select `Scripts/Battle/BattleSceneSetup.cs`
5. Click **Create**

### Step 3: Configure Project Settings
1. Open `project.godot` in the root directory
2. Ensure .NET support is enabled:
   ```
   [dotnet]
   project/assembly_name="RogueCard"
   ```

### Step 4: Run the Scene
1. Open `Scenes/Battle/BattleScene.tscn`
2. Press **F5** or click **Play Scene** button
3. You should see:
   - A grid of tiles (8x8)
   - An isometric camera view
   - Phase information (MOVE PHASE, Round 1)
   - An "End Phase" button at the bottom
   - Debug text showing controls

### Step 5: Test the Phase System
1. In the running scene, press **Space** or click the **"End Phase"** button
2. The phase should advance: Move → Battle → Setup → Move (Round 2)
3. Check the output console for debug messages

## Scene Structure

```
BattleScene (Node3D)
├── MainCamera (Camera3D)
├── BattleManager (C# script)
├── BoardVisuals (Node3D)
│   └── BoardManager (C# script)
│       └── [8x8 Grid of Tiles]
└── UILayer (CanvasLayer)
    └── BattleUIManager (C# script)
        └── [UI elements: Labels, Buttons]
```

## Debug Features

### Current Debug Capabilities
- **Phase Advancement**: Press Space or click "End Phase" button
- **Console Output**: Check the Output console for system messages
- **Test Characters**: Three placeholder characters placed on board (Player1, Player2, Enemy1)

### Debug Messages
Watch the Output console for messages like:
- "BattleManager: Initialization complete"
- "PhaseManager: Phase changed to Battle"
- "BoardManager: Character Player1 placed at (1, 1)"

## Input Controls

| Input | Action |
|-------|--------|
| **Space** | Advance to next phase |
| **Click "End Phase"** | Advance to next phase (alternative) |
| **ESC** | Exit battle (closes game in test mode) |

## What's Implemented

✅ 8x8 Grid Board with visual tiles
✅ Phase System (Move → Battle → Setup → Loop)
✅ Round Counter
✅ Phase Display and Description
✅ Phase Advancement Controls
✅ Board Manager (tile placement, occupancy)
✅ Camera (isometric view)
✅ Test Character Placement

## What's NOT Yet Implemented

❌ Character models/visuals
❌ Card system
❌ Field effects
❌ Character movement
❌ Combat resolution
❌ Multiplayer networking
❌ Animation and VFX
❌ Full UI (settings, pause menu, etc.)

## Next Steps for Development

1. **Character System**: Create character nodes and visuals
2. **Card System**: Implement card structures and deck management
3. **Input Handling**: Add click detection for board tiles
4. **Character Movement**: Implement pathfinding and movement
5. **Card Execution**: Implement card playing and effect resolution
6. **Network Integration**: Add multiplayer capabilities
7. **Animation**: Add smooth transitions and visual feedback
8. **Combat Effects**: Visual effects for attacks, buffs, and field effects

## Troubleshooting

### Scene won't load
- Make sure the BattleSceneSetup.cs script is attached to the root node
- Check that the .csproj file includes all scripts
- Rebuild the .NET project: **Project → Tools → Build C# Project**

### UI not appearing
- Make sure BattleUIManager is under a CanvasLayer
- Check the console for any script errors
- Try resetting the scene and ensuring UILayer is added

### No visual board
- Check that BoardManager is creating tiles (watch console output)
- Ensure camera position is correct (should see board from above-right angle)
- Try adjusting camera position if needed

### Phase not advancing
- Check console for errors
- Make sure ui_select input action is configured (Space key)
- Verify PhaseManager signals are connected properly

## Tips for Collaboration

- This is a **foundation scene** designed for team development
- Each co-worker can work on different systems in parallel:
  - One develops character system
  - One develops card system
  - One develops field effects
  - One develops networking
- Always test locally before committing to git
- Document any changes to the scene structure in this file

## Scene Save Format

The BattleScene.tscn file is a Godot scene file. It can be:
- Edited in Godot Editor (visual scene composer)
- Edited as text (if opened in a text editor) for merge conflict resolution
- Committed to git as a text file
