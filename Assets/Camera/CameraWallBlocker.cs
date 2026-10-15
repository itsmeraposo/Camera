using UnityEngine;

// Put this on the MAIN CAMERA (the one with the Cinemachine Brain).
// It runs AFTER Cinemachine: if a wall is between the player and the camera,
// it pulls the camera in front of the wall so it never goes inside it.
[DefaultExecutionOrder(1000)]
public class CameraWallBlocker : MonoBehaviour
{
    [Header("Target (the rat)")]
    [Tooltip("Leave empty to find the object with the Player tag")]
    public Transform target;
    [Tooltip("Height of the ray origin above the player's pivot (the pivot is at the feet)")]
    public float targetHeight = 0.5f;

    [Header("Collision")]
    [Tooltip("Check only the WALL and ground layers (usually Default). Do NOT check Player.")]
    public LayerMask obstacleLayers = 1; // 1 = Default
    [Tooltip("Gap between the camera and the wall (avoids seeing through wall edges)")]
    public float margin = 0.3f;
    [Tooltip("The camera never gets closer to the player than this (avoids seeing inside the body)")]
    public float minDistance = 0.7f;
    [Tooltip("Radius of the test sphere (0 = a simple line)")]
    public float radius = 0.15f;
    [Tooltip("The camera is pushed away from any wall closer than this (fixes walls that disappear beside the camera)")]
    public float sideClearance = 0.5f;
    [Tooltip("How many push-away passes per frame (more = better in corners)")]
    public int clearancePasses = 3;

    [Header("When the camera is pulled close by a wall")]
    [Tooltip("How much the camera rises when a wall pulls it all the way to the player (0 = off)")]
    public float liftWhenBlocked = 0.6f;
    [Tooltip("How much the camera tilts to look down at the player when pulled in (0 = off, 1 = full)")]
    [Range(0f, 1f)]
    public float lookDownBlend = 0.6f;

    [Header("Debug")]
    [Tooltip("Draws a line in the Scene view: from the player to where Cinemachine wants the camera. GREEN = free, RED = wall found, YELLOW sphere = where the wall was hit.")]
    public bool drawDebug = true;

    Vector3 dbgOrigin, dbgWanted, dbgHitPoint;
    bool dbgBlocked, dbgHasData;

    void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        if (target == null)
            Debug.LogWarning("[CameraWallBlocker] No target found: drag the Player into the Target field.");
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 origin = target.position + Vector3.up * targetHeight;
        Vector3 cameraPos = transform.position;
        Vector3 direction = cameraPos - origin;
        float distance = direction.magnitude;

        if (distance < 0.01f) return;

        direction /= distance;

        dbgOrigin = origin;
        dbgWanted = cameraPos;
        dbgHasData = true;

        RaycastHit hit;
        bool blocked = radius > 0.001f
            ? Physics.SphereCast(origin, radius, direction, out hit, distance, obstacleLayers, QueryTriggerInteraction.Ignore)
            : Physics.Raycast(origin, direction, out hit, distance, obstacleLayers, QueryTriggerInteraction.Ignore);

        // Extra plain raycast: SphereCast ignores walls that the sphere already starts touching
        if (!blocked)
            blocked = Physics.Raycast(origin, direction, out hit, distance, obstacleLayers, QueryTriggerInteraction.Ignore);

        dbgBlocked = blocked;
        if (blocked) dbgHitPoint = hit.point;

        if (blocked)
        {
            float newDistance = Mathf.Max(hit.distance - margin, minDistance);
            transform.position = origin + direction * newDistance;

            // How much the wall pulled the camera in (0 = not at all, 1 = all the way to the player)
            float pulled = Mathf.Clamp01(1f - newDistance / distance);

            if (liftWhenBlocked > 0f)
                transform.position += Vector3.up * (liftWhenBlocked * pulled);

            if (lookDownBlend > 0f)
            {
                Quaternion lookAtPlayer = Quaternion.LookRotation(origin - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookAtPlayer, lookDownBlend * pulled);
            }
        }

