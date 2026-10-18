using UnityEngine;

// Porta física com HingeJoint.
// Vai no objeto "Dobradiça" (o pivô na borda da porta). O menu GameObject > Hotel > Criar Porta com Marco
// já monta tudo e adiciona este script.
//
// - O rato empurra a porta (ela gira na dobradiça) e uma mola a fecha devagar.
// - Inimigos (objetos com EnemyBrain) que chegam perto abrem a porta sozinhos, para o lado oposto a eles.
//   O NavMeshAgent não empurra objetos físicos, então sem isso eles atravessariam a porta fechada.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HingeJoint))]
public class PortaHinge : MonoBehaviour
{
    [Header("Movimento")]
    [Tooltip("Ângulo máximo de abertura para cada lado (graus)")]
    public float anguloMaximo = 100f;
    [Tooltip("Massa da porta. Mais leve = o rato empurra com mais facilidade")]
    public float massa = 3f;
    [Tooltip("Força da mola que traz a porta de volta para fechada (0 = a porta fica aberta onde parar)")]
    public float forcaFechar = 15f;
    [Tooltip("Amortecimento: evita a porta balançar de um lado para o outro")]
    public float amortecimento = 6f;
    [Tooltip("Atrito do giro")]
    public float atritoGiro = 1.5f;

    [Header("Trancada")]
    public bool trancada = false;

    [Header("Rato (Player) abre a porta")]
    [Tooltip("A porta abre sozinha quando o objeto com a tag Player chega perto. Funciona com qualquer tipo de movimento.")]
    public bool ratoAbrePorta = true;
    public string tagDoRato = "Player";
    [Tooltip("Distância (no chão, a partir do centro da porta) em que o rato faz a porta abrir. Menor = abre mais tarde.")]
    public float raioRato = 0.8f;
    [Tooltip("Deixa a porta atravessar o rato enquanto abre. Normalmente desligado (a porta empurra o rato de verdade).")]
    public bool portaAtravessaRato = false;

    [Header("Inimigos abrem a porta")]
    public bool inimigosAbremPorta = true;
    [Tooltip("Distância (a partir do centro da porta) em que o inimigo faz a porta abrir")]
    public float raioDeteccao = 1.1f;
    public float velocidadeAbrir = 140f;
    public float forcaAbrir = 60f;
    [Tooltip("Marque se a porta abrir PARA o inimigo em vez de se afastar dele")]
    public bool inverterSentido = false;

    private Rigidbody rb;
    private HingeJoint hinge;
    private Transform raiz;
    private float proximaVerificacao;
    private readonly Collider[] buffer = new Collider[16];

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        hinge = GetComponent<HingeJoint>();
        raiz = transform.parent != null ? transform.parent : transform;

                rb.mass = massa;
        rb.angularDamping = atritoGiro;
        rb.constraints = RigidbodyConstraints.None;   // o HingeJoint já prende a porta; travar a posição impede o giro
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.sleepThreshold = 0f;   // a porta nunca "dorme", senão o motor não a move
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        hinge.autoConfigureConnectedAnchor = true;   // preso ao mundo
        hinge.anchor = Vector3.zero;                 // a dobradiça fica na origem deste objeto
        hinge.axis = Vector3.up;

