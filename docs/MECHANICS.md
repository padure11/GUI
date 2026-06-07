# Mechanics Reference

## Existing mechanics (implemented)

### 1. Pressure Plate + Pushable Crate

**Purpose**: Static state mechanic. Robot pushes a crate onto a plate; plate triggers something while crate sits on it.

- `PushableCrate` tagged object — has Collider + Rigidbody, can be pushed via robot's `push` command
- Player command: `push()` — checks raycast forward for PushableCrate, slides it 2 units forward (if floor exists behind)
- Pressure plate: trigger collider, when crate enters → fire event (open door, etc.)
- See `Try.cs` for a basic floor-tile-triggers-spawn implementation

### 2. Electric Fence (`ElectricZone.cs`)

**Purpose**: Death/risk mechanic. Robot dies if it enters the trigger zone while active.

- 2 pole positions (`poleA`, `poleB` Transform references)
- N parallel zigzag LineRenderer beams between poles (auto-created at Start)
- BoxCollider (Trigger) covers area between poles
- Implements `IToggleable` — can be turned on/off via Lever or other source
- OnTriggerEnter (server-only): calls `robot.Die("electric")`

**Setup checklist**:
- Prefab `Assets/Prefabs/Mechanics/ElectricFence.prefab`
- Root GameObject with: BoxCollider (Is Trigger ✓), ElectricZone script
- 2 empty children at pole tops: PoleATop, PoleBTop
- ElectricZone Inspector: pole references, beamCount (4 default), jaggedness (0.05), startActive (true)

### 3. Lever (`Lever.cs`)

**Purpose**: Toggle control. Robot pulls; affects connected IToggleable objects.

- `connectedTargets` (GameObject array) — drag any IToggleable (ElectricZone, future Door, etc.)
- `invertTargetState` (bool) — when true, lever ON = targets OFF (default true, so lever turns OFF the fence)
- `handle` Transform — child that rotates between offRotation and onRotation
- NetworkVariable `netIsOn` syncs state on multiplayer
- Player command: `pull()` — raycast forward for Lever, calls `Flip()`

**Setup checklist**:
- Prefab `Assets/Prefabs/Mechanics/Lever.prefab`
- Root GameObject with: BoxCollider (NOT Trigger), NetworkObject, Lever script
- Child: Handle (mesh that rotates)
- Lever Inspector: connectedTargets, invertTargetState, handle, offRotation, onRotation

### 4. Press Button (`PressButton.cs`)

**Purpose**: One-time press to trigger something (e.g., RoboArm animation).

- Reference to a `RoboArm` component (or future targets)
- Player command: `press()` — raycast forward for PressButton, calls `Activate()`
- Activate triggers connected animator

### 5. Death + Reset (`PlayerController.Die`)

**Purpose**: Generic death mechanic. Used by ElectricZone, future hazards (lava, spikes).

- `Die(string cause)` coroutine on PlayerController
- Sets `isDeadThisRun = true` → all subsequent commands skip via `if (isDeadThisRun) yield break`
- Sets `isDying = true` → `IsMoving()` returns true while dying (lockstep waits)
- Plays animation (`Die` trigger), spawns death effect prefab (optional)
- Waits `deathDuration` seconds → teleports to `startPosition`, rebinds animator
- `ResetToStart()` clears `isDeadThisRun` and `isDying` at end of run

## Planned mechanics

### 1. Co-op Button

**Purpose**: True coordination. Button activates only if BOTH robots press in the same lockstep step.

- Component: `CoopButton.cs`
- When robot's `press` raycast hits CoopButton: register "P1 pressed this step" or "P2 pressed this step"
- After current step ends (or via timer), check if both registered → activate connected IToggleable
- Visual: 2 LEDs/segments that light up as each player presses

### 2. Portal

**Purpose**: Spatial shortcut. Robot enters one portal → exits the paired portal.

- Component: `Portal.cs`
- Reference to `Portal otherPortal`
- OnTriggerEnter: teleport robot to other portal's position
- Anti-loop: flag `justTeleported` for 0.5s on robot to prevent immediate re-teleport on exit portal
- Visual: vortex particle effect, colored to match the pair

### 3. (TBD — third new mechanic)

Suggestions:
- **Conveyor belt** — robot/crates on belt move 1 tile per step automatically (in belt direction)
- **Crumbling tile** — disappears after standing on it; one-way path
- **Key + Locked Door** — uses `grab`/`drop` (already in Lexer), inventory mechanic

## Future mechanics (deferred)

- Doors with state (locked/unlocked, open/closed)
- Hazards: lava (death on contact), spikes, falling blocks
- Switches with state (one-time, toggle, momentary)
- Moving platforms (ride them between positions)
- Lasers/beams (moving, reflective with mirrors)
- Keys + locked doors (with grab/drop)
- Power crystals (temporary abilities)

## Adding a new mechanic — checklist

1. **Component script** in `Assets/Scripts/MechanicsScripts/<MechanicName>.cs`
2. **Implement IToggleable** if it can be on/off via Lever/Button
3. **Trigger handling** — if it kills/teleports robot, OnTriggerEnter on server-only
4. **Networking** — server-authoritative, NetworkVariable or ClientRpc for state sync
5. **Prefab** in `Assets/Prefabs/Mechanics/<MechanicName>.prefab`
6. **Player command** (if needed) — add to Lexer commands list, PlayerController coroutine, CommandExecutor case, GameSessionManager.ExecuteSingleCommand case, cheat sheet in CodeEditor.uxml
