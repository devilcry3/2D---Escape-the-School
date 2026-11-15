# Hallway Escape — Unity 2D graybox prototype

> **READ THIS FIRST (Canvas classroom edition):** All ten gameplay/UI scripts and the Editor scene generator have been rewritten for readability: descriptive variable names, multi-line statements, braces on conditionals, explanations of Unity event methods, and clearly separated `ALTERNATIVE SYNTAX` comments. See [`CODE_READING_AND_WALL_SETUP.md`](CODE_READING_AND_WALL_SETUP.md) for Boolean examples (`!hasKey`, `hasKey == false`, `hasKey != true`) and a full custom-wall checklist. **C# uses lowercase `true` and `false`.**

> **Custom maps:** YES—walls edited before Play are incorporated automatically when the navigation grid is created. Give solid objects a **2D Collider**, put them on the **Walls layer**, keep corridors/waypoints traversable and within the **Navigation** rectangle. Changes to walls during Play require an explicit `BuildGrid()` or the component's **Rebuild Navigation Grid** context-menu command.


A small **code-first school escape demo** designed for a group teaching C# to grades 5–8. No external art packs, tilemaps, pathfinding packages, or NavMesh extensions are required. All world art is a tinted square/circle placeholder. The on-screen interface uses **Canvas + TextMeshPro**, with an interactive Restart Button. See [`UI_CANVAS_GUIDE.md`](UI_CANVAS_GUIDE.md).

## Requirements

- Recommended editor: **Unity 6**, using a 2D Core project. The scripts also use APIs available in many Unity 2022.3+ versions, but the demo menu has only been written for Unity 6 and has **not been run inside a Unity Editor here**.
- **Input System** package (`com.unity.inputsystem`) enabled. This project reads the keyboard directly from the new Input System; it does not require you to create an Input Actions asset.
- **Unity UI / uGUI and TextMeshPro** (included with typical Unity 6 2D projects). Import **TMP Essential Resources** from **Window > TextMeshPro > Import TMP Essential Resources** before generating the scene if your project does not have a default TextMeshPro font asset. The generator checks and prompts if missing.
- A desktop keyboard for playtesting (WASD or arrow keys).

## Installation — roughly one import and one menu click

1. Create/open a **2D Core** Unity project.
2. In Unity, open **Window > Package Manager > Unity Registry > Input System > Install**. If prompted, enable the new input back end and let Unity restart. You can check **Edit > Project Settings > Player > Other Settings > Active Input Handling**; use **Input System Package (New)** (or **Both**). Check that **Unity UI (uGUI)** is installed.
3. Choose **Window > TextMeshPro > Import TMP Essential Resources** if those resources are not already present. This supplies the TextMeshPro font asset for the Canvas HUD.
4. Close Unity if convenient, and **extract the ZIP to the project root** (the folder that contains the existing `Assets` folder). The ZIP contains `Assets/HallwayEscape/Scripts`, `/Editor`, and `/Placeholders`.
5. Reopen Unity and wait for scripts to compile. If a `UnityEngine.InputSystem` compiler error occurs, Step 2 was not completed.
6. Choose **Tools > Hallway Escape > Create Demo Scene**. The builder also creates an editable Canvas, TextMeshPro labels, and the input EventSystem.
7. Open the **Game** view and press **Play**. Use **WASD** or **arrow keys**. Touch the **gold key** to unlock the **red exit** (it turns green). Touch the **cyan drink** for a short speed boost. Avoid the **orange hall monitor**. After winning/losing, click **RESTART** on the Canvas or press **R** to restart.

The menu **creates or reuses a user layer called `Walls`**, saves `Assets/HallwayEscape/Scenes/HallwayEscape_Demo.unity`, and adds that scene to Build Settings. It asks before overwriting the generated demo scene and gives you an opportunity to save other modified scenes. **Make a copy of the demo scene before extensively editing it, or don't run the builder again.**

## Scene hierarchy and Inspector controls

| Object in Hierarchy | Script / component | Useful Inspector controls |
| --- | --- | --- |
| `PLAYER` | `PlayerMovement2D`, `Rigidbody2D`, `BoxCollider2D` | `Move Speed` (default 4) |
| `HALL MONITOR` | `HallMonitorAI2D`, `Rigidbody2D`, `CircleCollider2D` | `Detection Range`, `Chase Speed`, `Lose Range`, `Repath Seconds`, `Patrol Points` |
| `NAVIGATION - grid bounds` | `GridPathfinder2D`, trigger `BoxCollider2D` | `Wall Layers`, `Cell Size`, `Clearance Factor`, `Play Area` |
| `PATROL WAYPOINTS` children | Empty GameObject transforms | Drag waypoints in the Scene to change patrol destinations |
| Interior and border walls | `BoxCollider2D`, `Walls` layer | Move/resize, keep layer as `Walls` |
| `GOLD KEY` | `KeyPickup2D`, trigger `CircleCollider2D` | Position |
| `BLUE DRINK` | `SpeedDrink2D`, trigger `CircleCollider2D` | `Speed Multiplier`, `Duration Seconds` |
| `EXIT` | `LockedExit2D`, solid + trigger collider | `Player`, `Solid Barrier`, `Door Visual` |
| `GAME SESSION` | `GameSession2D` | `Player`, `Hud` references; win/lose/restart logic |
| `GAME HUD CANVAS` | `Canvas`, `CanvasScaler`, `GameHUD2D` | TextMeshPro fields, Image colors, anchors, panel layout |
| `EVENT SYSTEM` | `EventSystem`, `InputSystemUIInputModule` | Receives UI mouse and keyboard events (leave prebuilt) |
| `Main Camera` | `CameraFollow2D` | `Target`, `Follow Sharpness` |

