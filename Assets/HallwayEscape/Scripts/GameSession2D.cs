using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/*
 * GAME SESSION — BEGINNER / INTERMEDIATE READING
 *
 * PURPOSE:
 *   1. Remember whether the player has won or lost.
 *   2. Tell the Canvas HUD when the game ends or a message appears.
 *   3. Restart the current scene when R (or the on-screen button) is pressed.
 *
 * IMPORTANT SEPARATION:
 *   GameSession2D stores the GAME'S STATE.
 *   GameHUD2D controls the VISIBLE USER INTERFACE (the Unity Canvas).
 *
 * We do not use OnGUI() anywhere in this version.
 */
public class GameSession2D : MonoBehaviour
{
    // A static property lets pickups and the door find the active game session.
    // The private setter prevents other scripts from replacing it accidentally.
    public static GameSession2D Instance { get; private set; }

    [Header("References assigned by the demo scene builder")]
    public PlayerMovement2D Player;
    public GameHUD2D Hud;

    // Only this class may change the game's finished state.
    public bool IsFinished { get; private set; }

    private void Awake()
    {
        // Unity calls Awake before the first Update.
        // Store a shared reference to this session for other scripts.
        Instance = this;
        IsFinished = false;
    }

    private void Start()
    {
        // Other objects may call this method once the scene is ready.
        if (Hud != null)
        {
            Hud.ShowMessage("Find the key, then reach the exit.", 4f);
        }
        else
        {
            Debug.LogWarning("GameSession2D needs a GameHUD2D Canvas reference.");
        }
    }

    private void Update()
    {
        // Ignore R until the player wins or loses.
        if (IsFinished == false)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // wasPressedThisFrame happens once when the key is pressed.
        // isPressed would continue being true while the key is held.
        if (keyboard.rKey.wasPressedThisFrame == true)
        {
            RestartLevel();
        }
    }

    // Other gameplay scripts can show feedback without editing UI text.
    // "= 3f" provides a default duration when the caller omits it.
    public void ShowMessage(string newMessage, float displaySeconds = 3f)
    {
        if (IsFinished == true)
        {
            return; // Never put a normal pickup message over the win/lose screen.
        }

        if (Hud != null)
        {
            Hud.ShowMessage(newMessage, displaySeconds);
        }
    }

    public void Win()
    {
        if (IsFinished == true)
        {
            return; // Prevent the result from changing twice.
        }

        IsFinished = true;

        if (Hud != null)
        {
            Hud.ShowResult(true); // true means the player escaped.
        }
    }

    public void Lose()
    {
        if (IsFinished == true)
        {
            return;
        }

        IsFinished = true;

        if (Hud != null)
        {
            Hud.ShowResult(false); // false means the monitor caught the player.
        }
    }

    // PUBLIC because the Canvas Restart Button calls this method.
    // The same method also handles pressing the R key after the game ends.
    public void RestartLevel()
    {
        if (IsFinished == false)
        {
            return; // The starter game only allows restarting after finishing.
        }

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);

        /*
         * ALTERNATIVE TO THE TWO LINES ABOVE:
         * SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
         *
         * Both reload the current scene; the longer version introduces
         * useful ideas: a local variable and accessing a scene's name.
         * The scene must be enabled in the project's scene/build list.
         */
    }

    private void OnDestroy()
    {
        // When the scene reloads, clear the old static reference.
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /*
     * BOOLEAN SYNTAX COMPARISON — these MEAN THE SAME THING for a bool:
     *
     * if (IsFinished == false) { ... }  // Beginner-friendly, explicit.
     * if (IsFinished != true)  { ... }  // Not equal to true.
     * if (!IsFinished)         { ... }  // Logical NOT (common shorthand).
     *
     * "false" and "true" must be lowercase in C#.
     * DO NOT uncomment all three: they are alternative EXAMPLES.
     */
}
