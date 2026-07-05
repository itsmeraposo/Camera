using UnityEngine;
using UnityEngine.AI;

// IA de movimento do inimigo à distância.
// Diferente do "Chase.cs" (que persegue até encostar), esse tenta manter
// uma distância ideal do player: se aproxima demais, ele recua; se o
// player se afasta, ele se aproxima até voltar pra distância ideal.
public class RangedChase : MonoBehaviour
{
    private GameObject player;
    private NavMeshAgent agent;

    [Header("Distâncias")]
    public float distanciaDeteccao = 25f; // a partir daqui, o inimigo passa a reagir ao player
    public float distanciaIdeal = 12f;    // distância que ele tenta manter pra atirar
    public float distanciaMinima = 8f;    // mais perto que isso, ele recua

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
            Debug.LogError("Nenhum objeto com a tag 'Player' foi encontrado na cena!");

        if (agent == null)
            Debug.LogError("O componente NavMeshAgent está faltando neste GameObject!");
    }

    void Update()
    {
        if (player == null || agent == null) return;

        float distancia = Vector3.Distance(transform.position, player.transform.position);

        if (distancia > distanciaDeteccao)
        {
            // Player fora do alcance de detecção — fica parado
            agent.ResetPath();
            return;
        }

        if (distancia < distanciaMinima)
        {
            // Muito perto — recua na direção oposta ao player
            Vector3 direcaoFuga = (transform.position - player.transform.position).normalized;
            Vector3 destinoFuga = transform.position + direcaoFuga * (distanciaIdeal - distancia);
            agent.SetDestination(destinoFuga);
        }
        else if (distancia > distanciaIdeal + 1f)
        {
            // Longe demais — se aproxima até a distância ideal
            agent.SetDestination(player.transform.position);
        }
        else
        {
            // Distância boa — para de andar e fica de frente pro player
            agent.ResetPath();
            OlharParaPlayer();
        }
    }

    void OlharParaPlayer()
    {
        Vector3 direcao = player.transform.position - transform.position;
        direcao.y = 0f;
        if (direcao.sqrMagnitude < 0.001f) return;

        Quaternion rotacaoAlvo = Quaternion.LookRotation(direcao);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoAlvo, Time.deltaTime * 5f);
    }

    // Desenha os raios no Editor pra facilitar o ajuste das distâncias
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaDeteccao);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaIdeal);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, distanciaMinima);
    }
}
