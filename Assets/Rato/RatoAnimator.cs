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

    private Rigidbody rb;
    private float velocidadeSuave;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int SocoHash = Animator.StringToHash("Soco");
    private static readonly int AtacandoHash = Animator.StringToHash("Atacando");
    private float fimDoSoco;

    void Awake()
    {
        rb = GetComponentInParent<Rigidbody>();
        if (animator == null)
            animator = GetComponent<Animator>();
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
        fimDoSoco = Time.time + tempoAteVoltar;
        if (animator != null)
        {
            animator.SetBool(AtacandoHash, true);
            animator.SetTrigger(SocoHash);
        }
        if (maoDano != null) maoDano.AbrirJanela(dano, atrasoDano, duracaoDano);
    }
}