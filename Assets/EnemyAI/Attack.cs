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

    private Transform player;
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
        animator = GetComponent<Animator>();

        GameObject obj = GameObject.FindGameObjectWithTag("Player");

        if (obj != null)
            player = obj.transform;
    }

    void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (attacking)
        {
            if (checkingHit)
                CheckHit();

            return;
        }

        if (distance <= attackRange)
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

        agent.isStopped = true;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

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
        agent.isStopped = false;
    }

    void CheckHit()
    {
        if (alreadyHit || attackPoint == null)
            return;

        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
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
