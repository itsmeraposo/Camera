using UnityEngine;
using UnityEngine.AI;

// Perseguição do inimigo ATIRADOR (EnemyType.Ranged):
// - longe: anda até a distância ideal
// - perto demais: recua
// - na distância certa: para, olha pro player e atira
public class RangedChaseState : IEnemyState
{
    private EnemyBrain enemy;
    private RangedAttack ranged;
    private Transform player;

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

        if (ranged == null)
            Debug.LogWarning("[RangedChaseState] " + enemy.name + " está com Type = Ranged mas não tem o script RangedAttack.");
    }

    public void Update()
    {
        if (player == null || ranged == null)
            return;

        if (!enemy.CanSeePlayer())
        {
            enemy.ChangeState(new SearchState(enemy));
            return;
        }

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

        // Atira quando está no alcance e o tempo de recarga acabou
        if (distance <= ranged.shootRange && ranged.CanShoot())
        {
            enemy.movement.Stop();
            FaceDirection(toPlayer);
            ranged.Shoot(player);
            return;
        }

        // Player muito perto: recua
        if (distance < ranged.retreatDistance)
        {
            Retreat(toPlayer);
            return;
        }

        // Player longe: aproxima
        if (distance > ranged.preferredDistance)
        {
            enemy.movement.MoveTo(player.position);
            return;
        }

        // Distância ideal: espera a recarga olhando pro player
        enemy.movement.Stop();
        FaceDirection(toPlayer);
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
