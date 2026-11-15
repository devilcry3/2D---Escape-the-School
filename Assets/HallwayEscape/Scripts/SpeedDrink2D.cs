using UnityEngine;

/*
 * SPEED DRINK — BEGINNER / INTERMEDIATE LESSON
 *
 * A pickup that changes a number for a limited amount of time.
 * Like the key, the drink's Collider2D MUST have Is Trigger checked.
 */
[RequireComponent(typeof(Collider2D))]
public class SpeedDrink2D : MonoBehaviour
{
    [Header("Drink Power-Up")]
    [Min(1f)]
    public float SpeedMultiplier = 1.6f; // 1.6 means 160% of normal speed.

    [Min(0f)]
    public float DurationSeconds = 5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement2D player = other.GetComponent<PlayerMovement2D>();

        if (player == null)
        {
            return;
        }

        // Pass two values into the method in PlayerMovement2D.
        player.GiveSpeedBoost(SpeedMultiplier, DurationSeconds);

        if (GameSession2D.Instance != null)
        {
            GameSession2D.Instance.ShowMessage("Speed boost! Run!");
        }

        Destroy(gameObject);

        /*
         * ALTERNATIVE LESSON IDEA:
         * A power-up could change an integer score instead of movement speed:
         * score = score + 1;    // Clear beginner version.
         * score += 1;           // Shorthand version.
         * score++;              // Another shorthand for adding exactly 1.
         *
         * These are examples only: this game does not have a score yet.
         */
    }
}
