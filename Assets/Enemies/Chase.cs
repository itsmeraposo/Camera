using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))] // Adiciona o NavMeshAgent automaticamente
public class Chase : MonoBehaviour
{
    private GameObject player;
    private NavMeshAgent agent;

    [Header("Detecção")]
    public float distanciaDeteccao = 30f; // Distância para começar a perseguir

    void Start()
    {
        // Mostra no Console em qual objeto este script está
        Debug.Log("Chase está no objeto: " + gameObject.name);

        // Busca o player pela tag
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            Debug.LogError("Nenhum objeto com a tag 'Player' foi encontrado na cena!");

        // Busca o NavMeshAgent no mesmo GameObject
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            Debug.LogError("O componente NavMeshAgent está faltando neste GameObject!");
        else
            Debug.Log("NavMeshAgent encontrado com sucesso!");

        // Verifica se está em cima de um NavMesh válido
        if (agent != null && !agent.isOnNavMesh)
            Debug.LogWarning("O inimigo NÃO está em cima do NavMesh! Faça o Bake no chão (Window → AI → Navigation → Bake) e mova o inimigo para a área azul.");
    }

    void Update()
    {
        if (player == null || agent == null) return;
        if (!agent.isOnNavMesh) return; // evita erro se não estiver no NavMesh

        float distancia = Vector3.Distance(transform.position, player.transform.position);

        if (distancia <= distanciaDeteccao)
        {
            // Player dentro do alcance — perseguir
            agent.SetDestination(player.transform.position);
        }
        else
        {
            // Player fora do alcance — parar
            agent.ResetPath();
        }
    }

    // Desenha o raio de detecção no Editor para facilitar ajuste
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccao);
    }
}