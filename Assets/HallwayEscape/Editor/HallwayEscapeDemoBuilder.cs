#if UNITY_EDITOR
// Everything in this file runs only in the Unity Editor, NOT in a built game.
// The #if / #endif lines prevent the game build from needing UnityEditor.

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

/*
 * DEMO SCENE BUILDER — INSTRUCTOR-ONLY UTILITY
 *
 * This script creates the starter game objects automatically:
 * floor, walls, player, hall monitor, waypoints, navigation, key, drink,
 * exit, HUD Canvas (TextMeshPro), Restart Button, and camera.
 *
 * All visible art is a simple square/circle placeholder.
 *
 * TEACHER WARNING:
 * Re-running this menu can replace the generated demo scene. Make a
 * separate copy of your edited scene before using the menu again.
 *
 * THIS IS NOT A REQUIRED STUDENT LESSON. It uses advanced Editor APIs.
 */
public static class HallwayEscapeDemoBuilder
{
    private const string AssetRoot = "Assets/HallwayEscape";
    private const string DemoScenePath = AssetRoot + "/Scenes/HallwayEscape_Demo.unity";
    private const string WhiteSquarePath = AssetRoot + "/Placeholders/white-square.png";
    private const string WhiteCirclePath = AssetRoot + "/Placeholders/white-circle.png";

    // The MenuItem attribute adds a clickable command to Unity's top menu.
    [MenuItem("Tools/Hallway Escape/Create Demo Scene")]
    public static void CreateDemoScene()
    {
        // First let the instructor save any other work that is open.
        bool okayToContinue = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        if (okayToContinue == false)
        {
            return;
        }

        // Protect the already-generated scene from accidental overwrites.
        SceneAsset previousDemo = AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScenePath);
        if (previousDemo != null)
        {
            bool confirmedReplacement = EditorUtility.DisplayDialog(
                "Replace demo scene?",
                "The generated HallwayEscape_Demo scene already exists. " +
                "Rebuilding will overwrite changes to THAT scene. " +
                "Make a separate copy first if you want to keep your changes.",
                "Replace demo scene",
                "Cancel"
            );

            if (confirmedReplacement == false)
            {
                return;
            }
        }

        int wallLayerNumber = FindOrCreateLayer("Walls");
        if (wallLayerNumber < 0)
        {
            EditorUtility.DisplayDialog(
                "No available Unity layers",
                "Create a layer called Walls in Tags and Layers, then run the menu again.",
                "OK"
            );
            return;
        }

        Sprite squareSprite = LoadPlaceholderSprite(WhiteSquarePath);
        Sprite circleSprite = LoadPlaceholderSprite(WhiteCirclePath);
        if (squareSprite == null || circleSprite == null)
        {
            EditorUtility.DisplayDialog(
                "Placeholder images are missing",
                "Extract the entire ZIP into the Unity project's root folder, " +
                "including Assets/HallwayEscape/Placeholders.",
                "OK"
            );
            return;
        }

        // TextMeshPro fonts are separate resources in a fresh Unity project.
        // Check before replacing the current scene so a missing font never
        // results in a partially generated or unsaved demo.
        TMP_FontAsset hudFont = FindInterfaceFont();
        if (hudFont == null)
        {
            EditorUtility.DisplayDialog(
                "Import TextMeshPro Essential Resources",
                "In Unity, choose Window > TextMeshPro > Import TMP Essential Resources " +
                "and then run Tools > Hallway Escape > Create Demo Scene again. " +
                "The Canvas needs a TextMeshPro font asset.",
                "OK"
            );
            return;
        }

        if (AssetDatabase.IsValidFolder(AssetRoot + "/Scenes") == false)
        {
            AssetDatabase.CreateFolder(AssetRoot, "Scenes");
        }