### How pathfinding works (instructor-level; skip for novices)

- `GridPathfinder2D` samples a rectangle at scene startup, marking a small cell **blocked** if its overlap box hits a collider on the `Walls` layer.
- `HallMonitorAI2D` follows normal **Transform waypoints**. It does **not** need tiny trigger colliders at each destination.
- Each time the hall monitor plans a move, the grid script runs **breadth-first search (BFS)** on the four adjacent cells (up, right, down, left). This prevents routes directly through wall cells. It recalculates the enemy's route periodically while chasing.
- The hall monitor detects players within a radius **only if a wall raycast is clear**. There is no directional vision cone; awareness is currently 360 degrees. Losing distance immediately ends the chase; breaking sight for about 1.25 seconds also ends the chase.
- When a chase ends, the monitor chooses a nearby patrol point and walks back along the grid route.
- This simple grid is **built once at Play**. Changing walls while the game is running will NOT rebuild it. For the intended static classroom test level, that is fine. Call `BuildGrid()` after changing collision geometry at runtime in a later lesson.
- The wall sizes/room spacing in the supplied scene are set to work with the 0.5-unit grid and the monitor size. When making much narrower halls, adjust `Cell Size` and `Clearance Factor` and retest.

## Recommended lesson split

### Tier 1: complete beginners (5th grade / no prior Scratch)

- First **play** the game, then change one value in the Inspector: `Move Speed = 4` to `Move Speed = 6`.
- Open `PlayerMovement2D.cs` and find the `if` statements for WASD/arrow input.
- In `KeyPickup2D.cs`, explain **event** (trigger), **object** (player), and the **Boolean** `HasKey`.
- Change the key position or the length of the speed drink in the Inspector. Students should not edit BFS/pathfinding.

### Tier 2: Scratch-experienced students (roughly 6th–8th)

- Compare Scratch **when key pressed** with C# `keyboard.wKey.isPressed`.
- Compare Scratch **if touching** with Unity `OnTriggerEnter2D`.
- Trace the flow: collide with key → call `CollectKey()` → `HasKey = true` → door becomes unlocked.
- Change `SpeedDrink2D` to make a slower or faster drink. Introduce variables, methods, conditions, and timers.
- Change `Detection Range` and discuss what makes the monitor fair or frustrating.

### Tier 3: ready for a challenge

- Add collectible items that increment a counter, rather than a Boolean.
- Add a second hall monitor with a different patrol route or chase speed.
- Add a locker: when the player is within its trigger and presses `E`, call `player.SetHidden(true)`; pressing `E` again calls `player.SetHidden(false)`. You must also make the player inaccessible to physical contact while hidden or ensure the locker geometry shields them. The player and enemy scripts already include the `IsHidden` property for this optional extension, but **the locker interaction itself is not implemented**.
- Add a directional field of view to the hall monitor, or convert to Input Actions for rebindable controls/gamepads.

## Notes / troubleshooting

- **Compile errors mentioning InputSystem**: install and enable the Input System package first.
- **Player goes through walls**: walls must have enabled `BoxCollider2D`; player's Rigidbody2D must be `Dynamic`, Gravity Scale 0. Avoid using `Kinematic` without understanding its collision rules.
- **Enemy tries to move through walls**: every wall must be on the `Walls` layer, and the monitor/pathfinder `Wall Layers` masks must include that layer. Restart Play after wall changes.
- **Enemy stops after you edit the map**: its waypoint or the player may have ended up inside a blocked grid cell, or a hallway may be narrower than the clearance requirement. Select the hall monitor to see its patrol and detection gizmos.
- **Key/drink not collected**: their Collider2D components must have `Is Trigger` checked; the player must have a Rigidbody2D and Collider2D.
- **Restart Button or R doesn't restart**: ensure the demo scene is enabled in **File > Build Profiles > Scene List** (Unity 6), that the Canvas Button event points to `GAME SESSION > GameSession2D.RestartLevel()`, and that the EventSystem uses the Input System UI Input Module.
- **Old input vs new input**: this package intentionally uses `UnityEngine.InputSystem.Keyboard`. For beginners, direct keyboard reads avoid the setup of Action Maps while still using Unity's modern input backend. For a later multi-device project, switch to an `InputActionAsset` / `PlayerInput` setup.
- **Canvas HUD**: the scene generator builds an editable Unity UI Canvas using TextMeshPro. No `OnGUI()` drawing is used. See `UI_CANVAS_GUIDE.md` for a click-by-click guide.
- **TextMeshPro/font error**: import TMP Essential Resources, then rerun the demo scene builder.

## What is/isn't done

**Included:** movement, physical wall collisions, waypoint patrol, obstacle-aware BFS route planning, wall-blocked detection, chase/lose/resume, a key pickup, a genuinely solid locked exit that unlocks, a timed drink boost, win/lose feedback, keyboard/button restart, camera follow, a TextMeshPro Canvas HUD with a win/lose panel, and a one-click graybox scene generator.

**Not included:** production art, audio, animation, directional vision, hiding/locker interaction, mobile or gamepad controls, multiple levels, dynamic pathfinding updates, or production-quality UI styling. This is meant to be a starting prototype, not a finished game.

**Testing limitation:** sources and level connectivity were reviewed, but the Unity Editor is not available in this environment for an actual compile/playtest. Test the generated demo in your installed Unity version and adjust settings if necessary.