        PushAwayFromWalls(origin);
    }

    // Keeps the camera at least 'sideClearance' away from any wall, so the near clip plane
    // never cuts through a wall that is beside the camera.
    void PushAwayFromWalls(Vector3 targetPoint)
    {
        if (sideClearance <= 0f) return;

        for (int pass = 0; pass < Mathf.Max(1, clearancePasses); pass++)
        {
            Vector3 pos = transform.position;
            Collider[] walls = Physics.OverlapSphere(pos, sideClearance, obstacleLayers, QueryTriggerInteraction.Ignore);

            if (walls.Length == 0) return;

            foreach (Collider wall in walls)
            {
                // ClosestPoint does not work on non-convex MeshColliders: skip those
                if (wall is MeshCollider mc && !mc.convex) continue;

                Vector3 closest = wall.ClosestPoint(pos);
                Vector3 away = pos - closest;
                float dist = away.magnitude;

                if (dist > 0.0001f)
                {
                    // Camera is outside the wall but too close: push straight away from the surface
                    pos += away / dist * (sideClearance - dist);
                }
                else
                {
                    // Camera is INSIDE the wall: leave through the nearest face, on the player's side
                    pos += ExitVector(wall, pos, targetPoint);
                }
            }

            transform.position = pos;
        }
    }

    // Vector that moves a point that is inside 'wall' out of it, on the same side as 'targetPoint'
    Vector3 ExitVector(Collider wall, Vector3 pos, Vector3 targetPoint)
    {
        BoxCollider box = wall as BoxCollider;

        if (box != null)
        {
            Transform t = box.transform;
            Vector3 center = t.TransformPoint(box.center);
            Vector3 scale = t.lossyScale;
            Vector3 half = new Vector3(
                Mathf.Abs(box.size.x * scale.x),
                Mathf.Abs(box.size.y * scale.y),
                Mathf.Abs(box.size.z * scale.z)) * 0.5f;

            Vector3[] axes = { t.right, t.up, t.forward };
            float[] halves = { half.x, half.y, half.z };

            float bestPenetration = float.MaxValue;
            Vector3 bestDirection = Vector3.zero;

            for (int i = 0; i < 3; i++)
            {
                // Only leave through the side faces (never push the camera up over the top of the wall)
                if (Mathf.Abs(axes[i].y) > 0.5f) continue;

                float offset = Vector3.Dot(pos - center, axes[i]);
                float penetration = halves[i] - Mathf.Abs(offset);

                if (penetration < bestPenetration)
                {
                    bestPenetration = penetration;

                    // Choose the face on the same side as the player
                    float side = Vector3.Dot(targetPoint - center, axes[i]);
                    bestDirection = axes[i] * (side >= 0f ? 1f : -1f);
                }
            }

            if (bestDirection != Vector3.zero)
            {
                // Distance to the chosen face (from the camera) + clearance
                float offsetOnAxis = Vector3.Dot(pos - center, bestDirection);
                float axisHalf = 0f;
                for (int i = 0; i < 3; i++)
                    if (Mathf.Abs(Vector3.Dot(axes[i], bestDirection)) > 0.99f) axisHalf = halves[i];

                return bestDirection * (axisHalf - offsetOnAxis + sideClearance);
            }
        }

        // Fallback for other collider types: push away from the collider center, horizontally
        Vector3 fallback = pos - wall.bounds.center;
        fallback.y = 0f;
        if (fallback.sqrMagnitude < 0.0001f) fallback = -transform.forward;
        return fallback.normalized * sideClearance;
    }

    void OnDrawGizmos()
    {
        if (!drawDebug || !dbgHasData) return;

        Gizmos.color = dbgBlocked ? Color.red : Color.green;
        Gizmos.DrawLine(dbgOrigin, dbgWanted);
        Gizmos.DrawWireSphere(dbgWanted, 0.1f);

        if (dbgBlocked)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(dbgHitPoint, 0.15f);
        }
    }
}
