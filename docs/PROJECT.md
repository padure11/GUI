# CODE BOTS / Factory Breakout — Project Overview

Two-player co-op puzzle game built in Unity. Each player writes code in a custom programming language to control their robot through a level. Both robots execute their code in lockstep, sharing the same level.

## High-level architecture

```
┌──────────────┐         ┌──────────────┐
│  MainMenu    │ ───────►│  SampleScene │ (gameplay)
│  (UI Toolkit)│         │  (gameplay)  │
└──────────────┘         └──────────────┘
       │                       │
       │ Host/Join via Relay   │ NetworkManager.SceneManager.LoadScene
       ▼                       ▼
   NetworkManager (DontDestroyOnLoad, bootstrap in MainMenu)
       │
       └─► Unity Netcode for GameObjects + Unity Relay
```

- **MainMenu.unity** — UI Toolkit screen with Host/Join/Quit. Bootstraps NetworkManager + Unity Services (Auth + Relay).
- **SampleScene.unity** — gameplay scene loaded via NGO scene manager. Contains robots, level geometry, mechanics, GameSessionManager.
- **NetworkManager** lives in MainMenu, persists across scenes via DontDestroyOnLoad.
- **Server-authoritative**: Server runs all coroutines for robot movement. NetworkTransform syncs positions. NetworkAnimator syncs animation parameters.

## Code execution flow

```
Player types code in CodeEditor UI (UXML panel in SampleScene)
            │
            ▼ click Run
CodeEditorUI.OnRunPressed → GameSessionManager.SubmitCodeServerRpc
            │
            ▼ on server
Server collects both players' codes (code1, code2)
            │
            ▼ when both submitted
GameSessionManager.ExecuteBothStepByStep (server coroutine)
  - Lexer.Tokenize(code1) → tokens
  - Parser.Parse(tokens) → Command tree
  - ExtractFunctions (separates function defs)
  - FlattenCommands (inlines functions, expands repeat) → flat list of command strings
  - For each step: ExecuteStep(cmd1, cmd2) which starts coroutines on robot1/robot2
  - WaitUntil(both robots IsMoving=false) → next step
  - At end: ResetLevelClientRpc (if not completed)
```

## File map

### Code (`Assets/Scripts/`)

| Folder | Files | Role |
|---|---|---|
| `InterpreterScripts/` | Lexer.cs, Parser.cs, Token.cs, Command.cs | Custom language parser |
| | CommandExecutor.cs | Single-player executor (used in test scenes) |
| | CodeEditorUI.cs | UI Toolkit code editor (drag/resize/min/close/cheat sheet) |
| `NetworkManagerScripts/` | GameSessionManager.cs | NetworkBehaviour, server-authoritative executor for multiplayer |
| | LobbyUI.cs | Legacy (unused now; lobby is in MainMenu) |
| `PlayerScripts/` | PlayerController.cs | NetworkBehaviour, robot movement coroutines, Die mechanic |
| `CameraScripts/` | RTSCameraController.cs | RTS-style camera with focus on local robot |
| `UIScripts/` | MainMenu.cs | Main menu controller (Host/Join/Quit via Relay) |
| | SettingsOverlay.cs | ESC pause overlay (Resume/Back to Menu/Quit) |
| `LevelsScripts/` | FinishZone.cs | Trigger zone for level completion |
| | LevelResettable.cs | Marker — objects that reset on failure |
| | LevelSpawnTracker.cs | Tracks runtime-spawned objects, destroys on reset |
| `ButtonStandScripts/` | PressButton.cs | Press button → triggers animator on linked RoboArm |
| `RoboticArmScripts/` | RoboArm.cs | Animator wrapper for button-press animation |
| `MechanicsScripts/` | IToggleable.cs | Interface for toggle-able objects (ElectricZone, future Door) |
| | ElectricZone.cs | Electric fence between 2 poles, kills on contact |
| | Lever.cs | Toggle lever; affects connected IToggleable targets |
| `Assets/` (root) | LevelManager.cs | Level reset/complete coordinator (should be moved to Scripts/LevelsScripts/) |
| | Try.cs | Floor tile that spawns object when crate touches (used for platform reveal) |
| | ZimproveZ.cs | Notes file (dead — TODO delete) |
| | AnimationTester.cs | Test scene controller (OnGUI panel for testing animations) |

### UI (`Assets/UI/`)

| Folder | Files | Role |
|---|---|---|
| `MainMenu/` | MainMenu.uxml, MainMenuStyle.uss | Main menu UI |
| `CodeEditor/` | CodeEditor.uxml, EditorStyle.uss, CodeEditorController.asset | In-game code editor with cheat sheet |
| `Settings/` | SettingsOverlay.uxml, SettingsOverlayStyle.uss | ESC pause overlay |

### Scenes (`Assets/Scenes/`)

| File | Role |
|---|---|
| `MainMenu.unity` | Bootstrap scene with Host/Join UI |
| `SampleScene.unity` | Gameplay level |

### Prefabs (`Assets/Prefabs/Mechanics/`)

| Prefab | Role |
|---|---|
| `ElectricFence` | Electric fence with 2 poles + zigzag beams + trigger |
| `Lever` | Pull-able lever, toggles connected targets |

## Networking model

- **Authority**: Server runs all robot coroutines (server-authoritative).
- **NetworkTransform** on robots: syncs position/rotation to clients automatically.
- **NetworkAnimator** on robots: syncs animation parameters.
- **Death**: ElectricZone detects collision (server-side only), calls `robot.Die()` which:
  - Sets `isDeadThisRun = true` (skips all further commands this run)
  - Plays death animation + particles via ClientRpc
  - Teleports to startPosition after deathDuration
- **Reset**: GameSessionManager broadcasts ResetLevelClientRpc at end of run if level not completed.

## Robot setup checklist

Every robot needs:
- Animator (Controller=RobotAnimator, Avatar, Apply Root Motion OFF)
- NetworkObject
- NetworkTransform (Authority=Server)
- NetworkAnimator (drag root in Animator field)
- Collider (Capsule, NOT Trigger)
- Rigidbody (Is Kinematic ✓, Use Gravity ✗, Freeze Rotation X/Y/Z)
- PlayerController script

GameSessionManager.robot1 and robot2 must reference these in Inspector.

## Known issues / debt

See [TODO.md](TODO.md).
