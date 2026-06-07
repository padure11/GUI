# Status + Roadmap

## Currently working

### Multiplayer + UI
- ✅ Main Menu (UI Toolkit) with Host/Join/Quit + Relay setup
- ✅ Wait for player 2 before scene transition
- ✅ Settings overlay on ESC (Resume / Back to Menu / Quit)
- ✅ Code editor with drag/resize/minimize/close + cheat sheet + run/idle indicator + Pull support
- ✅ Camera per-player (each client focuses local robot) + F to recenter + auto-follow

### Robot + Animation
- ✅ Server-authoritative movement (NetworkTransform sync)
- ✅ Mixamo Idle animation (Loop Pose for smooth loop)
- ⏸️ Walk animation (paused — robot can walk but no animation yet)
- ⏸️ Jump/Push/Press/Die animations (paused — only Die hooked up)
- ✅ Boost jump (climb + gap, parabolic arc, wind-up phase)
- ✅ Turn (no animation, pure code rotation)

### Mechanics
- ✅ Pressure plate + push crate (existing)
- ✅ Press button (existing)
- ✅ Electric fence (ElectricZone, IToggleable, zigzag beams, death on contact)
- ✅ Lever (Lever, toggles connected IToggleables, smooth handle rotation)
- ⏳ Co-op button (planned, in active roadmap)
- ⏳ Portal (planned, in active roadmap)
- ⏳ Third new mechanic (TBD — recommend conveyor belt)

### Language
- ✅ Commands: moveForward, moveBack, turnLeft, turnRight, jump, push, press, pull, wait
- ✅ `repeat(n) { }`
- ✅ Function definitions + calls (no parameters, recursion-safe)
- ❌ `if`/`while` + conditions (parsed but not evaluated; deferred)
- ❌ `grab`/`drop` (listed in Lexer but not implemented)

## Current iteration plan (user's stated order)

1. **Set up the second robot** with new model + animations (mirror of robot 1)
2. **Rebuild the base level** integrating all current mechanics (pressure plate + crate, electric fence + lever, finish zone)
3. **Add 2 new mechanics**: portal + co-op button + a third (TBD)

## Known bugs / debt

### High-impact
- ⚠️ Code editor mouse hover detection sometimes flaky on UI element edges (mostly OK after recent fixes)
- ⚠️ Robot's Box Collider must be on root (not a child) for ElectricZone trigger to detect

### Medium
- 🐛 Walking animation not yet imported — robots walk in Idle pose during moveForward
- 🐛 Push/Press/Jump animations not yet imported

### Cleanup needed
- 🧹 `Assets/LevelManager.cs` → move to `Assets/Scripts/LevelsScripts/`
- 🧹 `Assets/ZimproveZ.cs` → delete (notes only)
- 🧹 `Assets/Try.cs` → rename to something meaningful (e.g., `CrateSpawnTrigger`) or refactor into a proper mechanic
- 🧹 `Assets/Scripts/NetworkManagerScripts/LobbyUI.cs` → delete (no longer attached, MainMenu handles it)
- 🧹 `Assets/Scripts/InterpreterScripts/CodeParser.cs` → delete (replaced by Lexer+Parser+CommandExecutor)

### Optional features
- Indicator "YOU" cone above local robot (visual identifier in multiplayer)
- Level Complete screen (currently just Debug.Log)
- Sound effects (move/jump/press/death)
- Tutorial overlay for first level
- Spectator mode (camera follows other robot when local is dead)
- Save/load code in PlayerPrefs (persist between runs)
- Ctrl+Enter shortcut to Run (instead of clicking button)

## Deferred features (waiting on design)

- `if`/`while` + conditions evaluation
- `grab`/`drop` + Pickable + Key + LockedDoor system
- More animations (Walk, Push, Press)
- Doors as toggleable mechanic (open/closed)
- Hazards: lava, spikes, falling blocks

## Architecture decisions log

| Decision | Reason |
|---|---|
| Server-authoritative robot movement | Eliminates desync from alt-tab/lag (was the original bug); single source of truth |
| Lockstep flatten model in GameSessionManager | Simple, deterministic; downside is no runtime conditions (if/while) |
| Functions implemented as inlining (FlattenCommands) | Simple, no scope/recursion issues at runtime; recursion limit 100 |
| Animation root motion baked into pose | Code controls position; animation is purely visual |
| Turn commands: no animation, pure code rotation | Mixamo turn animations rotate visually too — double rotation looked bad; clean pivot in Idle is robotic and looks good |
| IToggleable interface | Lever can control any toggle-able mechanic; extensible to doors, platforms, etc. |
| `isDying` flag separate from `isMoving` | Prevents lockstep from proceeding during death animation (was the recent reset-too-fast bug) |
