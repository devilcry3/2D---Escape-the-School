# Hallway Escape — Code Reading and Custom Wall Setup

**Audience:** instructors and students in grades 5–8. The gameplay scripts deliberately prefer clear multi-line `if` statements, descriptive variable names, explicit braces `{ }`, and explanatory comments over compact code. Advanced code is clearly marked as *instructor-owned*.

## 1. What to open first

| Learning order | File | Student-level concept | Instructor responsibility |
| --- | --- | --- | --- |
| 1 | `PlayerMovement2D.cs` | Variables, keyboard conditions, two-dimensional positions | Input package/physics setup |
| 2 | `KeyPickup2D.cs` | Trigger event, Boolean (`true`/`false`), calling a method | Make sure collider is a trigger |
| 3 | `LockedExit2D.cs` | `if` / `else`, checking a stored key | Assign barrier and visual references |
| 4 | `SpeedDrink2D.cs` | Multiplication and a time-limited effect | Configure power-up values |
| 5 | `HallMonitorAI2D.cs` | Patrol/chase state, distance comparisons | Usually provide this file prebuilt |
| 6 | `GridPathfinder2D.cs` | Grid cells, BFS (advanced) | Provide prebuilt; students edit map |
| 7 (optional) | `GameHUD2D.cs` | UI references, TextMeshPro text changes, `if/else` | Canvas built automatically; students can restyle it |
| Optional | `CameraFollow2D.cs`, `GameSession2D.cs` | Smooth following, win/lose state and restart | Provide prebuilt initially |
| Instructor only | `Editor/HallwayEscapeDemoBuilder.cs` | Editor automation | Generates the graybox prototype |

**Canvas note:** `GameHUD2D.cs` changes actual TextMeshPro labels. `GameSession2D.cs` tracks the game result; the two communicate without the old `OnGUI` API. See [`UI_CANVAS_GUIDE.md`](UI_CANVAS_GUIDE.md).

**Reading convention:** Every script begins with a purpose explanation; meaningful Unity lifecycle methods (`Awake`, `Update`, `FixedUpdate`, trigger events) have comments; alternative ways of writing code appear in separate `ALTERNATIVE SYNTAX` comment sections. Those examples are commented out so Unity does not run them accidentally.

## 2. C# Boolean comparisons (the important correction)

C# Boolean values use lowercase `true` and `false`. Unlike Python, `True` and `False` are not Boolean keywords in C#.

For a regular `bool` named `hasKey`, all of these mean **the value is false**:

```csharp
if (hasKey == false)
{
    // No key yet.
}

if (hasKey != true)
{
    // No key yet.
}

if (!hasKey)
{
    // No key yet.
}
```

Likewise, these mean **the value is true**:

```csharp
if (hasKey == true)
{
    // Player has the key.
}

if (hasKey)
{
    // Player has the key.
}
```

**Recommended teaching progression:** start with `== true` and `== false`, then demonstrate `!hasKey` once students understand the variable. Use the shorter form naturally after that. The main scripts intentionally mix a few forms so comments can compare them in their actual context.

| Syntax | Meaning | Beginner explanation |
| --- | --- | --- |
| `=` | Assignment | Store a new value (`hasKey = true;`) |
| `==` | Equality comparison | Ask if two values are equal |
| `!=` | Inequality comparison | Ask if two values differ |
| `!hasKey` | NOT | Reverse this Boolean value |
| `&&` | AND | Both sides must be true |
| `||` | OR | At least one side must be true |
| `x = x + 1;` | Add one and store it | Very explicit beginner form |
| `x += 1;` | Add one and store it | Shorthand |
| `x++;` | Add one | Shortest form for this operation |

### C# versus Scratch

Scratch `if <touching player?> then` is conceptually related to checking a collision or trigger. Unity automatically calls `OnTriggerEnter2D` when a configured trigger is entered; the code inside decides what happens next.

Scratch `set has key to true` maps to `HasKey = true;` in `PlayerMovement2D`.

Scratch `if <key W pressed?>` maps to `if (keyboard.wKey.isPressed)` after reading the current keyboard from the Input System.

`return;` stops running the **current method**. It does not end the entire game or delete the object.

`null` means an object reference is absent; it is *not* another spelling of `false`.

## 3. Does the hall monitor handle custom environments?

**YES for walls placed, moved, resized, or removed in the Unity Editor before pressing Play, with the requirements below.** The monitor does not have a pre-drawn route of lines to follow: the pathfinder samples actual 2D wall colliders when the scene starts, then finds routes through open grid cells. Patrol waypoints are *destinations*, not the exact route between them.

For a custom wall:

