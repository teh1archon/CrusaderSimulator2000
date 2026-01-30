# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Cantus Crucis** is a medieval army management and real-time battle simulator where the player commands a crusading army through music. Orders are issued by performing melodies in a rhythm-based interface, and execution accuracy directly determines battlefield effectiveness.

See [docs/GDD_3.md](docs/GDD_3.md) for the full Game Design Document.

### Design Pillars
- **Music Is Command** — All battlefield orders are issued through melodies rather than direct control
- **Skill Over Speed** — Timing, accuracy, and restraint matter more than raw input frequency
- **Preparation Matters** — Army composition, stat investment, and melody selection define success
- **Order vs Chaos** — Discipline and morale determine whether the army moves as one or falls apart

### Game Structure
- **Preparation Phase**: Strategic planning, upgrades, training, NPC interaction
- **Battle Phase**: Real-time rhythm-driven combat with autonomous soldiers

### Melody System
Commands use five fixed keys: `5`, `T`, `G`, `B`, `Space`. Notes scroll right-to-left toward a timing bar. Scoring (Perfect/Good/Bad/Miss) determines command effectiveness.

## Development Setup

- **Unity Version**: 2022.3.62f2 (must match exactly)
- **IDE**: Visual Studio, Rider, or VSCode with C# extension
- **Solution File**: `crusade manager 2000.sln`

## Build and Run

Unity commands are executed through the Editor, not CLI:
- **Play Mode**: Ctrl+P in Unity Editor
- **Build**: File > Build Settings > Select Platform > Build
- **Reimport Assets**: Right-click in Project window > Reimport All

Scripts compile automatically when saved. Check Unity Console (Ctrl+Shift+C) for compilation errors.

## Key Dependencies

- **Universal Render Pipeline (URP)**: 2D rendering pipeline
- **AI Navigation + NavMeshPlus**: 2D pathfinding - see [docs/navmeshplus-2d-setup.md](docs/navmeshplus-2d-setup.md)
- **Physics2D**: Collision detection

## Architecture

### Folder Structure

```
Assets/Scripts/
  Core/           # GameManager, Events, Utilities
  Data/           # ScriptableObject definitions (MelodyData, UnitClassData)
  Rhythm/         # Melody/rhythm system
  Units/          # Unit behaviors, stats, classes
  Combat/         # Battle logic, damage
  UI/             # UI controllers
  Editor/         # Editor utilities

Assets/ScriptableObjects/
  Units/          # Unit class definitions (designer-editable)
  Melodies/       # Melody track definitions (designer-editable)
  Commands/       # Command definitions
```

### Key Systems

**Rhythm System** (`Scripts/Rhythm/`)
- `RhythmGameManager.cs` - Core manager, handles input and scoring
- `NoteHighway.cs` - Visual note track (scrolls right-to-left)
- `NoteVisual.cs` - Individual note display with hit/miss effects
- `TimingJudge.cs` - Timing evaluation (Perfect/Good/Bad/Miss)
- `RhythmTestRunner.cs` - Debug/test component

**Data Definitions** (`Scripts/Data/`)
- `MelodyData.cs` - ScriptableObject for melody tracks (sorted list of time+lane)
- `UnitClassData.cs` - ScriptableObject for unit types (stats, requirements, bonuses)

**Unit System** (`Scripts/Units/`)
- `Unit.cs` - Main unit component with autonomous behavior (Idle→Moving→Attacking states)
- `UnitVisuals.cs` - Health bar, selection indicator, state display
- `UnitSpawner.cs` - Spawns units from prefabs with object pooling
- `UnitMovement.cs` - NavMeshAgent movement helper (optional)
- `UnitSelector.cs` - Debug tool for unit selection/inspection
- `UnitTestRunner.cs` - Test harness (F1-F5 hotkeys)

**Editor Tools** (`Scripts/Editor/`)
- `SampleAssetCreator.cs` - Tools > Cantus Crucis > Create Sample Assets
- `UnitPrefabGenerator.cs` - Tools > Cantus Crucis > Generate Unit Prefabs

### Timing Windows (from GDD)

- Perfect: ±50ms = +5 pts
- Good: ±100ms = +2 pts
- Bad: ±150ms = -2 pts
- Miss: beyond = -3 pts

## Designer Workflow

1. **Create Unit Classes**: Right-click > Create > Cantus > Unit Class
2. **Create Melodies**: Right-click > Create > Cantus > Melody
3. **Generate Samples**: Tools > Cantus Crucis > Create Sample Assets

## Current State

**Implemented:**

- ✅ 2D NavMesh pathfinding (NavMeshPlus)
- ✅ Rhythm input system with 5-lane note highway
- ✅ ScriptableObjects for melodies and unit classes
- ✅ Sample asset generator
- ✅ Unit system with autonomous behavior (Idle→Moving→Attacking)
- ✅ Prefab-based unit spawning with object pooling
- ✅ Unit prefab generator editor tool

**In Progress:**

- Command system connecting melodies to unit effects
- Battle system / morale

## Unit Prefab Workflow

1. **Create UnitClassData**: Right-click > Create > Cantus > Unit Class
2. **Generate Prefab**: Right-click the UnitClassData > Create Unit Prefab
3. **Configure Prefab**: Adjust NavMeshAgent radius, add sprites/animations
4. **Spawn in Game**: Use `UnitSpawner.SpawnUnit(prefab, position, isPlayer)`

Prefabs are stored in `Assets/Prefabs/Units/` and include:

- Unit component (references UnitClassData)
- NavMeshAgent (configured for 2D)
- UnitVisuals (health bar, selection)
- SpriteRenderer and Collider2D
