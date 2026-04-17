# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**BigManTing** is a Godot 4.6 game using C# (assembly: `BigManTing`). It targets Windows with Direct3D 12 rendering and Jolt Physics for 3D.
This is a simple game about collecting different things.
The core gameplay is played in a style similar to how the app Tinder functions. The user is presented with an item, and can chose to either collect or reject it.
By collecting certain combinations the player will be given a score of some sort.

## Running & Building

Open via the Godot editor, or from CLI:

```sh
godot --path .              # run the project
godot --path . --editor     # open in editor
dotnet build                # compile C# assembly only
```

Godot drives MSBuild internally on play/export — `dotnet build` is mainly for checking compile errors without launching the editor.

## Folder Structure

Feature-folder layout: scenes and scripts live **together** under `src/`, grouped by system.

```
src/
  player/       # Player scene + scripts
  world/        # Level scenes, environment, tilemaps
  ui/           # HUD, menus, overlays
  autoloads/    # Singletons registered in Project Settings > Autoload
assets/
  art/
    sprites/    # 2D textures, spritesheets
    models/     # 3D meshes, materials
  audio/
    sfx/
    music/
  fonts/
resources/      # Godot .tres/.res custom Resource files
```

New systems get their own subfolder under `src/`. Don't scatter `.tscn` files at the root.

## Conventions

### C# scripts
- PascalCase class names matching the filename: `Player.cs` → `public partial class Player`
- All scripts inherit from a Godot type (`Node`, `CharacterBody3D`, etc.) and are `partial`
- Namespace: `BigManTing` (or `BigManTing.<System>` for larger systems)
- Use `[Export]` for designer-facing properties; keep game logic out of `_Ready` where possible

### Scenes
- One root node per scene; the root node name matches the file name
- Prefer instancing scenes over duplicating nodes
- Autoloads live in `src/autoloads/` and are registered manually in Project Settings

### Resources
- Custom `Resource` subclasses go in `resources/` as `.tres` files
- Name resource files in `snake_case.tres`

## Physics & Rendering Notes

- Physics engine is **Jolt** — use Jolt-specific node types (`JoltBody3D` etc.) where available
- Renderer is **Forward Plus** — supports decals, VoxelGI, SDFGI
- RenderingDevice backend is D3D12 on Windows; avoid GL compatibility assumptions
