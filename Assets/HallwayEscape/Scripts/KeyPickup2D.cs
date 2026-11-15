using UnityEngine;

/*
 * KEY PICKUP — BEGINNER LESSON
 *
 * Attach this to an object with a Collider2D set to Is Trigger.
 * When the player overlaps the object:
 *   1. Tell the player's script that the key was collected.
 *   2. Show a message.
 *   3. Remove the key from the scene.
 */
[RequireComponent(typeof(Collider2D))]
public class KeyPickup2D : MonoBehaviour
{
    // Unity automatically calls this when a suitable 2D trigger is entered.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // "other" is the Collider2D that entered our trigger.
        PlayerMovement2D player = other.GetComponent<PlayerMovement2D>();
        //this checks if the object with a collider has the PlayerMovement2D script attached to it. If it does, it will return a reference to that script; if not, it will return null.
        //Another way you could check for player is by using tags. You could assign a "Player" tag to your player object and then check if the other collider has that tag. For example:

        // Not everything entering the trigger is necessarily the player.
        // null means GetComponent did not find PlayerMovement2D.
        if (player == null)
        {
            return;
        }

        player.CollectKey();

        // We check for the GameSession because a missing reference should
        // not stop the basic key pickup from working.
        if (GameSession2D.Instance != null)
        {
            GameSession2D.Instance.ShowMessage("Key collected! Find the exit.");
        }

        Destroy(gameObject); // Remove THIS key object, not the player.

        /*
         * ALTERNATIVE SYNTAX (same condition, not three separate checks):
         *
         * if (player == null) { return; }  // Current clear version.
         * if (player != null) { ... }       // Reverse the check and put
         *                                  // successful actions inside it.
         *
         * "==" means equals; "!=" means does not equal.
         * null means "no object reference"; it is not the same as false.
         */
    }
}
