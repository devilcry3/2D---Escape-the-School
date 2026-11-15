using TMPro;
using UnityEngine;

/*
 * CANVAS HUD — BEGINNER / INTERMEDIATE READING
 *
 * HUD = Heads-Up Display (the status and instructions on the screen).
 *
 * This class changes the TextMeshPro labels and panels inside a Canvas.
 * It does NOT decide whether the player has won, lost, or picked up a key.
 * GameSession2D and PlayerMovement2D hold that information.
 *
 * Students may use the Unity Editor to change font sizes, move panels,
 * change colors, or edit headings without changing the C# code here.
 *
 * This script has no OnGUI() and does not create screen rectangles in code.
 */
public class GameHUD2D : MonoBehaviour
{
    [Header("Game state: drag GAME SESSION here")]
    public GameSession2D GameSession;

    [Header("Always-visible HUD labels (TextMeshPro)")]
    public TMP_Text KeyStatusText;
    public TMP_Text SpeedBoostText;

    [Header("Temporary feedback message near the bottom")]
    public GameObject MessagePanel;
    public TMP_Text MessageText;

    [Header("Win / lose overlay")]
    public GameObject ResultPanel;
    public TMP_Text ResultTitleText;
    public TMP_Text ResultInstructionsText;

    // A private variable stores the moment the message should disappear.
    private float messageEndTime = 0f;

    private void Awake()
    {
        // These panels start hidden; gameplay code shows them when needed.
        if (MessagePanel != null)
        {
            MessagePanel.SetActive(false);
        }

        if (ResultPanel != null)
        {
            ResultPanel.SetActive(false);
        }
    }

    // Update runs each frame and reads the player's CURRENT information.
    private void Update()
    {// Update the key and speed boost labels every frame. but calling the methods to
     // update the text is more efficient than doing it in GameSession2D's Update method.
        UpdateKeyText();
        UpdateSpeedBoostText();
        HideExpiredMessage();
    }

    private void UpdateKeyText()
    {
        if (KeyStatusText == null)
        {
            return;
        }

        if (GameSession == null || GameSession.Player == null)
        {
            KeyStatusText.text = "Key: No player assigned";
            return;
        }

        // We could write simply 'if (GameSession.Player.HasKey)', but the
        // explicit comparison helps beginners recognize a Boolean test.
        if (GameSession.Player.HasKey == true)
        {
            KeyStatusText.text = "Key: FOUND";
        }
        else
        {
            KeyStatusText.text = "Key: Not found";
        }

        /*
         * ALTERNATIVE, SHORTER WAY (do not enable both versions):
         * KeyStatusText.text = GameSession.Player.HasKey
         *     ? "Key: FOUND"
         *     : "Key: Not found";
         *
         * This is called the ternary (three-part) conditional operator.
         * The if/else above is easier for new C# students to trace.
         */
    }

    private void UpdateSpeedBoostText()
    {
        if (SpeedBoostText == null)
        {
            return;
        }

        if (GameSession == null || GameSession.Player == null)
        {
            SpeedBoostText.text = "Speed boost: Inactive";
            return;
        }

        float remainingSeconds = GameSession.Player.BoostRemaining;

        if (remainingSeconds > 0f)
        {
            // "0.0" displays one decimal place: 3.46 becomes 3.5.
            string formattedSeconds = remainingSeconds.ToString("0.0");
            SpeedBoostText.text = "Speed boost: " + formattedSeconds + "s";
        }
        else
        {
            SpeedBoostText.text = "Speed boost: Inactive";
        }

        /*
         * ALTERNATIVE STRING SYNTAX:
         * SpeedBoostText.text = $"Speed boost: {remainingSeconds:0.0}s";
         *
         * The $ sign enables string interpolation. We use joining with +
         * above because it is straightforward for early lessons.
         */
    }

    // Called by GameSession2D or other scripts through the session.
    public void ShowMessage(string message, float displaySeconds)
    {
        if (MessagePanel == null || MessageText == null)
        {
            return;
        }

        MessageText.text = message;
        messageEndTime = Time.time + displaySeconds;
        MessagePanel.SetActive(true);
    }

    private void HideExpiredMessage()
    {
        if (MessagePanel == null)
        {
            return;
        }

        if (MessagePanel.activeSelf == false)
        {
            return;
        }

        if (Time.time >= messageEndTime)
        {
            MessagePanel.SetActive(false);
        }
    }

    // Called once when the player escapes or is caught.
    // The full-screen result panel includes an interactive Restart Button.
    public void ShowResult(bool playerEscaped)
    {
        if (ResultPanel == null)
        {
            return;
        }

        if (playerEscaped == true)
        {
            if (ResultTitleText != null)
            {
                ResultTitleText.text = "YOU ESCAPED!";
            }
        }
        else
        {
            if (ResultTitleText != null)
            {
                ResultTitleText.text = "THE HALL MONITOR CAUGHT YOU!";
            }
        }

        if (ResultInstructionsText != null)
        {
            ResultInstructionsText.text = "Click Restart or press R to try again.";
        }

        // Hide any previous pickup message before showing the result.
        if (MessagePanel != null)
        {
            MessagePanel.SetActive(false);
        }

        ResultPanel.SetActive(true);
    }
}