1. Create a GameObject or select an existing wall/tilemap obstacle.
2. Add an enabled **2D collider** appropriate to its geometry, e.g. `BoxCollider2D`, `PolygonCollider2D`, `TilemapCollider2D`, or `CompositeCollider2D`. A 3D `BoxCollider` is **not** interchangeable with `BoxCollider2D`.
3. **Turn `Is Trigger` OFF** for solid walls so player and monitor physically collide with them.
4. At the top of the wall's Inspector, choose the **`Walls` layer** (the builder creates it). For a Tilemap, put the object carrying the 2D collider on `Walls`.
5. Make sure both the `HALL MONITOR` component and the `NAVIGATION` component have `Wall Layers` containing `Walls`. The generated demo already sets these references.
6. Keep the navigable area *inside* the `NAVIGATION - grid bounds` `BoxCollider2D`. Resize that bounds object when you substantially enlarge the map.
7. Place all patrol waypoints **in open walkable locations**, not inside a wall, and leave sufficiently wide hallways for the enemy to fit.
8. Press **Play** to automatically rebuild navigation using the current layout.

### What is automatic and what is not?

| Change | Will the monitor find new detours automatically? |
| --- | --- |
| Move an interior wall in Scene view, then start Play | **Yes**, assuming a route still exists |
| Add a new solid wall on `Walls` layer, then start Play | **Yes**, assuming a route still exists |
| Use PolygonCollider2D instead of BoxCollider2D | **Yes**, if properly set up on `Walls` |
| Create an obstacle using only art, without a 2D collider | **No** — pathfinder has no obstacle to detect |
| Give the wall a 3D BoxCollider instead of BoxCollider2D | **No** — this project uses 2D physics only |
| Put the wall on a different, unselected layer | **No** — pathfinder and sight raycast ignore it |
| Move a wall while Play Mode is already running | **Not immediately** — manually rebuild grid or write a runtime rebuild call |
| Completely seal a room or put a waypoint inside a wall | **No** — a valid route may not exist |
| Put patrol points outside the grid rectangle | **No** — enlarge the play area first |

### Rebuilding navigation after runtime wall changes

The default is a *static level*: the navigation grid is created in `Awake` when the scene starts. It is **not** recalculated every physics step. This saves time and makes student prototypes simpler.

For instructor testing, in Play Mode select `NAVIGATION - grid bounds (trigger)` and use the `GridPathfinder2D` component context menu (`⋮` or right-click its heading) → **Rebuild Navigation Grid**. The hall monitor will use the rebuilt grid at its next route refresh (typically within `Repath Seconds`).

For later code-generated or destructible walls, the script responsible for changing the wall can call:

```csharp
// Example only, to run after a wall changes.
pathfinder.BuildGrid();
```

Avoid calling this every frame on a large level; rebuilding checks *every* grid cell for a wall collider. If changes are frequent, a more advanced pathfinding/update design would be appropriate.

### Configure the hall monitor

- `Patrol Points`: Drag empty waypoint objects into the array in patrol order. These need **no collider** and **no trigger**.
- `Detection Range`: Maximum distance at which it first notices a visible player; solid walls on `Wall Layers` block its line of sight.
- `Lose Range`: Distance past which it immediately gives up chasing.
- `Lost Sight Seconds`: How long it continues trying after a wall blocks sight (unless the player goes out of losing range or hides).
- `Patrol Speed` / `Chase Speed`: Movement speeds.
- `Repath Seconds`: Time between attempts to calculate a new route. Lower values follow a moving player more closely but require more calculation.
- `Cell Size`: On the Pathfinder, smaller cells improve detail but increase computation.
- `Clearance Factor`: Approximate space to leave around walls; revisit if corridors become narrow. Test that the collider fits physically.

**Important limitation:** This is a simple grid pathfinder using four-direction movement, not a sophisticated navigation mesh. It finds a route through connected cells if one exists; it does not promise the visually shortest/smoothest route. Its current vision check works in all directions (360°) rather than only in front of the monitor.

## 4. Two quick teacher tests

**Test A: Wall avoidance.** In the generated demo, add a solid rectangle across a direct hallway but leave another passage around it. Set its Layer to `Walls`, stop Play if running, then press Play. Watch the monitor walk around the obstacle toward waypoints or a visible player.

**Test B: Unreachable destination.** Surround one patrol point with wall colliders or place it inside a wall. The monitor should not walk through the wall. It will be unable to reach that point; move the waypoint back into open space and press Play again.

Do not treat the absence of a path as a successful route. A maze must have real connected passages wider than the monitor's collider.

## 5. Instruction design recommendation

Give beginners the entire working scene and prebuilt advanced scripts. On the first day, change **one behavior at a time**: movement speed, key state, drink duration, or text feedback. Avoid presenting the BFS code as part of their first C# lesson. Scratch-experienced students can begin combining conditions (`&&`, `||`) and edit the patrol/chase values; experienced students can inspect `HallMonitorAI2D` and later add a new enemy state.

The code comments are for learning, but this is **not compile-tested in a Unity Editor in this environment**. Verify once in your installed Unity version before distributing to students.
