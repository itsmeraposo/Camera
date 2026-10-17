using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Dano do ataque")]
    public float damage = 10f;

    [Header("Duração do ataque (segundos)")]
    // Tempo em que o ataque conta como "ativo" (EstaAtacando). Se clicar de novo, o ataque reinicia.
    public float duracaoAtaque = 0.35f;

    [Header("Animação da espada/faca (opcional)")]
    public string animacao = "AnimacaoFaca 0";
    // Tempo de "blend" suave ao trocar de animação (ao invés de cortar instantaneamente)
    public float transicaoAnimacao = 0.1f;

    public Sword sword;

    private bool isAttacking = false; // true enquanto o ataque está "ativo"
    private bool usarSomAlternativo = false;

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
        if (Mouse.current.leftButton.wasPressedThisFrame)
            StartAttack();
    }

    void StartAttack()
    {
        isAttacking = true;

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
