using UnityEngine;

// Faz o inimigo atirar um projétil no player quando está dentro do alcance,
// respeitando um cooldown entre tiros. Equivalente ao "EnemySword.cs",
// só que à distância em vez de corpo a corpo.
public class EnemyRangedAttack : MonoBehaviour
{
    [Header("Projétil")]
    public GameObject projetilPrefab;
    public Transform pontoDeTiro; // opcional: arraste um filho posicionado na "arma"/mão do inimigo

    [Header("Combate")]
    public float alcance = 15f;
    public float cooldown = 2f;
    public float danoProjetil = 10f;
    public float velocidadeProjetil = 12f;

    [Header("Áudio")]
    public int indiceSomTiro = 20; // ajuste pro índice livre no seu AudioController

    [Header("Mira")]
    public bool mirarAntecipado = true; // mira à frente do player, baseado em pra onde ele está indo
    public float antecipacao = 0.4f;    // quanto tempo (segundos) à frente ele tenta prever

    [Header("Linha de Visão")]
    public bool exigirLinhaDeVisao = true; // só atira se não tiver parede/obstáculo no caminho
    public LayerMask camadasQueBloqueiam = ~0; // por padrão, qualquer camada bloqueia a visão
    public Transform olhos; // opcional: de onde ele "enxerga" (senão usa a posição dele + altura)

    private GameObject player;
    private Rigidbody playerRb;
    private float ultimoTiro = -999f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerRb = player.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (player == null || projetilPrefab == null) return;

        float distancia = Vector3.Distance(transform.position, player.transform.position);
        if (distancia > alcance) return;

        if (Time.time - ultimoTiro >= cooldown && TemLinhaDeVisao())
        {
            Atirar();
            ultimoTiro = Time.time;
        }
    }

    // Checa se existe algo (parede, caixa, etc.) entre o inimigo e o player.
    // Se bater em algo que não seja o Player nem outro Enemy antes de chegar
    // no alvo, considera que a visão está bloqueada e não atira.
    bool TemLinhaDeVisao()
    {
        if (!exigirLinhaDeVisao) return true;

        Vector3 origem = olhos != null ? olhos.position : transform.position + Vector3.up;
        Vector3 alvo = player.transform.position + Vector3.up;
        Vector3 direcao = alvo - origem;
        float distancia = direcao.magnitude;

        if (Physics.Raycast(origem, direcao.normalized, out RaycastHit hit, distancia, camadasQueBloqueiam))
        {
            // Bateu no próprio player ou em outro inimigo — não conta como bloqueio
            if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Enemy"))
                return true;

            return false; // bateu em outra coisa antes — visão bloqueada
        }

        return true; // raio livre, não bateu em nada — visão livre
    }

    // Desenha o raio de visão no Editor (verde = livre, vermelho = bloqueado)
    void OnDrawGizmosSelected()
    {
        if (player == null) return;

        Vector3 origem = olhos != null ? olhos.position : transform.position + Vector3.up;
        Vector3 alvo = player.transform.position + Vector3.up;

        Gizmos.color = TemLinhaDeVisao() ? Color.green : Color.red;
        Gizmos.DrawLine(origem, alvo);
    }

    void Atirar()
    {
        // Se não tiver um "pontoDeTiro" definido, nasce um pouco à frente E acima do
        // inimigo — só "para cima" (como era antes) corria o risco de nascer dentro
        // do próprio corpo/chão e se autodestruir na hora ao bater em algo que não é Player/Enemy.
        Vector3 origem = pontoDeTiro != null
            ? pontoDeTiro.position
            : transform.position + transform.forward * 1f + Vector3.up * 1.3f;

        Vector3 posicaoAlvo = player.transform.position;

        // Mira à frente do player, na direção que ele está se movendo agora
        if (mirarAntecipado && playerRb != null)
        {
            posicaoAlvo += playerRb.linearVelocity * antecipacao;
        }

        Vector3 alvo = posicaoAlvo + Vector3.up; // mira mais ou menos na altura do peito

        GameObject projetil = Instantiate(projetilPrefab, origem, Quaternion.LookRotation(alvo - origem));

        // Garante que o projétil nunca vai colidir com o próprio inimigo que o disparou,
        // mesmo que nasça encostado nele (independe da tag, é uma trava a mais).
        Collider colProjetil = projetil.GetComponent<Collider>();
        Collider colInimigo = GetComponent<Collider>();
        if (colProjetil != null && colInimigo != null)
            Physics.IgnoreCollision(colProjetil, colInimigo);

        EnemyProjectile script = projetil.GetComponent<EnemyProjectile>();
        if (script != null)
        {
            script.dano = danoProjetil;
            script.velocidade = velocidadeProjetil;
        }

        //  SOM DE TIRO — ajuste "indiceSomTiro" no Inspector pro índice certo do seu AudioController
        FindObjectOfType<AudioController>()?.TocarEfeito(indiceSomTiro);
    }
}
