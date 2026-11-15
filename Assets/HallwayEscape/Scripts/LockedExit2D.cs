using UnityEngine;

/*
 * LOCKED EXIT — BEGINNER LESSON
 *
 * This object has TWO different Collider2D components:
 *   - Solid BoxCollider2D: physically blocks the doorway when locked.
 *   - Larger trigger CircleCollider2D: notices when the player arrives.
 *
 * The key is stored on the PLAYER as HasKey. We do not store a second copy.
 */
public class LockedExit2D : MonoBehaviour
{
    [Header("References set up in the Inspector")]
    public PlayerMovement2D Player;
    public Collider2D SolidBarrier;
    public SpriteRenderer DoorVisual;

    [Header("Door Colors")]
    public Color LockedColor = new Color(0.9f, 0.2f, 0.2f);
    public Color UnlockedColor = new Color(0.2f, 0.85f, 0.35f);

    private bool isUnlocked = false;

    private void Start()
    {
        if (DoorVisual != null)
        {
            DoorVisual.color = LockedColor;
        }
    }

    private void Update()
    {
        // We intentionally separate the checks instead of combining all
        // conditions into one long "if" statement.
        if (isUnlocked == true)
        {
            return; // Already unlocked; there is nothing to do.
        }

        if (Player == null)
        {
            return; // No player assigned in the Inspector.
        }

        if (Player.HasKey == false)
        {
            return; // The player still needs to find the key.
        }

        // We reach this part only when the player HAS the key.
        isUnlocked = true;

        if (SolidBarrier != null)
        {
            SolidBarrier.enabled = false; // Door no longer blocks movement.
        }

        if (DoorVisual != null)
        {
            DoorVisual.color = UnlockedColor; // Give a visible confirmation.
        }

        if (GameSession2D.Instance != null)
        {
            GameSession2D.Instance.ShowMessage("Exit unlocked! Reach the green doorway.");
        }

        /*
         * BOOLEAN ALTERNATIVES (choose ONE, not all three):
         *
         * if (Player.HasKey == false)  // Explicit: HasKey equals false.
         * if (Player.HasKey != true)   // Explicit: HasKey is not true.
         * if (!Player.HasKey)          // Common short version: NOT HasKey.
         *
         * All three above are equivalent for a normal C# bool.
         * C# uses lowercase "true" and "false". "True" / "False"
         * with capital letters are not valid C# Boolean keywords.
         *
         * if (Player.HasKey == true) and if (Player.HasKey)
         * also mean the same thing.
         */
    }

    // When the player first approaches the doorway...
    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckWhetherPlayerCanExit(other);
    }

    // When a player waits in the trigger while the door changes to unlocked...
    private void OnTriggerStay2D(Collider2D other)
    {
        if (isUnlocked == true)
        {
            CheckWhetherPlayerCanExit(other);
        }
    }

    // One helper method avoids repeating the same logic in both trigger events.
    private void CheckWhetherPlayerCanExit(Collider2D other)
    {
        PlayerMovement2D enteringPlayer = other.GetComponent<PlayerMovement2D>();

        if (enteringPlayer == null)
        {
            return;
        }

        if (GameSession2D.Instance == null)
        {
            return;
        }

        if (isUnlocked == true)
        {
            GameSession2D.Instance.Win();
        }
        else
        {
            GameSession2D.Instance.ShowMessage("The exit is locked. Find the key!", 1f);
        }

        /*
         * ALTERNATIVE WAY TO WRITE THE LAST DECISION:
         *
         * if (!isUnlocked)
         * {
         *     // Show the locked message here.
         * }
         * else
         * {
         *     // Win the game here.
         * }
         *
         * The logic is the same, but the branches are in reverse order.
         */
    }
}
