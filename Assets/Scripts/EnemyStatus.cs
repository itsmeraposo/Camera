using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Vida do inimigo: leva dano, reage ao golpe e morre.
// Mantém o método TakeDamage(float) usado por Sword.cs e MaoDano.cs.
public class EnemyStatus : MonoBehaviour
{
    [Header("Vida")]
    public float maxLife = 30f;

    [Header("Reação ao dano")]
    [Tooltip("Para o inimigo por um instante quando leva um golpe (0 = não para)")]
    public float stunTime = 0.25f;

    [Header("Morte")]
    [Tooltip("Tempo até o corpo sumir (segundos). Dê tempo para a animação de morte tocar.")]
    public float destroyDelay = 2f;
    [Tooltip("Se não houver animação de morte, o corpo some depois deste tempo curto")]
    public float destroyDelayWithoutAnimation = 0.1f;

    [Tooltip("Mantém o corpo encostado no chão durante a animação de morte (resolve o corpo flutuando, sem subir e descer)")]
    public bool snapBodyToGround = true;
    [Tooltip("Folga entre o ponto mais baixo do corpo e o chão (metros). Se o corpo afundar, suba (0.02); se flutuar, desça (pode ser negativo)")]
    public float footOffset = 0f;

    [Header("Animator (opcional)")]
    [Tooltip("Trigger de morte no Animator. Se o parâmetro não existir, é ignorado.")]
    public string dieTrigger = "Die";
    [Tooltip("Trigger de dano no Animator. Se o parâmetro não existir, é ignorado.")]
    public string hitTrigger = "Hit";

    [Header("Sons (índices do AudioController)")]
    public int somDano = 15;
    public int somMorte = 15;

    public float CurrentLife { get; private set; }
    public bool IsDead { get; private set; }

    private float groundY;
    private bool snapActive;
    private SkinnedMeshRenderer[] meshes;

    private Animator animator;
    private NavMeshAgent agent;
    private EnemyBrain brain;
    private Attack attack;
    private EnemyMovement movement;
    private Coroutine stunRoutine;

    void Awake()
    {
        CurrentLife = maxLife;

        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        brain = GetComponent<EnemyBrain>();
        attack = GetComponent<Attack>();
        movement = GetComponent<EnemyMovement>();
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        CurrentLife -= damage;
        Debug.Log("Inimigo levou " + damage + " de dano. Vida restante: " + CurrentLife);

        if (CurrentLife <= 0f)
        {
            Die();
            return;
        }

        FindObjectOfType<AudioController>()?.TocarEfeito(somDano);

        SetTriggerIfExists(hitTrigger);

        if (stunTime > 0f)
        {
            if (stunRoutine != null)
                StopCoroutine(stunRoutine);

            stunRoutine = StartCoroutine(Stun());
        }
    }

    // Para o inimigo por alguns instantes (cancela ataque em andamento) e depois volta ao normal.
    IEnumerator Stun()
    {
        if (attack != null)
        {
            attack.AttackEnd();       // libera o estado de ataque, para não ficar preso (ele religa o agent)
            attack.enabled = false;   // sem atacar durante o atordoamento
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        yield return new WaitForSeconds(stunTime);

        if (IsDead)
            yield break;

        if (attack != null) attack.enabled = true;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;

        stunRoutine = null;
    }

    void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        CurrentLife = 0f;

        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
            stunRoutine = null;
        }

        FindObjectOfType<AudioController>()?.TocarEfeito(somMorte);

        // Desliga a IA, o ataque e o movimento
        if (brain != null) brain.enabled = false;
        if (attack != null) attack.enabled = false;
        if (movement != null) movement.enabled = false;

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
                agent.isStopped = true;

            agent.enabled = false;
        }

        // Desliga os colliders: o rato não bate no corpo e o corpo não bloqueia o caminho
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        // O Animator pode estar sobrescrevendo a posição do modelo (root motion): desliga
        if (animator != null)
            animator.applyRootMotion = false;

        // Descobre onde é o chão (colliders já desligados, então o raio não bate no próprio corpo)
        if (snapBodyToGround && animator != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 5f, ~0, QueryTriggerInteraction.Ignore))
                groundY = hit.point.y;
            else
                groundY = transform.position.y;

            // Usa a malha visível (o ponto mais baixo do corpo de verdade), não os ossos
            meshes = animator.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer m in meshes)
                m.updateWhenOffscreen = true;   // bounds exatos a cada frame

            snapActive = meshes.Length > 0;
        }

        bool hasDeathAnimation = SetTriggerIfExists(dieTrigger);

        Destroy(gameObject, hasDeathAnimation ? destroyDelay : destroyDelayWithoutAnimation);
    }

    // Depois do Animator: mantém o osso mais baixo do corpo encostado no chão
    void LateUpdate()
    {
        if (!snapActive || meshes == null)
            return;

        float lowest = float.MaxValue;
        foreach (SkinnedMeshRenderer m in meshes)
        {
            if (m != null && m.bounds.min.y < lowest)
                lowest = m.bounds.min.y;
        }

        if (lowest == float.MaxValue)
            return;

        float delta = (groundY + footOffset) - lowest;
        transform.position += Vector3.up * delta;
    }

    // Dispara o trigger só se ele existir no Animator (evita aviso no Console). Retorna true se disparou.
    bool SetTriggerIfExists(string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName))
            return false;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
            {
                animator.SetTrigger(triggerName);
                return true;
            }
        }

        return false;
    }
}
