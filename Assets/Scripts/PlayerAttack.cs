using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header(" COMBO - Dano de cada golpe (1, 2 e 3)")]
    public float[] comboDamage = { 10f, 15f, 25f };

    [Header(" COMBO - Nome do estado no Animator da espada para cada golpe")]
    // Por padrão os 3 apontam pro mesmo estado (só tem uma animação pronta ainda).
    // Quando você criar animações novas, é só trocar o nome do estado aqui.
    public string[] comboAnimations = { "AnimacaoFaca 0", "AnimacaoFaca 0", "AnimacaoFaca 0" };

    [Header(" COMBO - Duração MÁXIMA de cada golpe (segundos)")]
    // Se o jogador NÃO clicar de novo, o golpe "termina" sozinho depois desse tempo
    // e abre a janela do combo. Se o jogador clicar antes disso, a animação é
    // CANCELADA na hora e já parte pro próximo golpe (não precisa tocar inteira).
    public float[] comboDuration = { 0.35f, 0.35f, 0.5f };

    [Header(" COMBO - Janela para continuar o combo depois que o golpe termina sozinho")]
    public float comboWindow = 0.8f;

    [Header(" COMBO - Suavização entre golpes (segundos)")]
    // Tempo de "blend" suave ao trocar de animação (ao invés de cortar instantaneamente)
    public float transicaoEntreGolpes = 0.1f;

    public Sword sword;

    private int comboStep = 0;        // índice do próximo golpe (0 = Ataque 1)
    private bool isAttacking = false; // true enquanto o golpe atual está "ativo"
    private float comboTimer = 0f;    // contagem regressiva da janela do combo

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
        // Se a janela do combo estourar sem novo clique, volta pro Ataque 1
        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                comboStep = 0;
                Debug.Log("Combo resetado (demorou demais pra encadear).");
            }
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isAttacking)
            {
                //  CANCELA a animação atual na hora e já avança pro próximo golpe
                CancelInvoke(nameof(EndAttack));
                AdvanceCombo();
                StartAttack();
            }
            else
            {
                StartAttack();
            }
        }
    }

    void StartAttack()
    {
        isAttacking = true;
        comboTimer = 0f;

        int index = Mathf.Clamp(comboStep, 0, comboDamage.Length - 1);

        if (sword != null)
        {
            sword.damage = comboDamage[index];
            sword.PlayAnimation(comboAnimations[index], transicaoEntreGolpes);
        }

        // Soco do rato (animação + janela de dano na mão)
        if (rato == null)
            Debug.LogWarning("[PlayerAttack] Nenhum RatoAnimator encontrado na cena: o soco do rato não é chamado.");
        else
            rato.Soco(comboDamage[index]);

        TocarSom();

        Debug.Log("Golpe " + (index + 1) + " do combo!");

        // Isso só executa se o jogador NÃO clicar de novo antes do tempo acabar
        float duration = comboDuration[Mathf.Clamp(index, 0, comboDuration.Length - 1)];
        CancelInvoke(nameof(EndAttack));
        Invoke(nameof(EndAttack), duration);
    }

    // Chamado quando o golpe termina "naturalmente" (jogador não clicou de novo a tempo)
    void EndAttack()
    {
        isAttacking = false;
        AdvanceCombo();
        comboTimer = comboWindow; // abre a janela pra continuar o combo
    }

    void AdvanceCombo()
    {
        comboStep++;
        if (comboStep >= comboDamage.Length)
        {
            comboStep = 0; // completou os 3 golpes, combo reinicia do começo
        }
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
