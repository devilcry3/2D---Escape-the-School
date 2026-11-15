using UnityEngine;

/*
 * CAMERA FOLLOW — OPTIONAL INTERMEDIATE SCRIPT
 *
 * Attach to the Main Camera and assign the Player Transform to Target.
 * We keep the camera's Z position unchanged so it can see the 2D scene.
 */
public class CameraFollow2D : MonoBehaviour
{
    public Transform Target;

    [Min(0f)]
    public float FollowSharpness = 8f;

    // LateUpdate runs after Update. It is often useful for cameras because
    // we want to follow the player's new position, not the previous one.
    private void LateUpdate()
    {
        if (Target == null)
        {
            return;
        }

        Vector3 desiredPosition = new Vector3(
            Target.position.x,
            Target.position.y,
            transform.position.z
        );

        // Mathf.Exp + Lerp produce smooth movement independent of frame rate.
        // For a first lesson, this entire formula can remain prewritten.
        float smoothAmount = 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime);
        Vector3 newCameraPosition = Vector3.Lerp(transform.position, desiredPosition, smoothAmount);
        transform.position = newCameraPosition;

        /*
         * SIMPLE ALTERNATIVE (instant follow, without smoothing):
         * transform.position = desiredPosition;
         *
         * This is an excellent beginner experiment. Replace the three
         * smoothing lines above with the single line to compare behavior.
         */
    }
}
