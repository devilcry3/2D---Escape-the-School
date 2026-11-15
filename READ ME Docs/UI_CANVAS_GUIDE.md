# Hallway Escape — Canvas HUD & TextMeshPro Guide (v3)

This guide describes the **actual Unity Canvas** version. The prototype no longer uses `OnGUI()`. All the visible UI elements are ordinary GameObjects that you can click, move, resize, recolor, and style in the Unity Editor.

## 1. What is in the generated scene?

```text
GAME SESSION - win and lose logic
    GameSession2D.cs              <- knows if the game ended; reloads the level

GAME HUD CANVAS - edit colors, layout, and text here
    Canvas
    CanvasScaler                 <- scales UI for different screen resolutions
    GraphicRaycaster             <- supports UI clicks
    GameHUD2D.cs                 <- updates TextMeshPro labels and panels
    STATUS PANEL - top left
        Title - SCHOOL ESCAPE
        Controls - WASD or arrows
        Key Status - automatically updated
        Speed Boost - automatically updated
    MESSAGE PANEL - temporary tips
        Message Text - edit its formatting
    RESULT OVERLAY - shown on win or lose
        RESULT BOX
            Result Title - win or lose
            Result Instructions
            RESTART BUTTON - UI Button
                Restart Button Text

EVENT SYSTEM - Canvas mouse and keyboard
    EventSystem
    InputSystemUIInputModule     <- reads clicks through the NEW Input System
```

The Canvas is in **Screen Space - Overlay** mode: it remains on the screen when the camera moves. The `CanvasScaler` uses **Scale With Screen Size**, reference resolution **1280 × 720**, and matches width/height equally. This is a good starting configuration for a classroom desktop game.

## 2. How to create it (no manual wiring)

1. Use Unity **6 / 2D Core**, with the **Input System** package installed and enabled (New, or Both).
2. Unity UI (uGUI) and TextMeshPro must be available. If the project has no TextMeshPro font assets, choose **Window → TextMeshPro → Import TMP Essential Resources**. On some versions, the menu wording may appear under the TextMeshPro menu in the top bar.
3. Extract the starter ZIP into the Unity project's root directory, merging its `Assets` folder. **Do not leave the extracted `Assets` directory nested inside another `Assets` directory.**
4. Allow scripts to compile. Choose **Tools → Hallway Escape → Create Demo Scene**.
5. Open the generated scene (usually already open) and press **Play**.
6. Collect the key and reach the exit to win, or let the monitor catch you to lose. Click the **RESTART** button or press **R** to play again.

If the generator warns that it cannot find a TextMeshPro font, use **Window → TextMeshPro → Import TMP Essential Resources**, then repeat step 4. It checks for a font *before* replacing the scene.

**Warning:** Running the generator again can overwrite your edited `HallwayEscape_Demo.unity`. Make a copy of the scene before substantial editing.

## 3. Visually editing the HUD (an ideal student task)

1. Stop Play Mode (runtime text changes are not saved to the scene).
2. In the Hierarchy, expand `GAME HUD CANVAS - edit colors, layout, and text here`.
3. Select `STATUS PANEL - top left`, `RESULT BOX`, or any TextMeshPro child.
4. In the **Inspector**, change a `Rect Transform` position/size, an `Image` color, or a `TextMeshPro - Text (UI)` font size and color.
5. Press **Play** again. The placement, sizing, and styles are preserved.

**Do not rename a script's fields** if you are only changing the appearance. The generator assigns the references for you. Changing a static title is safe. Labels like `Key Status` and `Speed Boost` are updated by `GameHUD2D.cs` every frame, so their starting text will be replaced during Play.

## 4. How the code communicates with the Canvas

The player collects the key:

```text
Player touches gold key
    -> KeyPickup2D tells PlayerMovement2D it has the key
    -> PlayerMovement2D.HasKey becomes true
    -> GameHUD2D reads HasKey in Update()
    -> TextMeshPro label displays "Key: FOUND"
```

The player wins:

```text
Player reaches unlocked exit
    -> LockedExit2D calls GameSession2D.Instance.Win()
    -> GameSession2D changes IsFinished to true
    -> GameSession2D calls Hud.ShowResult(true)
    -> GameHUD2D enables the result panel
    -> The Restart Button calls GameSession2D.RestartLevel()
```

This separation matters: gameplay scripts do not need to know *where* a text label is positioned, and the UI does not decide the rules for collecting keys.

## 5. Understanding the different UI components

| Component | Purpose | Typical student activity |
| --- | --- | --- |
| `Canvas` | Root for on-screen UI | Learn what belongs in the HUD |
| `RectTransform` | UI anchors, position, size | Move a status panel to the other side |
| `Image` | Flat colored panel or button background | Change colors |
| `TextMeshProUGUI` | Text with styling and alignment | Change font size/wording |
| `Button` | Clickable interface control | Add another menu action later |
| `EventSystem` | Delivers clicks and UI events | Usually leave untouched |
| `InputSystemUIInputModule` | Connects the newer Input System to UI | Instructor-owned setup |
| `GameHUD2D` | Updates the on-screen status | Learn variables, if/else, text properties |
| `GameSession2D` | Holds game outcome, restart logic | Learn method calls and Boolean state |

## 6. If you are making your own scene

The scene generator automatically wires these references; a **new empty scene does not**. You can duplicate the generated scene and edit its walls freely. If building from scratch, you must also create and wire the Canvas, `GameHUD2D`, `GameSession2D`, `EventSystem`, and every referenced HUD label/panel. The easiest instructor workflow is to duplicate the generated scene, then delete/replace the graybox geometry while keeping `GAME SESSION`, `GAME HUD CANVAS`, and `EVENT SYSTEM` intact.

The hall monitor's wall navigation is **independent** of the Canvas. Walls on the `Walls` layer with proper solid 2D colliders are sampled on Play. If you rearrange walls during Play, request a navigation-grid rebuild manually.

## 7. Common UI problems

- **UI text blank or pink/missing:** Import TextMeshPro Essential Resources and verify each label has a Font Asset.
- **`UnityEngine.UI` or `TMPro` doesn't compile:** Confirm Unity UI/uGUI is installed in Package Manager; verify Unity version and packages.
- **Restart button doesn't respond:** Ensure the generated `EVENT SYSTEM` exists with `InputSystemUIInputModule`, that the new Input System is enabled, and that the Button's On Click event points to `GAME SESSION -> GameSession2D.RestartLevel()`.
- **HUD cut off at a very narrow game window:** Expand the Game view and adjust the Canvas Scaler's reference resolution and panel anchors if needed. The default layout targets typical desktop landscape resolutions.
- **Changed a label but it reverts in Play:** Key/boost/result text is intentionally set by `GameHUD2D`; change those strings in its script if you want different runtime wording.

## 8. Teaching suggestion

For a complete beginner: first move the key-status label in the Inspector, then change its color, then open `GameHUD2D.cs` and find the `if (GameSession.Player.HasKey == true)` statement. This connects **visual design → a variable → an if/else decision → the text displayed**. Students who used Scratch can then edit the conditional messages, and advanced students can write a second HUD field (e.g., number of keys collected).

**Testing status:** the code and required references were inspected, but the Unity Editor is not installed in this environment. The generator and button should be compile- and play-tested in the project before classroom distribution.
