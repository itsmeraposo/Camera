using System.Collections;
using UnityEngine;

public class RatoAnimator : MonoBehaviour
{
    [Tooltip("Animator do modelo Rato04 (filho do Player)")]
    public Animator animator;

    [Tooltip("Quão rápido o parâmetro acompanha a velocidade real")]
    public float suavizacao = 10f;

    [Header("Soco")]
    [Tooltip("Arraste aqui o objeto HitMao (esfera trigger na mão do rato)")]
    public MaoDano maoDano;
    [Tooltip("Tempo até o punho 'acertar' (segundos)")]
    public float atrasoDano = 0.15f;
    [Tooltip("Quanto tempo a mão causa dano (segundos)")]
    public float duracaoDano = 0.2f;
    [Tooltip("Depois do último clique, quanto tempo o rato fica no soco antes de voltar ao Idle/Walk")]
    public float tempoAteVoltar = 0.35f;

    [Header("Avanço do soco (dá alcance à mão)")]
    [Tooltip("Quanto o modelo do rato avança para frente durante o soco (unidades locais). 0 = não avança")]
    public float avancoSoco = 0.25f;
    [Tooltip("Duração do avanço (vai e volta), em segundos")]
    public float duracaoAvanco = 0.2f;
    [Tooltip("Espera depois do clique antes de começar a avançar")]
    public float atrasoAvanco = 0.0f;

    private Rigidbody rb;
    private float velocidadeSuave;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int SocoHash = Animator.StringToHash("Soco");
    private static readonly int AtacandoHash = Animator.StringToHash("Atacando");
    private float fimDoSoco;

    private Transform modelo;          // o modelo do rato (move só ele, a física do Player não muda)
    private Vector3 posicaoBase;       // posição local original do modelo
    private Coroutine avancoRoutine;

    void Awake()
    {
        rb = GetComponentInParent<Rigidbody>();
        if (animator == null)
            animator = GetComponent<Animator>();

        modelo = animator != null ? animator.transform : transform;
        posicaoBase = modelo.localPosition;
    }
    
    void Update()
    {
        if (animator == null) return;

        Vector3 v = rb.linearVelocity;
        float horizontal = new Vector3(v.x, 0f, v.z).magnitude;

        velocidadeSuave = Mathf.Lerp(velocidadeSuave, horizontal, suavizacao * Time.deltaTime);
        animator.SetFloat(SpeedHash, velocidadeSuave);

        // Quando para de socar, libera a volta para o Locomotion
        if (Time.time >= fimDoSoco)
            animator.SetBool(AtacandoHash, false);
    }

    // Chamado pelo PlayerAttack a cada clique
    public void Soco(float dano)
    {
        if (maoDano == null)
            Debug.LogWarning("[RatoAnimator] O campo 'Mao Dano' está VAZIO: arraste o HitMao para ele, senão o soco nunca causa dano.");

        fimDoSoco = Time.time + tempoAteVoltar;
        if (animator != null)
        {
            animator.SetBool(AtacandoHash, true);
            animator.SetTrigger(SocoHash);
        }
        if (maoDano != null) maoDano.AbrirJanela(dano, atrasoDano, duracaoDano);

        // Avanço do corpo: aproxima a mão do inimigo
        if (avancoSoco > 0f && modelo != null)
        {
            if (avancoRoutine != null)
                StopCoroutine(avancoRoutine);

            avancoRoutine = StartCoroutine(Avanco());
        }
    }

    IEnumerator Avanco()
    {
        if (atrasoAvanco > 0f)
            yield return new WaitForSeconds(atrasoAvanco);

        float t = 0f;
        while (t < duracaoAvanco)
        {
            // Seno: vai até o máximo no meio e volta suave, sem tranco
            float k = Mathf.Sin(t / duracaoAvanco * Mathf.PI);
            modelo.localPosition = posicaoBase + Vector3.forward * (avancoSoco * k);

            t += Time.deltaTime;
            yield return null;
        }

        modelo.localPosition = posicaoBase;
        avancoRoutine = null;
    }

    void OnDisable()
    {
        if (modelo != null)
            modelo.localPosition = posicaoBase;
    }
}