using UnityEngine;
using UnityEngine.InputSystem;

/*
 * PLAYER MOVEMENT — BEGINNER LESSON
 *
 * PURPOSE:
 *   1. Read WASD and arrow keys.
 *   2. Move a 2D player without going through solid walls.
 *   3. Remember whether the player collected a key.
 *   4. Temporarily increase movement speed after a drink pickup.
 *
 * SCRATCH CONNECTION:
 *   "if key pressed"  ->  if (keyboard.wKey.isPressed)
 *   "set variable"   ->  HasKey = true;
 *
 * IMPORTANT:
 *   This version uses Unity's NEW Input System, but does not require
 *   Input Actions or a PlayerInput component. See the setup guide.
 */

// RequireComponent helps prevent accidentally deleting required components.
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Player Movement")]
    [Min(0f)]
    public float MoveSpeed = 4f;  // Editable in the Unity Inspector.

    // A bool (Boolean) can be true or false. We start with no key.
    // { get; private set; } lets other scripts READ HasKey, but only
    // this script can CHANGE HasKey. See CollectKey() below.
    public bool HasKey { get; private set; }

    // Reserved for a possible future locker/hiding lesson.
    public bool IsHidden { get; private set; }

    // Other scripts can read the time left on the drink power-up.
    public float BoostRemaining
    {
        get
        {
            float secondsLeft = boostEndTime - Time.time;
            //Time.time is the number of seconds since the scene started.
            //boostEndTime is the time when the boost will end. Subtracting Time.time from boostEndTime gives the remaining time for the boost.
            return Mathf.Max(0f, secondsLeft);
        }
    }

    private Rigidbody2D playerRigidbody;
    private Vector2 movementDirection;
    private float speedBoostMultiplier = 1f;
    private float boostEndTime = 0f;

    // Awake runs once when Unity initializes this component.
    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();
        // GetComponent finds a component on the same GameObject.
        movementDirection = Vector2.zero;
    }

    // Update runs once per rendered frame. It is a good place to read input.
    private void Update()
    {
        // Stop the player when hiding or when a win/loss ends the game.
        if (IsHidden == true)
        {
            movementDirection = Vector2.zero;
            return; // Leave Update immediately; do not read input.
        }

        if (GameSession2D.Instance != null)
        {
            if (GameSession2D.Instance.IsFinished == true)
            {
                movementDirection = Vector2.zero;
                return;
            }
        }

        // Keyboard.current is the keyboard detected by the Input System.
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            movementDirection = Vector2.zero;
            return;
        }

        float horizontalInput = 0f;
        float verticalInput = 0f;

        // Both A and the left arrow mean LEFT (negative X).
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontalInput = horizontalInput - 1f;
        }

        // Both D and the right arrow mean RIGHT (positive X).
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontalInput = horizontalInput + 1f;
        }

        // Both S and the down arrow mean DOWN (negative Y).
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            verticalInput = verticalInput - 1f;
        }

        // Both W and the up arrow mean UP (positive Y).
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            verticalInput = verticalInput + 1f;
        }

        // Vector2 stores a direction using two numbers: X and Y.
        movementDirection = new Vector2(horizontalInput, verticalInput);

        // Diagonal input (1, 1) has a length greater than 1. Normalizing
        // means diagonal movement will not be faster than straight movement.
        if (movementDirection.sqrMagnitude > 1f)
        {
            movementDirection = movementDirection.normalized;
        }

        /*
         * ALTERNATIVE SYNTAX EXAMPLES (do not enable all at once):
         *
         * horizontalInput = horizontalInput - 1f;  // Long form, used above.
         * horizontalInput -= 1f;                    // Shorthand for the same thing.
         * horizontalInput--;                        // Subtracts exactly 1.
         *
         * if (keyboard.wKey.isPressed == true)      // Explicit comparison.
         * if (keyboard.wKey.isPressed)              // Shorter equivalent.
         *
         * || means OR: either key being pressed is enough.
         * && means AND: BOTH conditions must be true.
         */
    }

    // FixedUpdate runs at Unity's physics interval, not every visual frame.
    // MovePosition asks the physics engine to move the Rigidbody2D.
    private void FixedUpdate()
    {
        if (IsHidden == true)
        {
            return;
        }

        if (GameSession2D.Instance != null && GameSession2D.Instance.IsFinished == true)
        {
            playerRigidbody.linearVelocity = 0f * Vector2.one; // Stop moving.
            return;
        }

        float currentSpeed = MoveSpeed;

        // Time.time is the number of seconds since the scene started.
        // If the drink's timer has not expired, multiply the player's normal speed.
        if (Time.time < boostEndTime)
        {
            currentSpeed = currentSpeed * speedBoostMultiplier;
        }

        // Calculate the player's velocity.
        // Direction determines where to go.
        // Speed determines how fast to go.
        Vector2 desiredVelocity =
            movementDirection * currentSpeed;

        // Tell Unity's physics system how fast to move.
        playerRigidbody.linearVelocity = desiredVelocity;

        /*
         * ALTERNATIVE SYNTAX:
         * currentSpeed *= speedBoostMultiplier;
         * is identical to:
         * currentSpeed = currentSpeed * speedBoostMultiplier;
         *
         * The long form is deliberately used above for teaching.
         */
    }

    // Another component (such as a future locker) may call this method.
    public void SetHidden(bool shouldHide)
    {
        IsHidden = shouldHide;

        if (shouldHide == true)
        {
            //movementDirection = Vector2.zero;
            playerRigidbody.linearVelocity = Vector2.zero;
        }
    }

    // Called by KeyPickup2D when the player touches the key.
    public void CollectKey()
    {
        HasKey = true;
    }

    // Called by SpeedDrink2D when the player touches a drink.
    public void GiveSpeedBoost(float newMultiplier, float durationSeconds)
    {
        // These limits prevent zero, negative, or weakening multipliers.
        speedBoostMultiplier = Mathf.Max(1f, newMultiplier);
        float safeDuration = Mathf.Max(0f, durationSeconds);
        boostEndTime = Time.time + safeDuration;
    }
}
