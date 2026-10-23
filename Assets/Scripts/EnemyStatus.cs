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

    [Header("Knockback")]
    [Tooltip("Distância (m) que o inimigo é empurrado ao levar um golpe. 0 = não empurra")]
    public float knockbackDistancia = 0.6f;
    [Tooltip("Duração (s) do empurrão")]
    public float knockbackTempo = 0.15f;
    [Tooltip("Raio do corpo (m) usado para não atravessar paredes quando o inimigo morre empurrado")]
    public float raioCorpo = 0.4f;
    [Tooltip("Folga (m) mantida da parede quando o inimigo morre empurrado (espaço para a animação de queda). Se o corpo ainda entrar na parede, aumente.")]
    public float folgaParede = 0.5f;
    [Tooltip("Trigger do Animator tocado quando o inimigo é empurrado (no lugar do Hit). Vazio ou inexistente = usa o Hit normal.")]
    public string knockbackTrigger = "";

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
    [Tooltip("Nome do ESTADO de morte no Animator. Tocado direto na morte, sem depender das transições.")]
    public string dieState = "Die";
    [Tooltip("Nome do ESTADO de dano (Hit) no Animator. Tocado direto ao levar o golpe, sem esperar a transição. Vazio ou inexistente = usa o trigger.")]
    public string hitState = "Hit";
    [Tooltip("Tempo (s) da mistura até o Hit. 0 = corta direto para o Hit.")]
    public float hitCrossFade = 0.05f;

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
    private RangedAttack ranged;
    private EnemyMovement movement;
    private Coroutine stunRoutine;
    private Coroutine knockbackRoutine;

    void Awake()
    {
        CurrentLife = maxLife;

        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        brain = GetComponent<EnemyBrain>();
        attack = GetComponent<Attack>();
        ranged = GetComponent<RangedAttack>();
        movement = GetComponent<EnemyMovement>();
    }

    // Se o inimigo for destruído sem passar por Die() (ex.: Destroy direto), ainda conta como morto
    void OnDestroy()
    {
        if (!IsDead && gameObject.scene.isLoaded && GameManager.Instance != null)
            GameManager.Instance.InimigoMorreu();
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, null);
    }

    // 'origem' = de onde veio o golpe (o inimigo é empurrado para longe dela)
    public void TakeDamage(float damage, Vector3? origem)
    {
        if (IsDead)
            return;

        CurrentLife -= damage;
        Debug.Log("Inimigo levou " + damage + " de dano. Vida restante: " + CurrentLife);

        bool empurrou = origem.HasValue && Empurrar(origem.Value);

        if (CurrentLife <= 0f)
        {
            Die();
            return;
        }

        FindObjectOfType<AudioController>()?.TocarEfeito(somDano);

        // Se foi empurrado e existe uma animação de knockback, toca ela; senão toca o Hit normal
        if (!(empurrou && SetTriggerIfExists(knockbackTrigger)))
            PlayHitAnimation();

        if (stunTime > 0f)
        {
            if (stunRoutine != null)
                StopCoroutine(stunRoutine);

            stunRoutine = StartCoroutine(Stun());
        }
    }

    // Toca o Hit direto no estado (sem esperar a transição do Any State); reinicia a cada golpe.
    // Se o estado não existir no Animator, usa o trigger como antes.
    void PlayHitAnimation()
    {
        if (animator == null)
            return;

        if (!string.IsNullOrEmpty(hitState))
        {
            int hash = Animator.StringToHash(hitState);
            if (animator.HasState(0, hash))
            {
                animator.CrossFadeInFixedTime(hash, hitCrossFade, 0, 0f);
                return;
            }
        }

        SetTriggerIfExists(hitTrigger);
    }

    // Retorna true se o empurrão começou
    bool Empurrar(Vector3 origem)
    {
        if (knockbackDistancia <= 0f || knockbackTempo <= 0f)
            return false;

        Vector3 dir = transform.position - origem;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return false;

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(Knockback(dir.normalized));
        return true;
    }

    IEnumerator Knockback(Vector3 dir)
    {
        float velocidade = knockbackDistancia / knockbackTempo;
        float t = 0f;

        while (t < knockbackTempo)
        {
            float dt = Time.deltaTime;
            Vector3 passo = dir * velocidade * dt;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Move(passo);   // o NavMesh já impede de atravessar paredes
            }
            else
            {
                // Sem agent (ex.: morreu): move direto, então confere a parede antes de cada passo
                if (BloqueadoPorParede(dir, passo.magnitude))
                    break;

                transform.position += passo;
            }

            t += dt;
            yield return null;
        }

        knockbackRoutine = null;
    }

    // Tem algo sólido (parede, outro inimigo, objeto) logo à frente do empurrão?
    bool BloqueadoPorParede(Vector3 dir, float distancia)
    {
        // Esfera na altura do tronco (um pouco acima do chão, para não bater no piso)
        Vector3 origem = transform.position + Vector3.up * (raioCorpo + 0.2f);
        RaycastHit[] hits = Physics.SphereCastAll(origem, raioCorpo, dir, distancia + folgaParede, ~0, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit h in hits)
        {
            if (h.collider.GetComponentInParent<EnemyStatus>() == this)
                continue;   // o próprio corpo não conta

            return true;
        }

        return false;
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

        // Avisa o GameManager (conta para a vitória)
        if (GameManager.Instance != null)
            GameManager.Instance.InimigoMorreu();

        // Desliga a IA, o ataque e o movimento
        if (brain != null) brain.enabled = false;
        if (attack != null) attack.enabled = false;
        if (ranged != null)
        {
            ranged.CancelAll();      // cancela o arremesso em andamento
            ranged.enabled = false;
        }
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

        bool hasDeathAnimation = PlayDeathAnimation();

        Destroy(gameObject, hasDeathAnimation ? destroyDelay : destroyDelayWithoutAnimation);
    }

    // Toca a morte direto no estado (não depende das transições do Any State)
    // e limpa o Hit pendente, para ele não atropelar a morte.
    bool PlayDeathAnimation()
    {
        if (animator == null)
            return false;

        ResetTriggerIfExists(hitTrigger);
        bool triggered = SetTriggerIfExists(dieTrigger);

        if (!string.IsNullOrEmpty(dieState))
        {
            int hash = Animator.StringToHash(dieState);
            if (animator.HasState(0, hash))
            {
                animator.CrossFade(hash, 0.05f, 0, 0f);
                return true;
            }
        }

        return triggered;
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

    void ResetTriggerIfExists(string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName))
            return;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
            {
                animator.ResetTrigger(triggerName);
                return;
            }
        }
    }
}
