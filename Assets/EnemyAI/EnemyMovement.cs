using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    public NavMeshAgent agent;

    [Header("Velocidades")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 4f;

    [Header("Patrulha")]
    public Transform[] patrolPoints;

    private int currentPoint = 0;
    private Attack attack;

    void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        attack = GetComponent<Attack>();
        agent.speed = patrolSpeed;
    }

    public void SetPatrolSpeed() { agent.speed = patrolSpeed; }
    public void SetChaseSpeed()  { agent.speed = chaseSpeed; }

    public void Stop()
    {
        agent.isStopped = true;
    }

    public void MoveTo(Vector3 position)
    {
        // Durante o ataque o inimigo fica parado.
        if (attack != null && attack.IsAttacking)
            return;

        agent.isStopped = false;
        agent.SetDestination(position);
    }

    public bool ReachedDestination()
    {
        if (agent.pathPending)
            return false;

        if (agent.remainingDistance > agent.stoppingDistance)
            return false;

        return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
    }

    public void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        MoveTo(patrolPoints[currentPoint].position);

        currentPoint++;

        if (currentPoint >= patrolPoints.Length)
            currentPoint = 0;
    }

    public bool GoToRandomPoint(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            MoveTo(hit.position);
            return true;
        }

        return false;
    }
}
