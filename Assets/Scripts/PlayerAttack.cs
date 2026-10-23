using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Dano do ataque")]
    public float damage = 10f;

    [Header("Ritmo dos ataques")]
    [Tooltip("Tempo mínimo (s) entre o início de um soco e o próximo. Use perto da duração da animação do soco: menor = ataca mais rápido, mas a animação começa a ser cortada.")]
    public float intervaloEntreAtaques = 0.45f;
    [Tooltip("Se você clicar um pouco ANTES de poder atacar, o clique fica guardado (s) e o soco sai assim que liberar. Deixa o ataque responsivo sem cortar a animação.")]
    public float janelaBuffer = 0.25f;

    [Tooltip("Segurando o botão do mouse, o rato ataca sozinho no ritmo do intervalo")]
    public bool segurarParaRepetir = true;

    [Header("Duração do ataque (segundos)")]
    // Tempo em que o ataque conta como "ativo" (EstaAtacando).
    public float duracaoAtaque = 0.35f;

    [Header("Animação da espada/faca (opcional)")]
    public string animacao = "AnimacaoFaca 0";
    // Tempo de "blend" suave ao trocar de animação (ao invés de cortar instantaneamente)
    public float transicaoAnimacao = 0.1f;

    public Sword sword;

    private bool isAttacking = false; // true enquanto o ataque está "ativo"
    private bool usarSomAlternativo = false;
    private float proximoAtaque = 0f;        // a partir de quando pode atacar de novo
    private float tempoUltimoClique = -999f; // quando foi o último clique ainda não usado

    // Exposto pra Sword.cs saber se deve causar dano agora ou ignorar o toque
    public bool EstaAtacando => isAttacking;

    private RatoAnimator rato;

    void Start()
    {
        sword = GetComponentInChildren<Sword>();
        rato = FindFirstObjectByType<RatoAnimator>();
    }

    void Update()
    {
        // Jogo pausado (menu de pausa, vitória, derrota): cliques na UI não podem atacar
        if (Time.timeScale == 0f) return;

        var botao = Mouse.current.leftButton;
        if (botao.wasPressedThisFrame || (segurarParaRepetir && botao.isPressed))
            tempoUltimoClique = Time.time;

        // Tem um clique guardado (recente) e já pode atacar?
        bool querAtacar = (Time.time - tempoUltimoClique) <= janelaBuffer;
        if (querAtacar && Time.time >= proximoAtaque)
        {
            tempoUltimoClique = -999f; // consome o clique
            StartAttack();
        }
    }

    void StartAttack()
    {
        isAttacking = true;
        proximoAtaque = Time.time + intervaloEntreAtaques;

        if (sword != null)
        {
            sword.damage = damage;
            sword.PlayAnimation(animacao, transicaoAnimacao);
        }

        // Soco do rato (animação + janela de dano na mão)
        if (rato == null)
            Debug.LogWarning("[PlayerAttack] Nenhum RatoAnimator encontrado na cena: o soco do rato não é chamado.");
        else
            rato.Soco(damage);

        TocarSom();

        CancelInvoke(nameof(EndAttack));
        Invoke(nameof(EndAttack), duracaoAtaque);
    }

    void EndAttack()
    {
        isAttacking = false;
    }

    void TocarSom()
    {
        if (usarSomAlternativo)
        {
            FindObjectOfType<AudioController>()?.TocarEfeito(10); // FacaCorte2
        }
        else
        {
            FindObjectOfType<AudioController>()?.TocarEfeito(9);  // FacaCorte
        }

        usarSomAlternativo = !usarSomAlternativo;
    }
}
