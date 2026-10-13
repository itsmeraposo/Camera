using UnityEngine;

public class SearchState : IEnemyState
{
    private EnemyBrain enemy;
    private float timer;

    public SearchState(EnemyBrain enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        timer = 0f;
        enemy.SetAlertAnimation(true);
        enemy.movement.SetPatrolSpeed();
        enemy.movement.MoveTo(enemy.GetLastKnownPosition());
    }

    public void Update()
    {
        if (enemy.CanSeePlayer())
        {
            enemy.PlayerSeen();
            enemy.ChangeState(new ChaseState(enemy));
            return;
        }

        // O tempo de busca só conta depois de chegar ao último local conhecido.
        if (!enemy.movement.ReachedDestination())
            return;

        timer += Time.deltaTime;

        if (timer >= enemy.SearchDuration())
            enemy.ReturnToDefaultState();
    }

    public void Exit()
    {
    }
}
