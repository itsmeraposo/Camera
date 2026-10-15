using UnityEngine;

// Put this on the MAIN CAMERA (the one with the Cinemachine Brain).
// Runs AFTER Cinemachine. It changes only ONE thing: how far the camera is from the player.
//  - A wall in the way pulls the camera in INSTANTLY (so you never see the other side).
//  - When the wall is gone, the camera eases back out SLOWLY (so it never jitters).
// It never rotates the camera and never pushes it sideways.
[DefaultExecutionOrder(1000)]
public class CameraWallBlocker : MonoBehaviour
{
    [Header("Target (the rat)")]
    [Tooltip("Leave empty to find the object with the Player tag")]
    public Transform target;
    [Tooltip("Height of the ray origin above the player's pivot (the pivot is at the feet)")]
    public float targetHeight = 0.5f;

    [Header("Collision")]
    [Tooltip("Check only the WALL layer (Paredes). Do NOT check Player or Enemy.")]
    public LayerMask obstacleLayers = 1;
    [Tooltip("Rays are cast around the camera at this distance, so walls beside the camera are detected too")]
    public float radius = 0.35f;
    [Tooltip("Gap between the camera and the wall")]
    public float margin = 0.15f;
    [Tooltip("The camera never gets closer to the player than this")]
    public float minDistance = 0.4f;

    [Header("Feel")]
    [Tooltip("How fast the camera eases back out after a wall is gone (lower = smoother)")]
    public float releaseSpeed = 4f;
    [Tooltip("How much the camera rises when a wall pulls it all the way in (0 = off). Position only, never rotates.")]
    public float liftWhenBlocked = 0.3f;

    [Header("Debug")]
    public bool drawDebug = false;

    float smoothedReduction;
    Vector3 dbgOrigin, dbgFinal;
    bool dbgHasData;

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
        Vector3 direction = transform.position - origin;
        float distance = direction.magnitude;
        if (distance < 0.01f) return;
        direction /= distance;

        // How far the camera may go: closest hit of 5 rays (center + 4 around the camera)
        float allowed = CastRay(origin, direction, distance);

        if (radius > 0.001f)
        {
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            Vector3 up = Vector3.Cross(direction, right).normalized;

            allowed = Mathf.Min(allowed, CastRay(origin + right * radius, direction, distance));
            allowed = Mathf.Min(allowed, CastRay(origin - right * radius, direction, distance));
            allowed = Mathf.Min(allowed, CastRay(origin + up * radius, direction, distance));
            allowed = Mathf.Min(allowed, CastRay(origin - up * radius, direction, distance));
        }

        float newDistance = Mathf.Clamp(allowed - margin, Mathf.Min(minDistance, distance), distance);
        float reduction = distance - newDistance;   // how much the wall wants to pull in

        // Going in: instant. Going out: slow.
        float eased = Mathf.Lerp(smoothedReduction, reduction, 1f - Mathf.Exp(-releaseSpeed * Time.deltaTime));
        smoothedReduction = Mathf.Max(reduction, eased);

        float finalDistance = distance - smoothedReduction;
        float pulled = Mathf.Clamp01(smoothedReduction / distance);

        Vector3 pos = origin + direction * finalDistance;
        if (liftWhenBlocked > 0f) pos += Vector3.up * (liftWhenBlocked * pulled);
        transform.position = pos;

        dbgOrigin = origin;
        dbgFinal = pos;
        dbgHasData = true;
    }

    // Distance the ray travels before hitting a wall (or 'maxDistance' if free)
    float CastRay(Vector3 start, Vector3 direction, float maxDistance)
    {
        RaycastHit hit;
        if (Physics.Raycast(start, direction, out hit, maxDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
            return hit.distance;
        return maxDistance;
    }

    void OnDrawGizmos()
    {
        if (!drawDebug || !dbgHasData) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(dbgOrigin, dbgFinal);
        Gizmos.DrawWireSphere(dbgFinal, 0.1f);
    }
}
