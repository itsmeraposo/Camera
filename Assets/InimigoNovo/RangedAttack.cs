using System.Collections;
using UnityEngine;

// INIMIGO ATIRADOR: configuração e disparo do projétil.
// Vai no objeto PAI do inimigo (junto com EnemyBrain, EnemyMovement, EnemySensor...).
// No EnemyBrain, escolha Type = Ranged.
// NÃO coloque o Attack.cs (corpo a corpo) neste inimigo.
//
// Animator (opcional): Trigger "Shoot". Se não existir, o tiro sai mesmo assim.
public class RangedAttack : MonoBehaviour
{
    [Header("Projétil")]
    [Tooltip("Prefab com o script Projectile (esfera pequena SEM collider)")]
    public GameObject projectilePrefab;
    [Tooltip("De onde o tiro sai (filho na altura da boca/mão). Se vazio, sai do peito.")]
    public Transform firePoint;
    public float projectileSpeed = 10f;
    public float damage = 8f;
    [Tooltip("Altura (do chão) que ele mira no player. O rato é baixinho: 0.2 a 0.4")]
    public float aimHeight = 0.3f;

    [Header("Distâncias")]
    [Tooltip("Só atira se o player estiver a menos disso")]
    public float shootRange = 9f;
    [Tooltip("Ele tenta ficar nesta distância (anda até aqui)")]
    public float preferredDistance = 6f;
    [Tooltip("Se o player chegar mais perto que isso, ele recua")]
    public float retreatDistance = 3.5f;

    [Header("Cadência")]
    public float fireCooldown = 2f;
    [Tooltip("Tempo entre começar a animação e o tiro sair")]
    public float shootDelay = 0.8f;
    [Tooltip("Tempo que ele fica parado DEPOIS de soltar a latinha (para terminar a animação do arremesso)")]
    public float afterShotPause = 0.5f;
    public string shootTrigger = "Shoot";

    [Header("Sincronia com a animação")]
    [Tooltip("LIGADO: a latinha sai no Animation Event 'ThrowRelease' do clipe (sempre no frame certo). DESLIGADO: sai depois do Shoot Delay.")]
    public bool useAnimationEvent = true;
    [Tooltip("Se o evento não chegar (esquecido no clipe), a latinha sai depois deste tempo mesmo assim")]
    public float eventTimeout = 1.5f;

    private float lastShotTime = -999f;
    private bool releaseSignal;
    private EnemyBrain brain;

    public bool IsShooting { get; private set; }

    void Awake()
    {
        brain = GetComponent<EnemyBrain>();
    }

    public bool CanShoot()
    {
        return !IsShooting && Time.time - lastShotTime >= fireCooldown;
    }

    public void Shoot(Transform target)
    {
        if (!CanShoot() || target == null)
            return;

        StartCoroutine(ShootRoutine(target));
    }

    IEnumerator ShootRoutine(Transform target)
    {
        IsShooting = true;
        lastShotTime = Time.time;

        if (brain != null && brain.enemyAnimator != null)
            brain.enemyAnimator.SetTrigger(shootTrigger);

        if (useAnimationEvent)
        {
            // Espera o Animation Event 'ThrowRelease' (ou o tempo limite)
            releaseSignal = false;
            float waited = 0f;

            while (!releaseSignal && waited < eventTimeout)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            if (!releaseSignal)
                Debug.LogWarning("[RangedAttack] O evento ThrowRelease não chegou em " + name + ". Adicione o Animation Event no clipe do arremesso.");
        }
        else
        {
            yield return new WaitForSeconds(shootDelay);
        }

        if (target != null)
            Fire(target);

        yield return new WaitForSeconds(afterShotPause);
        IsShooting = false;
    }

    // Chamado pelo Animation Event 'ThrowRelease' (via EnemyAnimationEvents)
    public void ThrowRelease()
    {
        releaseSignal = true;
    }

    void Fire(Transform target)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[RangedAttack] Falta arrastar o Projectile Prefab em " + name);
            return;
        }

        Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.up;
        Vector3 aim = target.position + Vector3.up * aimHeight;
        Vector3 direction = (aim - origin).normalized;

        GameObject go = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));

        Projectile projectile = go.GetComponent<Projectile>();
        if (projectile == null)
            projectile = go.AddComponent<Projectile>();

        projectile.Init(direction, projectileSpeed, damage, transform);
    }

    void OnDisable()
    {
        IsShooting = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);
    }
}
