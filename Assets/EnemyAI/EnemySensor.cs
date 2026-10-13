using UnityEngine;

public class EnemySensor : MonoBehaviour
{
    [Header("Player")]
    public Transform player;
    [Tooltip("Altura do alvo (peito do player) para o raio de visão")]
    public float targetHeight = 1f;

    [Header("Vision")]
    public Transform eyePoint;
    public float viewDistance = 12f;
    [Range(0, 360)]
    public float viewAngle = 90f;

    [Header("Optional SphereCast")]
    public float sphereRadius = 0.35f;

    [Header("Layers")]
    [Tooltip("Somente paredes/cenário. NÃO inclua a layer do Player.")]
    public LayerMask obstacleMask;

    public Vector3 LastKnownPosition { get; private set; }

    Vector3 Origin()
    {
        return eyePoint != null ? eyePoint.position : transform.position + Vector3.up;
    }

    public bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 origin = Origin();
        Vector3 target = player.position + Vector3.up * targetHeight;
        Vector3 direction = target - origin;

        if (direction.magnitude > viewDistance)
            return false;

        float angle = Vector3.Angle(transform.forward, direction.normalized);

        if (angle > viewAngle / 2f)
            return false;

        // Se existe obstáculo entre o inimigo e o jogador, não vê.
        if (Physics.Linecast(origin, target, obstacleMask))
            return false;

        LastKnownPosition = player.position;
        return true;
    }

    public bool SphereDetectPlayer()
    {
        if (player == null)
            return false;

        if (Physics.SphereCast(Origin(), sphereRadius, transform.forward, out RaycastHit hit, viewDistance))
        {
            if (hit.transform.CompareTag("Player"))
            {
                LastKnownPosition = player.position;
                return true;
            }
        }

        return false;
    }

    public bool RaycastDetectPlayer()
    {
        if (player == null)
            return false;

        if (Physics.Raycast(Origin(), transform.forward, out RaycastHit hit, viewDistance))
        {
            if (hit.transform.CompareTag("Player"))
            {
                LastKnownPosition = player.position;
                return true;
            }
        }

        return false;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origin = Origin();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, viewDistance);

        Vector3 left = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, left * viewDistance);
        Gizmos.DrawRay(origin, right * viewDistance);
    }
}
