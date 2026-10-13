using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    [Header("Systems")]
    public EnemySensor sensor;
    public EnemyMovement movement;
    public EnemyAnimator enemyAnimator;
    public EnemyCommunication communication;

    [Header("Behavior")]
    public bool isPatroller = true;
    public float searchTime = 5f;

    private IEnemyState currentState;

    private Vector3 lastKnownPosition;
    private bool hasNoise;
    private bool hasAlert;

    void Start()
    {
        if (sensor != null && sensor.player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                sensor.player = playerObject.transform;
        }

        if (isPatroller)
            ChangeState(new PatrolState(this));
        else
            ChangeState(new WanderState(this));
    }

    void Update()
    {
        currentState?.Update();

        if (enemyAnimator != null && movement != null)
            enemyAnimator.SetSpeed(movement.agent.velocity.magnitude);
    }

    public void ChangeState(IEnemyState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public void ReturnToDefaultState()
    {
        if (isPatroller)
            ChangeState(new PatrolState(this));
        else
            ChangeState(new WanderState(this));
    }

    public bool CanSeePlayer()
    {
        return sensor != null && sensor.CanSeePlayer();
    }

    public Vector3 GetLastKnownPosition()
    {
        return lastKnownPosition;
    }

    public void SetLastKnownPosition(Vector3 position)
    {
        lastKnownPosition = position;
    }

    public void SetAlertAnimation(bool alert)
    {
        if (enemyAnimator != null)
            enemyAnimator.SetAlert(alert);
    }

    public void ReceiveNoise(Vector3 position)
    {
        lastKnownPosition = position;
        hasNoise = true;
    }

    public void ReceiveAlert(Vector3 position)
    {
        lastKnownPosition = position;
        hasAlert = true;
    }

    public bool ConsumeNoise(out Vector3 position)
    {
        position = lastKnownPosition;

        if (!hasNoise)
            return false;

        hasNoise = false;
        return true;
    }

    public bool ConsumeAlert(out Vector3 position)
    {
        position = lastKnownPosition;

        if (!hasAlert)
            return false;

        hasAlert = false;
        return true;
    }

    public void PlayerSeen()
    {
        lastKnownPosition = sensor.player.position;

        if (communication != null)
            communication.AlertNearbyEnemies(lastKnownPosition);
    }

    public float SearchDuration()
    {
        return searchTime;
    }
}
