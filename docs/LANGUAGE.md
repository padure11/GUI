# CODE BOTS Custom Language Reference

The player-facing programming language for controlling the robot. Parsed by `Lexer` → `Parser` → executed by `CommandExecutor` (single-player) or `GameSessionManager` (multiplayer).

## Commands

### Movement
| Command | Effect |
|---|---|
| `moveForward()` | Move 1 tile (2 units) forward, if no obstacle |
| `moveBack()` | Move 1 tile backward, if no obstacle |
| `turnLeft()` | Rotate 90° counter-clockwise (in place) |
| `turnRight()` | Rotate 90° clockwise (in place) |
| `jump()` | Boost jump — auto-detects: if a higher block ahead → climb up; if a gap with floor on the other side → jump over |

### Actions
| Command | Effect |
|---|---|
| `push()` | Push a PushableCrate 1 tile forward |
| `press()` | Press a PressButton in front (triggers its target) |
| `pull()` | Pull a Lever in front (toggles its connected targets) |
| `wait()` | Pause for 0.5s |

### Reserved (not yet implemented)
| Command | Notes |
|---|---|
| `grab()` | Listed in Lexer; for picking up keys/items (future) |
| `drop()` | Listed in Lexer; for dropping carried items (future) |

## Control flow

### Repeat block
```
repeat(3) {
  moveForward
  turnLeft
}
```
Runs the block N times.

### Function definition (no parameters)
```
function moveAndTurn {
  moveForward
  turnLeft
}
```
Defines a reusable block. Call it by name elsewhere in the code.

### Function call
```
moveAndTurn
moveForward
moveAndTurn
```
Functions can call other functions. Recursion is detected — max depth 100.

### Reserved (parsed but not executed)
- `while(condition) { ... }` — parsed, ignored in multiplayer
- `if(condition) { ... } else { ... }` — parsed, ignored in multiplayer

### Conditions (parsed but not evaluated)
Listed in Lexer: `obstacleAhead`, `nothingAhead`, `doorClosed`, `doorOpen`, `holding`, `notHolding`, `atEdge`. To be implemented when if/while are enabled.

## Syntax notes

- Parentheses after commands are optional: `moveForward` and `moveForward()` are equivalent.
- Whitespace and newlines are flexible — both `{ }` blocks and indentation work.
- Comments: `//` line comments not yet implemented in Lexer.
- Function names cannot shadow built-in commands (Parser warns).

## Example: solving a basic puzzle
```
function moveTwice {
  moveForward
  moveForward
}

moveTwice
pull              // turn off electric fence
moveTwice
press             // press finish button
```

## Execution model

### Single-player (CommandExecutor)
- Walks Command tree, runs each command sequentially.
- Function calls inline by looking up in dictionary.
- Used in test scenes (no NGO).

### Multiplayer (GameSessionManager, lockstep)
- Both players submit code via `SubmitCodeServerRpc`.
- Server lexes/parses both codes.
- `ExtractFunctions` separates function definitions into a dict.
- `FlattenCommands` expands `repeat` blocks and inlines function calls → flat list of command strings per robot.
- Server runs in lockstep: at each step, both robots execute their commands in parallel; server waits for both to finish before next step.
- If/while are stripped during flatten (not yet supported in lockstep).

## Adding a new command

1. Add to Lexer's `commands` HashSet
2. Add coroutine method to `PlayerController` (e.g., `MyCommand()`)
3. Add case to `CommandExecutor.ExecuteCommand` switch
4. Add case to `GameSessionManager.ExecuteSingleCommand` switch
5. Add to cheat sheet in `Assets/UI/CodeEditor/CodeEditor.uxml`

## Adding a new control flow keyword

1. Add to Lexer's `keywords` HashSet
2. Add Parser method `ParseMyKeyword` returning a Command
3. Add case to `Parser.ParseCommand` switch
4. Add execution case to `CommandExecutor` and `GameSessionManager` if it has runtime behavior
