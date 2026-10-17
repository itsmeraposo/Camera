using UnityEngine;
using UnityEngine.AI;

// Perseguição do inimigo ATIRADOR (EnemyType.Ranged):
// - fora do alcance de tiro: anda até entrar no alcance
// - dentro do alcance: fica parado, olha pro player e atira
public class RangedChaseState : IEnemyState
{
    private EnemyBrain enemy;
    private RangedAttack ranged;
    private Transform player;
    private float lostSightTimer;
    private const float LoseSightGrace = 1.2f;   // segundos sem ver o rato até desistir e ir procurar

    public RangedChaseState(EnemyBrain enemy)
    {
        this.enemy = enemy;
        ranged = enemy.GetComponent<RangedAttack>();
    }

    public void Enter()
    {
        enemy.SetAlertAnimation(true);
        enemy.movement.SetChaseSpeed();
        player = enemy.sensor != null ? enemy.sensor.player : null;
        lostSightTimer = 0f;

        if (ranged == null)
            Debug.LogWarning("[RangedChaseState] " + enemy.name + " está com Type = Ranged mas não tem o script RangedAttack.");
    }

    public void Update()
    {
        if (player == null || ranged == null)
            return;

        // Perdeu o rato de vista por um instante (visão pisca de perto): não desiste na hora,
        // senão ele alterna Perseguir <-> Procurar e fica indo pra frente e pra trás.
        if (!enemy.CanSeePlayer())
        {
            lostSightTimer += Time.deltaTime;

            if (lostSightTimer >= LoseSightGrace)
            {
                enemy.ChangeState(new SearchState(enemy));
                return;
            }

            Vector3 lost = player.position - enemy.transform.position;
            lost.y = 0f;
            enemy.movement.Stop();
            FaceDirection(lost);
            return;
        }

        lostSightTimer = 0f;
        enemy.SetLastKnownPosition(player.position);

        Vector3 toPlayer = player.position - enemy.transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        // Está no meio de um tiro: fica parado olhando pro player
        if (ranged.IsShooting)
        {
            enemy.movement.Stop();
            FaceDirection(toPlayer);
            return;
        }

        // Fora do alcance: aproxima só até entrar no alcance de tiro
        if (distance > ranged.shootRange)
        {
            enemy.movement.MoveTo(player.position);
            return;
        }

        // Dentro do alcance: fica PARADO olhando pro rato e atira quando a recarga acabar
        // (não anda pra frente nem pra trás entre um tiro e outro)
        enemy.movement.Stop();
        FaceDirection(toPlayer);

        if (ranged.CanShoot())
            ranged.Shoot(player);
    }

    void Retreat(Vector3 toPlayer)
    {
        Vector3 away = -toPlayer.normalized;
        Vector3 target = enemy.transform.position + away * 3f;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            enemy.movement.MoveTo(hit.position);
        }
        else
        {
            // Sem espaço para recuar (parede atrás): fica parado e olha pro player
            enemy.movement.Stop();
            FaceDirection(toPlayer);
        }
    }

    void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion look = Quaternion.LookRotation(direction);
        enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, look, 10f * Time.deltaTime);
    }

    public void Exit()
    {
    }
}
