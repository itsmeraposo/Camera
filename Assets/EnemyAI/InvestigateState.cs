using UnityEngine;

public class InvestigateState : IEnemyState
{
    private EnemyBrain enemy;
    private Vector3 position;

    public InvestigateState(EnemyBrain enemy, Vector3 position)
    {
        this.enemy = enemy;
        this.position = position;
    }

    public void Enter()
    {
        enemy.SetAlertAnimation(false);
        enemy.movement.SetPatrolSpeed();
        enemy.movement.MoveTo(position);
    }

    public void Update()
    {
        if (enemy.CanSeePlayer())
        {
            enemy.PlayerSeen();
            enemy.ChangeState(new ChaseState(enemy));
            return;
        }

        if (enemy.movement.ReachedDestination())
            enemy.ChangeState(new SearchState(enemy));
    }

    public void Exit()
    {
    }
}
