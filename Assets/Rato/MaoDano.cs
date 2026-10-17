using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Colocar no objeto HitMao (filho do osso da mão do rato) com um Sphere Collider "Is Trigger".
// INIMIGOS: funcionam como antes (janela + encosto da mão).
// CAIXAS: janela própria, com atraso extra, procurando a cada frame na mão e à frente do rato.
public class MaoDano : MonoBehaviour
{
    public float damage = 10f;

    [Header("MODO (inimigos)")]
    [Tooltip("LIGADO: só causa dano quando a mão (Sphere Collider do HitMao) encosta de verdade no inimigo. Ignora as esferas de alcance e o limite de distância abaixo.")]
    public bool somenteMao = true;

    [Header("LIMITE DE DISTÂNCIA (resolve 'bate de longe')")]
    [Tooltip("Só dá dano se a superfície do alvo estiver a menos disso do centro do rato (no chão, em metros). Diminua para bater só de perto.")]
    public float alcanceMaximo = 0.5f;
    [Tooltip("Desligue para usar só o alcance frontal (a esfera da mão fica num osso e a escala dele pode distorcer o tamanho)")]
    public bool usarEsferaDaMao = true;

    [Header("Alcance à frente do rato")]
    [Tooltip("Além da esfera da mão, procura alvos numa esfera na frente do corpo do rato. Vale para inimigos (se 'somenteMao' estiver desligado) e para caixas.")]
    public bool usarAlcanceFrontal = true;
    [Tooltip("Distância à frente do rato (metros)")]
    public float alcanceFrente = 0.35f;
    [Tooltip("Raio da esfera de alcance")]
    public float raioFrontal = 0.3f;
    [Tooltip("Altura da esfera em relação ao pivô do Player (ajuste para ficar na altura do corpo do inimigo)")]
    public float alturaFrontal = 0.3f;
    public bool mostrarGizmo = true;

    [Header("CAIXAS")]
    [Tooltip("Atraso EXTRA (segundos) só para caixas, somado ao atraso do RatoAnimator. Aumente se a caixa leva dano antes do punho esticar.")]
    public float atrasoCaixa = 0.15f;
    [Tooltip("Margem extra em volta da mão (metros) ao procurar caixas.")]
    public float raioExtraMao = 0.1f;

    [Header("Diagnóstico")]
    [Tooltip("Escreve no Console o que a mão encosta e por que o golpe vale ou não. Desligue quando tudo funcionar.")]
    public bool debugLogs = true;

    private bool ativo;
    private readonly HashSet<Collider> vistos = new HashSet<Collider>();
    private readonly HashSet<Collider> jaAcertou = new HashSet<Collider>();
    private readonly HashSet<Component> caixasAtingidas = new HashSet<Component>();
    private readonly Collider[] buffer = new Collider[32];
    private Collider maoCollider;
    private PlayerFlask playerFlask;
    private Transform corpo;   // transform do Player (objeto com o Rigidbody)

    void Start()
    {
        playerFlask = GetComponentInParent<PlayerFlask>();
        maoCollider = GetComponent<Collider>();
        maoCollider.isTrigger = true;

        Rigidbody rb = GetComponentInParent<Rigidbody>();
        corpo = rb != null ? rb.transform : transform.root;
    }

    // Abre a janela de dano: espera 'atraso' (punho saindo) e fica ativa por 'duracao'
    public void AbrirJanela(float dano, float atraso, float duracao)
    {
        if (debugLogs)
            Debug.Log("[MaoDano] 1) AbrirJanela chamado (dano " + dano + ", atraso " + atraso + ", duração " + duracao + ")");

        vistos.Clear();
        StopAllCoroutines();
        StartCoroutine(Janela(dano, atraso, duracao));
        StartCoroutine(JanelaCaixas(dano, atraso + atrasoCaixa, duracao));
    }

    IEnumerator Janela(float dano, float atraso, float duracao)
    {
        damage = dano;
        ativo = false;
        yield return new WaitForSeconds(atraso);
        jaAcertou.Clear();   // cada alvo leva só 1 dano por soco
        ativo = true;

        float fim = Time.time + duracao;
        while (Time.time < fim)
        {
            if (usarAlcanceFrontal && !somenteMao)
                VerificarAlcanceFrontal();
            yield return null;
        }

        ativo = false;
    }

    // Janela só das caixas: começa depois (atraso + atrasoCaixa) e procura a cada frame
    IEnumerator JanelaCaixas(float dano, float atraso, float duracao)
    {
        yield return new WaitForSeconds(atraso);
        caixasAtingidas.Clear();   // cada caixa leva só 1 dano por soco

        float fim = Time.time + duracao;
        do
        {
            VerificarCaixas(dano);
            yield return null;
        }
        while (Time.time < fim);
    }

