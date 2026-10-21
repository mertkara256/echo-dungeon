# Echo Dungeon

Echo Dungeon is a room-based action dungeon game developed in Unity. The player clears rooms, collects coins, upgrades their character through shops and upgrade systems, and prepares for a boss encounter.

## Features

- Room-based dungeon progression
- Combat, treasure, upgrade, and boss room types
- Melee, ranged, tank, and elite ranged enemy archetypes
- Corrupted Guardian boss encounter
- Coin, shop, locked chest, and upgrade station systems
- Day progression and run state management
- Stop and Poison area spells
- HUD, health bar, spell cooldown, and room progress UI
- Universal Render Pipeline support

## Technology

- Unity `6000.3.10f1`
- Universal Render Pipeline `17.3.0`
- Unity Input System `1.18.0`
- AI Navigation `2.0.10`
- TextMesh Pro
- DOTween

## Getting Started

1. Install Unity `6000.3.10f1` through Unity Hub.
2. Clone this repository:

   ```bash
   git clone https://github.com/mertkara256/echo-dungeon.git
   ```

3. Open the project folder in Unity Hub using `Add project from disk`.
4. Wait for Unity to restore the required packages.
5. Open `Assets/Scenes/MainMenu.unity` or `Assets/Scenes/SampleScene.unity`.
6. Press Play to start the game.

## Controls

| Action | Key |
| --- | --- |
| Movement | `WASD` or arrow keys |
| Attack | Left mouse button |
| Secondary attack | Right mouse button |
| Interact | `E` |
| Dash | `Left Shift` or `Right Shift` |
| Potion | `Q` |
| Speed boost / choice 1 | `1` |
| Stasis spell / choice 2 | `2` |
| Poison spell / choice 3 | `3` |
| Pause | `Esc` |
| Restart | `R` |

## Project Structure

```text
Assets/
  Animations/       Player animations
  Materials/        Game materials
  Plugins/          DOTween and third-party plugins
  Prefabs/          Boss, dungeon item, and enemy prefabs
  Scenes/           MainMenu and SampleScene scenes
  Scripts/          Game code
  Settings/         URP and render settings
Packages/           Unity package manifest and lock files
ProjectSettings/    Unity project settings
docs/screenshots/   Gameplay screenshots
```

## Key Script Folders

- `Assets/Scripts/Core`: game state, input, event, and score management
- `Assets/Scripts/Dungeon`: room, door, shop, chest, item, and room manager systems
- `Assets/Scripts/Enemy`: enemy behavior and coin drop systems
- `Assets/Scripts/Player`: player movement, combat, health, inventory, stats, and spells
- `Assets/Scripts/Boss`: boss controller, phases, health, and hazard areas
- `Assets/Scripts/UI`: HUD, menu, and in-game UI components

## Git Notes

This repository tracks only the source project files. Unity-generated local files and folders such as `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `.plastic/`, `.csproj`, and `.sln` are excluded through `.gitignore`.
