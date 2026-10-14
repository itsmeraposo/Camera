using UnityEngine;
using UnityEngine.AI;

public class Attack : MonoBehaviour
{
    [Header("Ataque")]
    public float attackRange = 2f;
    public float timeToAttack = 0.5f;
    public float damage = 10f;

    [Header("Colisão do ataque")]
    public Transform attackPoint;
    public float attackRadius = 1f;

    [Header("Player pequeno (rato): limites extras")]
    [Tooltip("O dano só vale se a superfície do player estiver a menos disso do inimigo (no chão, em metros)")]
    public float maxHitDistance = 0.8f;
    [Tooltip("O inimigo só começa o ataque se estiver olhando para o player dentro deste ângulo (graus, de cada lado)")]
    public float maxAttackAngle = 60f;
    [Tooltip("Só ataca se o EnemySensor estiver vendo o player")]
    public bool requireSight = true;

    private Transform player;
    private Collider playerCollider;
    private EnemySensor sensor;
    private NavMeshAgent agent;
    private Animator animator;

    private float timeInRange = 0f;
    private bool attacking = false;
    private bool checkingHit = false;
    private bool alreadyHit = false;

    public bool IsAttacking => attacking;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // Procura o Animator também nos filhos (o modelo do inimigo é um objeto filho)
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogWarning("[Attack] Não achei nenhum Animator nos filhos de " + name + " (o modelo está ativo?)");

        GameObject obj = GameObject.FindGameObjectWithTag("Player");

        if (obj != null)
        {
            player = obj.transform;

            // Collider sólido do player (ignora triggers), usado para medir a distância até a superfície
            foreach (Collider c in obj.GetComponentsInChildren<Collider>())
            {
                if (!c.isTrigger)
                {
                    playerCollider = c;
                    break;
                }
            }
        }

        EnemyBrain brain = GetComponentInParent<EnemyBrain>();
        sensor = brain != null ? brain.sensor : GetComponentInChildren<EnemySensor>();
    }

    // Distância no chão entre o inimigo e a SUPERFÍCIE do player (não entre os pivôs)
    float DistanceToPlayerSurface()
    {
        Vector3 point = player.position;

        if (playerCollider != null)
        {
            if (playerCollider is MeshCollider mc && !mc.convex)
                point = playerCollider.bounds.ClosestPoint(transform.position);
            else
                point = playerCollider.ClosestPoint(transform.position);
        }

        Vector3 d = point - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    // O inimigo precisa estar de frente para o player e (opcionalmente) vendo ele
    bool CanStartAttack()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude > 0.0001f)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;

            if (Vector3.Angle(forward, toPlayer) > maxAttackAngle)
                return false;
        }

        if (requireSight && sensor != null && !sensor.CanSeePlayer())
            return false;

        return true;
    }

    void Update()
    {
        if (player == null)
            return;

        float distance = DistanceToPlayerSurface();

        if (attacking)
        {
            if (checkingHit)
                CheckHit();

            return;
        }

        if (distance <= attackRange && CanStartAttack())
        {
            timeInRange += Time.deltaTime;

            if (timeInRange >= timeToAttack)
                StartAttack();
        }
        else
        {
            timeInRange = 0f;
        }
    }

    void StartAttack()
    {
        attacking = true;
        timeInRange = 0f;

        SetAgentStopped(true);

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        if (animator != null)
            animator.SetTrigger("Attack");
    }

    // Animation Event: frame em que o golpe começa a causar dano.
    // (Renomeado de Start para AttackStart, pois Start já é método da Unity.)
    public void AttackStart()
    {
        checkingHit = true;
        alreadyHit = false;
    }

    // Animation Event: fim da janela de dano.
    public void AttackEnd()
    {
        checkingHit = false;
        attacking = false;
        SetAgentStopped(false);
    }

    // Só mexe no NavMeshAgent se ele estiver ativo e em cima do NavMesh
    // (evita erro se o inimigo morreu ou está fora da área azul do NavMesh)
    void SetAgentStopped(bool stopped)
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = stopped;
    }

    void CheckHit()
    {
        if (alreadyHit || attackPoint == null)
            return;

        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRadius, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                // Limite rígido: o golpe só acerta se o player estiver realmente perto
                if (DistanceToPlayerSurface() > maxHitDistance)
                    continue;

                alreadyHit = true;

                hit.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

                Debug.Log("Player atingido!");
                break;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
