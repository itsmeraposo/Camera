using UnityEngine;

public class ChaseState : IEnemyState
{
    private EnemyBrain enemy;

    public ChaseState(EnemyBrain enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.SetAlertAnimation(true);
        enemy.movement.SetChaseSpeed();
    }

    public void Update()
    {
        if (enemy.sensor == null || enemy.sensor.player == null)
            return;

        if (enemy.CanSeePlayer())
        {
            enemy.SetLastKnownPosition(enemy.sensor.player.position);
            enemy.movement.MoveTo(enemy.sensor.player.position);
        }
        else
        {
            enemy.ChangeState(new SearchState(enemy));
        }
    }

    public void Exit()
    {
    }
}