        // EmptyScene means we create the camera and every object ourselves.
        Scene newScene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single
        );

        GameObject environmentGroup = new GameObject("ENVIRONMENT - edit walls and classrooms here");
        GameObject pickupGroup = new GameObject("PICKUPS - key and speed drink");
        GameObject waypointGroup = new GameObject("PATROL WAYPOINTS - drag to edit the route");

        // The FLOOR is visual only. It has no Collider2D and cannot block.
        GameObject floor = CreateColoredShape(
            "Floor",
            new Vector2(0f, 0f),
            new Vector2(24f, 14f),
            new Color(0.18f, 0.20f, 0.24f),
            squareSprite, -10, environmentGroup.transform
        );

        // Exterior walls. Every wall receives a BoxCollider2D AND "Walls" layer.
        CreateSolidWall("Top border", 0f, 6.75f, 24f, 0.6f,
            wallLayerNumber, squareSprite, environmentGroup.transform);
        CreateSolidWall("Bottom border", 0f, -6.75f, 24f, 0.6f,
            wallLayerNumber, squareSprite, environmentGroup.transform);
        CreateSolidWall("Left border", -11.7f, 0f, 0.6f, 13.5f,
            wallLayerNumber, squareSprite, environmentGroup.transform);
        CreateSolidWall("Right border", 11.7f, 0f, 0.6f, 13.5f,
            wallLayerNumber, squareSprite, environmentGroup.transform);

        // Interior obstacles force the monitor to walk around them.
        CreateSolidWall("Classroom divider - vertical", -2.5f, 0f, 0.65f, 8f,
            wallLayerNumber, squareSprite, environmentGroup.transform);
        CreateSolidWall("Classroom divider - horizontal", 3.8f, -1.5f, 7.5f, 0.65f,
            wallLayerNumber, squareSprite, environmentGroup.transform);
        CreateSolidWall("Locker bank - noninteractive", 4.5f, 3.1f, 0.6f, 3.2f,
            wallLayerNumber, squareSprite, environmentGroup.transform);

        // PLAYER (blue square): Dynamic Rigidbody2D, collider, C# behavior.
        GameObject playerObject = CreateColoredShape(
            "PLAYER", new Vector2(-8.5f, -4.5f), new Vector2(0.62f, 0.62f),
            new Color(0.20f, 0.75f, 1f), squareSprite, 4
        );
        playerObject.AddComponent<BoxCollider2D>();
        Rigidbody2D playerRigidbody = playerObject.AddComponent<Rigidbody2D>();
        ConfigureCharacterPhysics(playerRigidbody);
        PlayerMovement2D playerScript = playerObject.AddComponent<PlayerMovement2D>();

        // PLAY AREA: a large TRIGGER is used only to measure grid bounds.
        // The hall monitor's BFS navigator will sample walls inside it.
        GameObject navigationObject = new GameObject("NAVIGATION - grid bounds (trigger)");
        BoxCollider2D navigationRectangle = navigationObject.AddComponent<BoxCollider2D>();
        navigationRectangle.size = new Vector2(23f, 13f);
        navigationRectangle.isTrigger = true;

        GridPathfinder2D pathfinder = navigationObject.AddComponent<GridPathfinder2D>();
        pathfinder.PlayArea = navigationRectangle;
        pathfinder.CellSize = 0.5f;
        pathfinder.ClearanceFactor = 1.4f;
        pathfinder.WallLayers = 1 << wallLayerNumber;

        // Waypoints are EMPTY objects (Transforms). No trigger is needed.
        Transform[] patrolWaypoints = new Transform[4];
        patrolWaypoints[0] = CreateWaypoint("Waypoint 1", -8f, 3.5f, waypointGroup.transform);
        patrolWaypoints[1] = CreateWaypoint("Waypoint 2", 8f, 3.5f, waypointGroup.transform);
        patrolWaypoints[2] = CreateWaypoint("Waypoint 3", 8f, -3.5f, waypointGroup.transform);
        patrolWaypoints[3] = CreateWaypoint("Waypoint 4", -8f, -3.5f, waypointGroup.transform);

        // HALL MONITOR (orange circle).
        GameObject monitorObject = CreateColoredShape(
            "HALL MONITOR", new Vector2(-8f, 3.5f), new Vector2(0.7f, 0.7f),
            new Color(1f, 0.40f, 0.20f), circleSprite, 4
        );
        CircleCollider2D monitorCollider = monitorObject.AddComponent<CircleCollider2D>();
        monitorCollider.radius = 0.49f;
        Rigidbody2D monitorRigidbody = monitorObject.AddComponent<Rigidbody2D>();
        ConfigureCharacterPhysics(monitorRigidbody);

        HallMonitorAI2D monitorScript = monitorObject.AddComponent<HallMonitorAI2D>();
        monitorScript.Player = playerScript;
        monitorScript.Pathfinder = pathfinder;
        monitorScript.PatrolPoints = patrolWaypoints;
        monitorScript.WallLayers = 1 << wallLayerNumber;

        // KEY: a gold pickup with trigger collision, NOT a physical wall.
        GameObject keyObject = CreateColoredShape(
            "GOLD KEY - pickup", new Vector2(8.6f, 5.1f), new Vector2(0.52f, 0.52f),
            new Color(1f, 0.85f, 0.18f), circleSprite, 3, pickupGroup.transform
        );
        CircleCollider2D keyCollider = keyObject.AddComponent<CircleCollider2D>();
        keyCollider.isTrigger = true;
        keyObject.AddComponent<KeyPickup2D>();

        // DRINK: another trigger, with an adjustable speed multiplier.
        GameObject drinkObject = CreateColoredShape(
            "CYAN DRINK - speed boost", new Vector2(-6f, -2.8f), new Vector2(0.55f, 0.55f),
            new Color(0.20f, 1f, 0.95f), circleSprite, 3, pickupGroup.transform
        );
        CircleCollider2D drinkCollider = drinkObject.AddComponent<CircleCollider2D>();
        drinkCollider.isTrigger = true;
        drinkObject.AddComponent<SpeedDrink2D>();

        // EXIT: one solid collider for blocking, plus a trigger to check entry.
        GameObject exitObject = CreateColoredShape(
            "EXIT - red until key", new Vector2(9.2f, -4.65f), new Vector2(0.95f, 1.1f),
            Color.red, squareSprite, 3
        );
        BoxCollider2D doorBlocker = exitObject.AddComponent<BoxCollider2D>();
        CircleCollider2D exitTrigger = exitObject.AddComponent<CircleCollider2D>();
        exitTrigger.isTrigger = true;
        exitTrigger.radius = 1.25f;

        LockedExit2D doorScript = exitObject.AddComponent<LockedExit2D>();
        doorScript.Player = playerScript;
        doorScript.SolidBarrier = doorBlocker;
        doorScript.DoorVisual = exitObject.GetComponent<SpriteRenderer>();

        // GameSession2D remembers win/lose state. A separate Canvas HUD
        // shows the information: this makes game logic and visual UI distinct.
        GameObject sessionObject = new GameObject("GAME SESSION - win and lose logic");
        GameSession2D gameSession = sessionObject.AddComponent<GameSession2D>();
        gameSession.Player = playerScript;

        // Create a real, editable Unity Canvas with TextMeshPro labels.
        // The restart button uses the modern Input System for mouse input.
        CreateCanvasHud(gameSession, hudFont);

        // CAMERA: orthographic means no perspective shrinking in this 2D game.
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera mainCamera = cameraObject.AddComponent<Camera>();
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 6.8f;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.10f, 0.11f, 0.14f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(-8.5f, -4.5f, -10f);

        CameraFollow2D cameraFollow = cameraObject.AddComponent<CameraFollow2D>();
        cameraFollow.Target = playerObject.transform;

        // Save to a real .unity scene asset and add it to the build list.
        EditorSceneManager.MarkSceneDirty(newScene);
        EditorSceneManager.SaveScene(newScene, DemoScenePath);
        AddSceneToBuildSettings(DemoScenePath);

        Selection.activeGameObject = playerObject;
        EditorGUIUtility.PingObject(playerObject);
        Debug.Log("Hallway Escape demo scene created. Press Play to test it!");
    }

    // Locate a font from TextMeshPro Essential Resources. The starter avoids
    // shipping fonts; Unity's own TextMeshPro resources provide the glyphs.
    private static TMP_FontAsset FindInterfaceFont()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font != null)
        {
            return font;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"
        );

        return font;
    }

    /*
     * CANVAS CREATION — INSTRUCTOR-ONLY EDITOR CODE
     *
     * The generated UI is made of normal Unity objects. After the scene
     * is created, teachers/students edit the Canvas in Scene/Hierarchy view.
     * The coordinates below are setup values, NOT the gameplay UI logic.
     *
     * Canvas uses anchors instead of the Screen.width rectangle calculations
     * that an old OnGUI() interface would require.
     */
    private static void CreateCanvasHud(GameSession2D gameSession, TMP_FontAsset font)
    {
        // Screen Space - Overlay draws the UI on top of the 2D world.
        GameObject canvasObject = new GameObject(
            "GAME HUD CANVAS - edit colors, layout, and text here",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        // Scale With Screen Size keeps labels readable at other resolutions.
        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1280f, 720f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        GameHUD2D hud = canvasObject.AddComponent<GameHUD2D>();
        hud.GameSession = gameSession;
        gameSession.Hud = hud;

        // Upper-left status panel: title, controls, key, and speed boost.
        RectTransform statusPanel = CreateUiRectangle(
            "STATUS PANEL - top left",
            canvasObject.transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(20f, -20f),
            new Vector2(360f, 170f)
        );
        AddColoredPanel(statusPanel.gameObject, new Color(0.08f, 0.12f, 0.18f, 0.88f), false);

        CreateTextElement(
            "Title - SCHOOL ESCAPE", statusPanel, font, "SCHOOL ESCAPE",
            30f, FontStyles.Bold, TextAlignmentOptions.Left,
            new Vector2(14f, -12f), new Vector2(330f, 40f)
        );
        CreateTextElement(
            "Controls - WASD or arrows", statusPanel, font, "Move: WASD or arrow keys",
            21f, FontStyles.Normal, TextAlignmentOptions.Left,
            new Vector2(14f, -55f), new Vector2(330f, 27f)
        );
        hud.KeyStatusText = CreateTextElement(
            "Key Status - automatically updated", statusPanel, font, "Key: Not found",
            22f, FontStyles.Normal, TextAlignmentOptions.Left,
            new Vector2(14f, -94f), new Vector2(330f, 28f)
        );
        hud.SpeedBoostText = CreateTextElement(
            "Speed Boost - automatically updated", statusPanel, font, "Speed boost: Inactive",
            21f, FontStyles.Normal, TextAlignmentOptions.Left,
            new Vector2(14f, -131f), new Vector2(330f, 28f)
        );

        // Temporary feedback appears above the bottom edge of the screen.
        RectTransform messagePanel = CreateUiRectangle(
            "MESSAGE PANEL - temporary tips",
            canvasObject.transform,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 24f),
            new Vector2(560f, 62f)
        );
        AddColoredPanel(messagePanel.gameObject, new Color(0.08f, 0.12f, 0.18f, 0.95f), false);
        hud.MessageText = CreateCenteredTextElement(
            "Message Text - edit its formatting", messagePanel, font,
            "Find the key, then reach the exit.", 23f,
            Vector2.zero, new Vector2(536f, 54f)
        );
        hud.MessagePanel = messagePanel.gameObject;

        // The final overlay fills the screen. It is initially hidden by HUD.
        RectTransform fullScreenOverlay = CreateUiRectangle(
            "RESULT OVERLAY - shown on win or lose",
            canvasObject.transform,
            Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero
        );
        AddColoredPanel(fullScreenOverlay.gameObject, new Color(0.02f, 0.04f, 0.07f, 0.75f), false);
        hud.ResultPanel = fullScreenOverlay.gameObject;

        RectTransform resultBox = CreateUiRectangle(
            "RESULT BOX", fullScreenOverlay,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 272f)
        );
        AddColoredPanel(resultBox.gameObject, new Color(0.15f, 0.21f, 0.30f, 1f), false);

        hud.ResultTitleText = CreateCenteredTextElement(
            "Result Title - win or lose", resultBox, font,
            "YOU ESCAPED!", 31f, new Vector2(0f, 73f), new Vector2(588f, 80f)
        );
        hud.ResultInstructionsText = CreateCenteredTextElement(
            "Result Instructions", resultBox, font,
            "Click Restart or press R to try again.", 21f,
            new Vector2(0f, 5f), new Vector2(578f, 42f)
        );

        RectTransform restartRectangle = CreateUiRectangle(
            "RESTART BUTTON - UI Button", resultBox,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -80f),
            new Vector2(232f, 58f)
        );
        Image buttonBackground = AddColoredPanel(
            restartRectangle.gameObject, new Color(0.13f, 0.60f, 0.80f, 1f), true
        );
        Button restartButton = restartRectangle.gameObject.AddComponent<Button>();
        restartButton.targetGraphic = buttonBackground;
        restartButton.transition = Selectable.Transition.ColorTint;
        CreateCenteredTextElement(
            "Restart Button Text", restartRectangle, font, "RESTART",
            25f, Vector2.zero, new Vector2(220f, 52f)
        );

        // AddPersistentListener saves the button connection IN THE SCENE.
        // Using onClick.AddListener here would only set up a temporary,
        // non-serialized callback in an Editor scene builder.
        UnityEventTools.AddPersistentListener(
            restartButton.onClick, gameSession.RestartLevel
        );
        EditorUtility.SetDirty(restartButton);

        // Without an EventSystem, clickable Canvas Buttons cannot work.
        // InputSystemUIInputModule supports the newer Unity Input System.
        GameObject eventSystemObject = new GameObject("EVENT SYSTEM - Canvas mouse and keyboard");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();

        // Start hidden; GameHUD2D.Awake also hides these at runtime.
        messagePanel.gameObject.SetActive(false);
        fullScreenOverlay.gameObject.SetActive(false);
    }

    // Create a RectTransform with anchors and a position relative to them.
    // For example, (0,1) means top-left and (0.5,0.5) means center.
    private static RectTransform CreateUiRectangle(
        string objectName, Transform parent,
        Vector2 anchorMinimum, Vector2 anchorMaximum,
        Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        RectTransform rectangle = uiObject.GetComponent<RectTransform>();
        rectangle.anchorMin = anchorMinimum;
        rectangle.anchorMax = anchorMaximum;
        rectangle.pivot = pivot;
        rectangle.anchoredPosition = anchoredPosition;
        rectangle.sizeDelta = size;
        return rectangle;
    }

    // Image is a Unity UI component, not the same as SpriteRenderer.
    private static Image AddColoredPanel(GameObject uiObject, Color color, bool receivesClicks)
    {
        Image panelImage = uiObject.AddComponent<Image>();
        panelImage.color = color;
        panelImage.raycastTarget = receivesClicks;
        return panelImage;
    }

    // The top-left anchored labels belong to the status panel.
    private static TMP_Text CreateTextElement(
        string objectName, Transform parent, TMP_FontAsset font,
        string initialText, float fontSize, FontStyles style,
        TextAlignmentOptions alignment, Vector2 topLeftOffset, Vector2 size)
    {
        RectTransform textRectangle = CreateUiRectangle(
            objectName, parent,
            new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), topLeftOffset, size
        );
        TextMeshProUGUI textComponent = textRectangle.gameObject.AddComponent<TextMeshProUGUI>();
        textComponent.font = font;
        textComponent.text = initialText;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = style;
        textComponent.alignment = alignment;
        textComponent.color = Color.white;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    // Center-aligned TextMeshPro is useful in overlays and buttons.
    private static TMP_Text CreateCenteredTextElement(
        string objectName, Transform parent, TMP_FontAsset font,
        string initialText, float fontSize, Vector2 offset, Vector2 size)
    {
        RectTransform textRectangle = CreateUiRectangle(
            objectName, parent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), offset, size
        );
        TextMeshProUGUI textComponent = textRectangle.gameObject.AddComponent<TextMeshProUGUI>();
        textComponent.font = font;
        textComponent.text = initialText;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = FontStyles.Normal;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
        textComponent.raycastTarget = false;
        return textComponent;
    }

    // Physics setup shared by player and monitor, so it lives in one method.
    private static void ConfigureCharacterPhysics(Rigidbody2D characterBody)
    {
        characterBody.bodyType = RigidbodyType2D.Dynamic;
        characterBody.gravityScale = 0f; // Top-down, no falling.
        characterBody.freezeRotation = true;
        characterBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        characterBody.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    // Creates a simple visible object. "parent = null" is optional.
    private static GameObject CreateColoredShape(
        string objectName, Vector2 position, Vector2 size, Color shapeColor,
        Sprite shapeSprite, int sortingOrder, Transform parent = null)
    {
        GameObject newObject = new GameObject(objectName);

        if (parent != null)
        {
            newObject.transform.SetParent(parent);
        }

        newObject.transform.position = new Vector3(position.x, position.y, 0f);
        newObject.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer shapeRenderer = newObject.AddComponent<SpriteRenderer>();
        shapeRenderer.sprite = shapeSprite;
        shapeRenderer.color = shapeColor;
        shapeRenderer.sortingOrder = sortingOrder;
        return newObject;
    }

    // Every real wall has BOTH a solid 2D collider and the Walls layer.
    private static void CreateSolidWall(
        string wallName, float x, float y, float width, float height,
        int wallLayerNumber, Sprite wallSprite, Transform parent)
    {
        GameObject wallObject = CreateColoredShape(
            wallName, new Vector2(x, y), new Vector2(width, height),
            new Color(0.43f, 0.49f, 0.57f), wallSprite, 1, parent
        );

        wallObject.layer = wallLayerNumber;
        BoxCollider2D solidCollider = wallObject.AddComponent<BoxCollider2D>();
        solidCollider.isTrigger = false;
    }

    // Waypoints are positions only. They do NOT need Collider2D components.
    private static Transform CreateWaypoint(string pointName, float x, float y, Transform parent)
    {
        GameObject pointObject = new GameObject(pointName);
        pointObject.transform.SetParent(parent);
        pointObject.transform.position = new Vector3(x, y, 0f);
        return pointObject.transform;
    }

    // Imported PNGs must be Sprites, not ordinary textures, for SpriteRenderer.
    private static Sprite LoadPlaceholderSprite(string assetPath)
    {
        TextureImporter imageImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imageImporter == null)
        {
            return null;
        }

        bool needsSpriteType = imageImporter.textureType != TextureImporterType.Sprite;
        bool needsPixelScale = imageImporter.spritePixelsPerUnit != 64f;

        if (needsSpriteType == true || needsPixelScale == true)
        {
            imageImporter.textureType = TextureImporterType.Sprite;
            imageImporter.spriteImportMode = SpriteImportMode.Single;
            imageImporter.spritePixelsPerUnit = 64f;
            imageImporter.alphaIsTransparency = true;
            imageImporter.SaveAndReimport();
        }

        Sprite loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        return loadedSprite;
    }

    // Unity lets us assign named layers numbered 8 through 31.
    // Layers 0–7 are built-in layers, so we do not overwrite them.
    private static int FindOrCreateLayer(string requestedLayerName)
    {
        Object[] projectSettingsAssets = AssetDatabase.LoadAllAssetsAtPath(
            "ProjectSettings/TagManager.asset"
        );

        if (projectSettingsAssets == null || projectSettingsAssets.Length == 0)
        {
            return -1;
        }

        SerializedObject tagSettings = new SerializedObject(projectSettingsAssets[0]);
        SerializedProperty layers = tagSettings.FindProperty("layers");

        if (layers == null)
        {
            return -1;
        }

        // If Walls exists already, return its existing layer number.
        for (int layerNumber = 8; layerNumber < layers.arraySize; layerNumber = layerNumber + 1)
        {
            SerializedProperty layerSlot = layers.GetArrayElementAtIndex(layerNumber);
            if (layerSlot.stringValue == requestedLayerName)
            {
                return layerNumber;
            }
        }

        // Otherwise put the name into the first unused user layer slot.
        for (int layerNumber = 8; layerNumber < layers.arraySize; layerNumber = layerNumber + 1)
        {
            SerializedProperty layerSlot = layers.GetArrayElementAtIndex(layerNumber);

            if (string.IsNullOrEmpty(layerSlot.stringValue) == false)
            {
                continue;
            }

            layerSlot.stringValue = requestedLayerName;
            tagSettings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return layerNumber;
        }

        return -1; // Every user layer is already occupied.
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> buildScenes =
            new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        foreach (EditorBuildSettingsScene existingScene in buildScenes)
        {
            if (existingScene.path == scenePath)
            {
                return; // Already added.
            }
        }

        buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();
    }
}
#endif