    void VerificarCaixas(float dano)
    {
        if (maoCollider != null)
        {
            Bounds b = maoCollider.bounds;
            float raio = Mathf.Max(b.extents.x, b.extents.y, b.extents.z) + raioExtraMao;
            TestarCaixas(b.center, raio, dano);
        }

        if (usarAlcanceFrontal)
            TestarCaixas(CentroFrontal(), raioFrontal, dano);
    }

    void TestarCaixas(Vector3 centro, float raio, float dano)
    {
        int n = Physics.OverlapSphereNonAlloc(centro, raio, buffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            Collider other = buffer[i];

            StatusThings thing = other.GetComponentInParent<StatusThings>();
            if (thing != null && caixasAtingidas.Add(thing))
            {
                if (debugLogs) Debug.Log("[MaoDano] Caixa '" + thing.name + "' atingida (dano " + dano + ")");
                thing.TakeDamage(dano);
            }

            ExplosiveCrate crate = other.GetComponentInParent<ExplosiveCrate>();
            if (crate != null && caixasAtingidas.Add(crate))
            {
                if (debugLogs) Debug.Log("[MaoDano] Caixa explosiva '" + crate.name + "' atingida (dano " + dano + ")");
                crate.TakeDamage(dano);
            }
        }
    }

    void VerificarAlcanceFrontal()
    {
        Collider[] alvos = Physics.OverlapSphere(CentroFrontal(), raioFrontal, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider c in alvos)
            Acertar(c);
    }

    Vector3 CentroFrontal()
    {
        Transform t = corpo != null ? corpo : transform;
        Vector3 flatForward = t.forward;
        flatForward.y = 0f;
        flatForward.Normalize();
        return t.position + flatForward * alcanceFrente + Vector3.up * alturaFrontal;
    }

    // Stay: pega alvos que já estavam dentro da esfera da mão quando a janela abriu
    void OnTriggerStay(Collider other)
    {
        if (!somenteMao && !usarEsferaDaMao) return;
        Acertar(other);
    }

    // Distância (no chão) do centro do rato até a superfície do alvo
    bool NoAlcance(Collider c)
    {
        Transform t = corpo != null ? corpo : transform;
        Vector3 origem = t.position;

        Vector3 ponto;
        if (c is MeshCollider mc && !mc.convex)
            ponto = c.bounds.ClosestPoint(origem);   // MeshCollider não convexo não aceita ClosestPoint
        else
            ponto = c.ClosestPoint(origem);

        Vector3 d = ponto - origem;
        d.y = 0f;
        return d.magnitude <= alcanceMaximo;
    }

    // Caminho dos INIMIGOS (as caixas são tratadas em JanelaCaixas)
    void Acertar(Collider other)
    {
        // Diagnóstico: registra cada coisa que a mão encosta (uma vez por soco)
        if (debugLogs && vistos.Add(other))
            Debug.Log("[MaoDano] 2) mão encostou em '" + other.name + "' (janela ativa: " + ativo + ", é trigger: " + other.isTrigger + ")");

        if (!ativo) return;

        // Caixas têm janela própria, com atraso extra: ignora aqui
        if (other.GetComponentInParent<StatusThings>() != null || other.GetComponentInParent<ExplosiveCrate>() != null)
            return;

        // Ignora triggers de outros objetos (ex.: esfera de detecção do inimigo)
        if (other.isTrigger && other.transform != transform) return;

        // Fora do alcance: não marca como "já acertou", pode entrar no alcance durante a janela
        // (no modo "somente mão", quem decide é o encosto real da mão, sem limite de distância)
        if (!somenteMao && !NoAlcance(other)) return;

        if (!jaAcertou.Add(other)) return;

        EnemyStatus enemy = other.GetComponentInParent<EnemyStatus>();
        if (enemy != null)
        {
            // Um inimigo com vários colliders só leva dano uma vez por soco
            foreach (Collider c in enemy.GetComponentsInChildren<Collider>())
                jaAcertou.Add(c);

            enemy.TakeDamage(damage);
            playerFlask?.OnAcertarInimigo();
            return;
        }

        if (debugLogs)
            Debug.Log("[MaoDano] 3) '" + other.name + "' não tem EnemyStatus: sem dano");
    }

    void OnDrawGizmosSelected()
    {
        if (!mostrarGizmo) return;

        if (corpo == null)
        {
            Rigidbody rb = GetComponentInParent<Rigidbody>();
            corpo = rb != null ? rb.transform : transform.root;
        }

        // Verde = região da mão (usada para caixas)
        Collider c = maoCollider != null ? maoCollider : GetComponent<Collider>();
        if (c != null)
        {
            Bounds b = c.bounds;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(b.center, Mathf.Max(b.extents.x, b.extents.y, b.extents.z) + raioExtraMao);
        }

        // Ciano = alcance à frente do rato
        if (usarAlcanceFrontal)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(CentroFrontal(), raioFrontal);
        }

        // Amarelo = limite de distância dos inimigos (só no modo sem "somenteMao")
        if (!somenteMao)
        {
            Transform t = corpo != null ? corpo : transform;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(t.position, alcanceMaximo);
        }
    }
}
