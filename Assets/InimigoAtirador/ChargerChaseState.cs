using UnityEngine;
using UnityEngine.AI;

// Perseguição do inimigo INVESTIDA (EnemyType.Charger).
// Fases: Chasing (anda atrás) -> Windup (avisa) -> Dash (corre) -> Recover (descansa / atordoado)
public class ChargerChaseState : IEnemyState
{
    enum Phase { Chasing, Windup, Dash, Recover }

    private EnemyBrain enemy;
    private ChargerAttack charger;
    private Transform player;
    private NavMeshAgent agent;

    private Phase phase;
    private float timer;
    private Vector3 dashDirection;
    private float traveled;
    private float recoverDuration;
    private float lostSightTimer;
    private const float LoseSightGrace = 1.5f;   // segundos sem ver o rato até desistir e ir procurar

    public ChargerChaseState(EnemyBrain enemy)
    {
        this.enemy = enemy;
        charger = enemy.GetComponent<ChargerAttack>();
    }

    public void Enter()
    {
        enemy.SetAlertAnimation(true);
        enemy.movement.SetChaseSpeed();
        player = enemy.sensor != null ? enemy.sensor.player : null;
        agent = enemy.movement.agent;
        phase = Phase.Chasing;
        lostSightTimer = 0f;

        if (charger == null)
            Debug.LogWarning("[ChargerChaseState] " + enemy.name + " está com Type = Charger mas não tem o script ChargerAttack.");
    }

    public void Update()
    {
        if (player == null || charger == null || agent == null)
            return;

        switch (phase)
        {
            case Phase.Chasing: UpdateChasing(); break;
            case Phase.Windup:  UpdateWindup();  break;
            case Phase.Dash:    UpdateDash();    break;
            case Phase.Recover: UpdateRecover(); break;
        }
    }

    // ---------- Chasing ----------
    void UpdateChasing()
    {
        // Depois da investida ele costuma ficar de costas pro rato e perde a visão por um instante.
        // Em vez de desistir na hora (e voltar a patrulhar), vira pro rato e espera um pouco.
        if (!enemy.CanSeePlayer())
        {
            lostSightTimer += Time.deltaTime;

            if (lostSightTimer >= LoseSightGrace)
            {
                enemy.ChangeState(new SearchState(enemy));
                return;
            }

            enemy.movement.Stop();
            FaceDirection(FlatDirectionToPlayer(), 6f);
            return;
        }

        lostSightTimer = 0f;
        enemy.SetLastKnownPosition(player.position);

        if (charger.CooldownReady && FlatDistance() <= charger.maxChargeDistance)
        {
            StartWindup();
            return;
        }

        enemy.movement.MoveTo(player.position);
    }

    // ---------- Windup (aviso) ----------
    void StartWindup()
    {
        phase = Phase.Windup;
        timer = 0f;

        enemy.movement.Stop();
        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = false; // sem destino ele fica parado, e o agent.Move da investida funciona
        }

        charger.SetTelegraph(true);

        if (enemy.enemyAnimator != null)
            enemy.enemyAnimator.SetTrigger(charger.windupTrigger);
    }

    void UpdateWindup()
    {
        timer += Time.deltaTime;

        // Acompanha o player, mas trava a mira nos últimos instantes
        if (timer < charger.windupTime - charger.lockAimTime)
            FaceDirection(FlatDirectionToPlayer(), 12f);

        if (timer >= charger.windupTime)
            StartDash();
    }

    // ---------- Dash (investida) ----------
    void StartDash()
    {
        phase = Phase.Dash;
        traveled = 0f;

        dashDirection = enemy.transform.forward;
        dashDirection.y = 0f;
        dashDirection.Normalize();

        charger.SetTelegraph(false);

        if (enemy.enemyAnimator != null)
            enemy.enemyAnimator.SetBool(charger.chargingBool, true);
    }

    void UpdateDash()
    {
        float step = charger.chargeSpeed * Time.deltaTime;

        Vector3 before = agent.nextPosition;
        agent.Move(dashDirection * step);
        Vector3 after = agent.nextPosition;

        Vector3 delta = after - before;
        delta.y = 0f;
        float moved = delta.magnitude;
        traveled += moved;

        enemy.transform.rotation = Quaternion.LookRotation(dashDirection);

        // Acertou o player?
        if (CheckHit())
        {
            EndDash(false);
            return;
        }

        // Quase não saiu do lugar = bateu na parede (a borda do NavMesh barrou)
        if (step > 0.0001f && moved < step * 0.3f)
        {
            EndDash(true);
            return;
        }

        if (traveled >= charger.chargeDistance)
            EndDash(false);
    }

    bool CheckHit()
    {
        Vector3 center = enemy.transform.position + dashDirection * charger.hitForward + Vector3.up * 0.5f;

        Collider[] cols = Physics.OverlapSphere(center, charger.hitRadius, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider col in cols)
        {
            PlayerHealth health = col.GetComponentInParent<PlayerHealth>();

            if (health == null)
                continue;

            health.TakeDamage(charger.damage);
            Debug.Log("Player atingido pela investida!");

            if (charger.knockbackForce > 0f)
            {
                Rigidbody rb = health.GetComponent<Rigidbody>();

                if (rb != null && !rb.isKinematic)
                    rb.AddForce(dashDirection * charger.knockbackForce + Vector3.up, ForceMode.Impulse);
            }

            return true;
        }

        return false;
    }

    void EndDash(bool crashed)
    {
        if (enemy.enemyAnimator != null)
        {
            enemy.enemyAnimator.SetBool(charger.chargingBool, false);

            if (crashed)
                enemy.enemyAnimator.SetTrigger(charger.stunTrigger);
        }

        charger.NotifyChargeEnd();

        recoverDuration = crashed ? charger.stunTime : charger.recoverTime;
        timer = 0f;
        phase = Phase.Recover;
    }

    // ---------- Recover ----------
    void UpdateRecover()
    {
        timer += Time.deltaTime;

        if (timer >= recoverDuration)
        {
            if (agent.isOnNavMesh)
                agent.isStopped = false;

            phase = Phase.Chasing;
        }
    }

    // ---------- Utilidades ----------
    float FlatDistance()
    {
        Vector3 d = player.position - enemy.transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    Vector3 FlatDirectionToPlayer()
    {
        Vector3 d = player.position - enemy.transform.position;
        d.y = 0f;
        return d;
    }

    void FaceDirection(Vector3 direction, float speed)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion look = Quaternion.LookRotation(direction);
        enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, look, speed * Time.deltaTime);
    }

    public void Exit()
    {
        if (charger != null)
            charger.SetTelegraph(false);

        if (charger != null && enemy.enemyAnimator != null)
            enemy.enemyAnimator.SetBool(charger.chargingBool, false);

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;
    }
}