        Aplicar();
    }

    void OnValidate()
    {
        if (Application.isPlaying && hinge != null)
            Aplicar();
    }

    void Aplicar()
    {
        hinge.useLimits = true;
        JointLimits lim = new JointLimits();
        lim.min = trancada ? 0f : -anguloMaximo;
        lim.max = trancada ? 0f : anguloMaximo;
        lim.bounciness = 0f;
        hinge.limits = lim;

        hinge.useMotor = false;
        hinge.useSpring = forcaFechar > 0f;
        JointSpring mola = new JointSpring();
        mola.spring = forcaFechar;
        mola.damper = amortecimento;
        mola.targetPosition = 0f;
        hinge.spring = mola;
    }

    public void SetTrancada(bool valor)
    {
        trancada = valor;
        Aplicar();
    }

    void FixedUpdate()
    {
        if ((!inimigosAbremPorta && !ratoAbrePorta) || trancada)
            return;

        proximaVerificacao -= Time.fixedDeltaTime;
        if (proximaVerificacao > 0f)
            return;
        proximaVerificacao = 0.1f;

        Verificar();
    }

    void Verificar()
    {
        Vector3 centro = raiz.position + Vector3.up * 1f;
        int n = Physics.OverlapSphereNonAlloc(centro, raioDeteccao, buffer, ~0, QueryTriggerInteraction.Ignore);

        Transform inimigo = null;
        Transform rato = null;
        for (int i = 0; i < n; i++)
        {
            if (inimigosAbremPorta)
            {
                EnemyBrain brain = buffer[i].GetComponentInParent<EnemyBrain>();
                if (brain != null)
                {
                    inimigo = brain.transform;
                    break;
                }
            }

            if (ratoAbrePorta)
            {
                Transform tr = AcharTag(buffer[i].transform, tagDoRato);
                if (tr != null)
                {
                    Vector3 d = tr.position - raiz.position;
                    d.y = 0f;
                    if (d.magnitude > raioRato)
                        continue;   // rato ainda longe: não abre
                    inimigo = buffer[i].transform;
                    rato = tr;
                    break;
                }
            }
        }

        if (inimigo != null)
        {
            if (rato != null && portaAtravessaRato) IgnorarRato(rato, true);
            Abrir(inimigo.position);
        }
        else
            Soltar();
    }

    static Transform AcharTag(Transform t, string tag)
    {
        while (t != null)
        {
            if (t.CompareTag(tag)) return t;
            t = t.parent;
        }
        return null;
    }

    // A porta atravessa o rato enquanto abre (evita o rato prender a folha fina da porta)
    private Collider[] portaCols;
    private Collider[] ratoCols;
    private Transform ratoIgnorado;
    private bool ignorando;

    void IgnorarRato(Transform rato, bool ignorar)
    {
        if (portaCols == null) portaCols = GetComponentsInChildren<Collider>();
        if (rato != null && rato != ratoIgnorado)
        {
            if (ignorando) AplicarIgnorar(false);
            ratoIgnorado = rato;
            ratoCols = rato.GetComponentsInChildren<Collider>();
        }
        if (ratoCols == null || ignorando == ignorar) return;
        AplicarIgnorar(ignorar);
    }

    void AplicarIgnorar(bool ignorar)
    {
        foreach (Collider p in portaCols)
            foreach (Collider r in ratoCols)
                if (p != null && r != null) Physics.IgnoreCollision(p, r, ignorar);
        ignorando = ignorar;
    }

    void Abrir(Vector3 posicaoInimigo)
    {
        // Lado em que o inimigo está em relação à porta: a porta abre para o lado oposto
        float lado = Vector3.Dot(posicaoInimigo - raiz.position, raiz.forward);
        float dir = lado >= 0f ? 1f : -1f;
        if (inverterSentido) dir = -dir;

        rb.WakeUp();
        if (!hinge.useMotor)
            Debug.Log("[Porta] abrindo (dir " + dir + ", angulo atual " + hinge.angle.ToString("F1") + ")");

        hinge.useSpring = false;
        hinge.useMotor = true;

        JointMotor motor = new JointMotor();
        motor.targetVelocity = dir * velocidadeAbrir;
        motor.force = forcaAbrir;
        motor.freeSpin = false;
        hinge.motor = motor;
    }

    void Soltar()
    {
        // porta já fechou: volta a colidir com o rato
        if (ignorando && Mathf.Abs(hinge.angle) < 3f)
            IgnorarRato(null, false);

        hinge.useMotor = false;
        hinge.useSpring = forcaFechar > 0f;
    }

    void OnDrawGizmosSelected()
    {
        Transform r = transform.parent != null ? transform.parent : transform;
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.6f);
        Gizmos.DrawWireSphere(r.position + Vector3.up, raioDeteccao);
    }
}
