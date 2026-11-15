using System.Collections.Generic;
using UnityEngine;

/*
 * HALL MONITOR — INTERMEDIATE/INSTRUCTOR LESSON
 *
 * The hall monitor has two simple BEHAVIOR STATES:
 *   PATROLLING: walk between waypoint objects in order.
 *   CHASING:    follow the player while the player is visible/nearby.
 *
 * The Pathfinder handles the difficult job of going AROUND walls.
 * Students can change speeds, detection ranges, or waypoint locations
 * without reading the pathfinding algorithm.
 *
 * "Can see" currently means within a DISTANCE and with NO WALL in the
 * line of sight. Vision is 360 degrees; there is no front-facing cone.
 */
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class HallMonitorAI2D : MonoBehaviour
{
    [Header("References: assign objects in the Inspector")]
    public PlayerMovement2D Player;
    public GridPathfinder2D Pathfinder;
    public Transform[] PatrolPoints;
    public LayerMask WallLayers;

    [Header("Hall Monitor Behavior")]
    [Min(0f)]
    public float PatrolSpeed = 2.1f;

    [Min(0f)]
    public float ChaseSpeed = 3.3f;

    [Min(0.1f)]
    public float DetectionRange = 4.5f;

    [Min(0.1f)]
    public float LoseRange = 7f;

    [Min(0f)]
    public float LostSightSeconds = 1.25f;

    [Min(0.1f)]
    public float RepathSeconds = 0.35f;

    private Rigidbody2D monitorRigidbody;

    // The route is a list of X/Y world positions to walk toward.
    private readonly List<Vector2> currentRoute = new List<Vector2>();
    private int nextRoutePositionIndex = 0;
    private int currentPatrolIndex = 0;
    private bool isChasing = false;
    private float secondsWithoutSight = 0f;
    private float nextPathCalculationTime = 0f;

    private void Awake()
    {
        monitorRigidbody = GetComponent<Rigidbody2D>();
    }

    // Update decides WHAT the monitor wants to do, not how it moves.
    private void Update()
    {
        if (IsGameOver() == true)
        {
            return;
        }

        if (Player == null)
        {
            return;
        }

        // Distance checks are faster than raycasts, so we check distance first.
        // Vector2.Distance returns the straight-line distance between two points.
        // We use the monitor's Rigidbody position instead of transform.position
        float distanceToPlayer = Vector2.Distance(
            monitorRigidbody.position,
            Player.transform.position
        );

        bool playerIsInDetectionRange = distanceToPlayer <= DetectionRange; // "Can see" is a distance check, not a vision cone.
        // when it does the math it returns a true or false value if the player is in detection range or not
        bool playerIsOutsideLoseRange = distanceToPlayer > LoseRange;
        bool playerIsHidden = Player.IsHidden;

        // Linecast checks whether a WALL collider stands between monitor
        // and player. WallLayers is a layer MASK, not a single layer number.
        RaycastHit2D wallHit = Physics2D.Linecast(
            monitorRigidbody.position,
            Player.transform.position,
            WallLayers
        );

        bool wallBlocksSight = wallHit.collider != null;
        bool canSeePlayer = playerIsInDetectionRange
                            && playerIsHidden == false
                            && wallBlocksSight == false;

        // Change from PATROLLING to CHASING when we first see the player.
        if (isChasing == false && canSeePlayer == true)
        {
            BeginChasing();
        }
        else
        {
            // Already chasing: decide when to give up the chase.
            if (isChasing == true)
            {
                if (playerIsOutsideLoseRange == true || playerIsHidden == true)
                {
                    ReturnToPatrol();
                }
                else if (canSeePlayer == true)
                {
                    secondsWithoutSight = 0f;
                }
                else
                {
                    secondsWithoutSight = secondsWithoutSight + Time.deltaTime;

                    if (secondsWithoutSight >= LostSightSeconds)
                    {
                        ReturnToPatrol();
                    }
                }
            }
        }

        // While patrolling, advance to the next destination when close.
        if (isChasing == false && PatrolPoints != null && PatrolPoints.Length > 0)
        {
            if (currentPatrolIndex >= PatrolPoints.Length)
            {
                currentPatrolIndex = 0;
            }

            Transform patrolDestination = PatrolPoints[currentPatrolIndex];

            if (patrolDestination != null)
            {
                float distanceToWaypoint = Vector2.Distance(
                    monitorRigidbody.position,
                    patrolDestination.position
                );

                if (distanceToWaypoint < 0.3f)
                {
                    currentPatrolIndex = currentPatrolIndex + 1;

                    if (currentPatrolIndex >= PatrolPoints.Length)
                    {
                        currentPatrolIndex = 0; // Loop back to first waypoint.
                    }

                    nextPathCalculationTime = 0f;
                }
            }
        }

        /*
         * BOOLEAN ALTERNATIVES FOR A NORMAL C# bool:
         * if (isChasing == false)  // Explicit beginner wording.
         * if (isChasing != true)   // Same: "not equal to true".
         * if (!isChasing)          // Same: logical NOT.
         *
         * These compare the VALUE. "!" before a bool flips true/false.
         *
         * ALTERNATIVE FOR CYCLING WAYPOINT INDEX:
         * currentPatrolIndex = (currentPatrolIndex + 1) % PatrolPoints.Length;
         *
         * The % operator returns a remainder, causing the index to wrap
         * around. We use the longer if version above for readability.
         */
    }

    // FixedUpdate decides HOW to move using the 2D physics engine.
    private void FixedUpdate()
    {
        if (IsGameOver() == true)
        {
            return;
        }

        if (Pathfinder == null || Player == null)
        {
            return;
        }

        // A patrol requires at least one valid waypoint.
        if (isChasing == false)
        {
            if (PatrolPoints == null || PatrolPoints.Length == 0)
            {
                return;
            }
        }

        if (Time.time >= nextPathCalculationTime)
        {
            Vector2 destination;

            if (isChasing == true)
            {
                destination = Player.transform.position;
            }
            else
            {
                Transform waypoint = PatrolPoints[currentPatrolIndex];
                if (waypoint == null)
                {
                    return;
                }

                destination = waypoint.position;
            }

            // Pathfinder returns false if the destination is unreachable.
            // It always clears currentRoute, even when no path exists.
            bool routeWasFound = Pathfinder.FindPath(
                monitorRigidbody.position, destination, currentRoute
            );

            nextRoutePositionIndex = 0;
            nextPathCalculationTime = Time.time + RepathSeconds;

            if (routeWasFound == false)
            {
                return; // Do not walk directly through a wall as a fallback.
            }
        }

        // If we are already close enough to a route point, skip past it.
        while (nextRoutePositionIndex < currentRoute.Count)
        {
            Vector2 nextRoutePoint = currentRoute[nextRoutePositionIndex];
            float distanceToNextPoint = Vector2.Distance(
                monitorRigidbody.position, nextRoutePoint
            );

            if (distanceToNextPoint >= 0.13f)
            {
                break;
            }

            nextRoutePositionIndex = nextRoutePositionIndex + 1;
        }

        if (nextRoutePositionIndex >= currentRoute.Count)
        {
            return; // Route complete, or there is no route right now.
        }

        float movementSpeed = PatrolSpeed;
        if (isChasing == true)
        {
            movementSpeed = ChaseSpeed;
        }

        Vector2 routeTarget = currentRoute[nextRoutePositionIndex];
        float distanceThisStep = movementSpeed * Time.fixedDeltaTime;
        Vector2 newPosition = Vector2.MoveTowards(
            monitorRigidbody.position, routeTarget, distanceThisStep
        );

        monitorRigidbody.MovePosition(newPosition);

        /*
         * ALTERNATIVE SHORT VERSION OF THE SPEED CHOICE:
         * float movementSpeed = isChasing ? ChaseSpeed : PatrolSpeed;
         *
         * Both versions choose ONE speed. We use if/else-style logic
         * because the ternary "? :" operator is not a first-day topic.
         */
    }

    // Contact with a visible player ends the game.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerMovement2D touchedPlayer = collision.collider.GetComponent<PlayerMovement2D>();

        if (touchedPlayer == null)
        {
            return;
        }

        if (touchedPlayer.IsHidden == true)
        {
            return;
        }

        if (GameSession2D.Instance != null)
        {
            GameSession2D.Instance.Lose();
        }
    }

    private void BeginChasing()
    {
        isChasing = true;
        secondsWithoutSight = 0f;
        nextPathCalculationTime = 0f; // Ask for a chase route immediately.
    }

    private void ReturnToPatrol()
    {
        isChasing = false;
        secondsWithoutSight = 0f;
        nextPathCalculationTime = 0f;
        currentRoute.Clear();

        if (PatrolPoints == null || PatrolPoints.Length == 0)
        {
            return;
        }

        // Return to the CLOSEST waypoint rather than teleporting the
        // monitor to its original starting position.
        float closestDistanceFound = float.PositiveInfinity;

        for (int waypointIndex = 0; waypointIndex < PatrolPoints.Length; waypointIndex = waypointIndex + 1)
        {
            Transform waypoint = PatrolPoints[waypointIndex];
            if (waypoint == null)
            {
                continue;
            }

            float waypointDistance = Vector2.Distance(
                monitorRigidbody.position, waypoint.position
            );

            if (waypointDistance < closestDistanceFound)
            {
                closestDistanceFound = waypointDistance;
                currentPatrolIndex = waypointIndex;
            }
        }
    }

    private bool IsGameOver()
    {
        if (GameSession2D.Instance == null)
        {
            return false;
        }

        return GameSession2D.Instance.IsFinished;
    }

    // Gizmos are visual helpers in Unity's SCENE view, not game artwork.
    // Select the hall monitor to see detection and losing-distance circles.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, DetectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, LoseRange);

        if (PatrolPoints == null || PatrolPoints.Length == 0)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        for (int waypointIndex = 0; waypointIndex < PatrolPoints.Length; waypointIndex = waypointIndex + 1)
        {
            Transform currentWaypoint = PatrolPoints[waypointIndex];
            int nextWaypointIndex = waypointIndex + 1;

            if (nextWaypointIndex >= PatrolPoints.Length)
            {
                nextWaypointIndex = 0;
            }

            Transform nextWaypoint = PatrolPoints[nextWaypointIndex];

            if (currentWaypoint == null || nextWaypoint == null)
            {
                continue;
            }

            Gizmos.DrawSphere(currentWaypoint.position, 0.13f);
            Gizmos.DrawLine(currentWaypoint.position, nextWaypoint.position);
        }
    }
}
