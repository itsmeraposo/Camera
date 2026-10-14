using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Colocar no objeto HitMao (filho do osso da mão do rato) com um Sphere Collider "Is Trigger".
public class MaoDano : MonoBehaviour
{
    public float damage = 10f;

    [Header("MODO")]
    [Tooltip("LIGADO: só causa dano quando a mão (Sphere Collider do HitMao) encosta de verdade no inimigo. Ignora as esferas de alcance e o limite de distância abaixo.")]
    public bool somenteMao = true;

    [Header("LIMITE DE DISTÂNCIA (resolve 'bate de longe')")]
    [Tooltip("Só dá dano se a superfície do alvo estiver a menos disso do centro do rato (no chão, em metros). Diminua para bater só de perto.")]
    public float alcanceMaximo = 0.5f;
    [Tooltip("Desligue para usar só o alcance frontal (a esfera da mão fica num osso e a escala dele pode distorcer o tamanho)")]
    public bool usarEsferaDaMao = true;

    [Header("Alcance à frente do rato (garante o acerto de perto)")]
    [Tooltip("Além da esfera da mão, procura alvos numa esfera na frente do corpo do rato")]
    public bool usarAlcanceFrontal = true;
    [Tooltip("Distância à frente do rato (metros)")]
    public float alcanceFrente = 0.35f;
    [Tooltip("Raio da esfera de alcance")]
    public float raioFrontal = 0.3f;
    [Tooltip("Altura da esfera em relação ao pivô do Player (ajuste para ficar na altura do corpo do inimigo)")]
    public float alturaFrontal = 0.3f;
    public bool mostrarGizmo = true;

    [Header("Diagnóstico")]
    [Tooltip("Escreve no Console o que a mão encosta e por que o golpe vale ou não. Desligue quando tudo funcionar.")]
    public bool debugLogs = true;

    private bool ativo;
    private readonly HashSet<Collider> vistos = new HashSet<Collider>();
    private readonly HashSet<Collider> jaAcertou = new HashSet<Collider>();
    private PlayerFlask playerFlask;
    private Transform corpo;   // transform do Player (objeto com o Rigidbody)

    void Start()
    {
        playerFlask = GetComponentInParent<PlayerFlask>();
        GetComponent<Collider>().isTrigger = true;

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

    void Acertar(Collider other)
    {
        // Diagnóstico: registra cada coisa que a mão encosta (uma vez por soco)
        if (debugLogs && vistos.Add(other))
            Debug.Log("[MaoDano] 2) mão encostou em '" + other.name + "' (janela ativa: " + ativo + ", é trigger: " + other.isTrigger + ")");

        if (!ativo) return;

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

        StatusThings thing = other.GetComponentInParent<StatusThings>();
        if (thing != null) { thing.TakeDamage(damage); return; }

        ExplosiveCrate crate = other.GetComponentInParent<ExplosiveCrate>();
        if (crate != null) { crate.TakeDamage(damage); return; }

        if (debugLogs)
            Debug.Log("[MaoDano] 3) '" + other.name + "' não tem EnemyStatus, StatusThings nem ExplosiveCrate: sem dano");
    }

    void OnDrawGizmosSelected()
    {
        if (!mostrarGizmo || !usarAlcanceFrontal || somenteMao) return;

        if (corpo == null)
        {
            Rigidbody rb = GetComponentInParent<Rigidbody>();
            corpo = rb != null ? rb.transform : transform.root;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(CentroFrontal(), raioFrontal);

        // Círculo amarelo = limite de distância (alcanceMaximo) ao redor do rato
        Transform t = corpo != null ? corpo : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(t.position, alcanceMaximo);
    }
}
