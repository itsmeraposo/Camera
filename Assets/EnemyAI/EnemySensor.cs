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

    [Header("Player pequeno (rato)")]
    [Tooltip("Dentro desta distância (no chão) o inimigo percebe o player mesmo fora do ângulo de visão, se não houver parede no meio")]
    public float closeSenseRadius = 1.5f;
    [Tooltip("Também lança um raio de baixo, para o rato baixinho não sumir atrás de objetos baixos")]
    public bool usarRaioBaixo = true;
    public float alturaRaioBaixo = 0.3f;

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

        // Ângulo e distância medidos no plano do chão (ignora a diferença de altura).
        // Antes, com o player baixo e perto, a direção apontava para baixo e ele
        // saía do cone de visão ("sumia" quando chegava perto).
        Vector3 flatDir = direction;
        flatDir.y = 0f;
        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;

        bool muitoPerto = flatDir.magnitude <= closeSenseRadius;

        if (!muitoPerto && flatDir.sqrMagnitude > 0.0001f && flatForward.sqrMagnitude > 0.0001f)
        {
            float angle = Vector3.Angle(flatForward, flatDir);

            if (angle > viewAngle / 2f)
                return false;
        }

        // Se existe obstáculo entre o inimigo e o jogador, não vê.
        bool bloqueadoAlto = Physics.Linecast(origin, target, obstacleMask);

        if (bloqueadoAlto)
        {
            bool bloqueadoBaixo = true;

            if (usarRaioBaixo)
            {
                Vector3 baixo = transform.position + Vector3.up * alturaRaioBaixo;
                bloqueadoBaixo = Physics.Linecast(baixo, target, obstacleMask);
            }

            if (bloqueadoBaixo)
                return false;
        }

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
