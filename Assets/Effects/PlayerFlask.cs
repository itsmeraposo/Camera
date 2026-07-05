using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerFlask : MonoBehaviour
{
    [Header("Configuração da Poção")]
    public float duracaoEfeito = 15f;
    public float cooldownRecarga = 30f;

    [Header("Efeito de Cura por Acerto")]
    public float curaPorAcerto = 10f;

    [Header("Efeito de Tamanho da Faca")]
    public float multiplicadorTamanhoFaca = 3f;

    [Header("Efeito Visual (VFX Graph)")]
    public VisualEffect vfxFuria; // Arraste o Visual Effect da Faca aqui
    public VisualEffect vfxAura; // Arraste o Visual Effect da Aura do Player aqui (separado da faca)

    [Header("UI")]
    public TextMeshProUGUI flaskStatusText;
    public Slider flaskBar;

    private bool efeitoAtivo = false;
    private bool podeUsar = true;
    private float cooldownTimer = 0f;

    private PlayerAttack playerAttack;
    private PlayerHealth playerHealth;
    private Sword sword;

    private Vector3 escalaOriginalFaca;

    public bool EfeitoAtivo => efeitoAtivo;
    public float TempoRestante { get; private set; }
    public float CooldownRestante { get; private set; }
    public bool PodeUsar => podeUsar;

    void Start()
    {
        playerAttack = GetComponent<PlayerAttack>();
        playerHealth = GetComponent<PlayerHealth>();

        // Busca a espada diretamente, não depende da ordem de inicialização do PlayerAttack
        sword = GetComponentInChildren<Sword>();

        if (sword != null)
            escalaOriginalFaca = sword.transform.localScale;

        if (vfxFuria != null)
            vfxFuria.Stop();

        if (vfxAura != null)
            vfxAura.Stop();
    }

    void Update()
    {
        // Cooldown de recarga da poção
        if (!podeUsar)
        {
            cooldownTimer += Time.deltaTime;
            CooldownRestante = Mathf.Max(0f, cooldownRecarga - cooldownTimer);

            if (cooldownTimer >= cooldownRecarga)
            {
                podeUsar = true;
                cooldownTimer = 0f;
                CooldownRestante = 0f;
            }
        }

        // Botão direito ativa a poção
        if (Mouse.current.rightButton.wasPressedThisFrame && podeUsar && !efeitoAtivo)
        {
            StartCoroutine(AtivarFlask());
        }

        AtualizarUI();
    }

    void AtualizarUI()
    {
        if (flaskStatusText != null)
        {
            if (efeitoAtivo)
                flaskStatusText.text = $"Fúria de Combate: {TempoRestante:0.0}s";
            else if (!podeUsar)
                flaskStatusText.text = $"Recarregando: {CooldownRestante:0.0}s";
            else
                flaskStatusText.text = "Flask Pronta!";
        }

        if (flaskBar != null)
        {
            if (efeitoAtivo)
            {
                flaskBar.value = TempoRestante / duracaoEfeito;
            }
            else if (!podeUsar)
            {
                flaskBar.value = 1f - (CooldownRestante / cooldownRecarga);
            }
            else
            {
                flaskBar.value = 1f;
            }
        }
    }

    IEnumerator AtivarFlask()
    {
        efeitoAtivo = true;
        podeUsar = false;
        cooldownTimer = 0f;
        TempoRestante = duracaoEfeito;

        // Som de poção ativada (índice 21 - ajuste se necessário)
        FindFirstObjectByType<AudioController>()?.TocarEfeito(21);

        // Ativa o efeito visual
        if (vfxFuria != null)
            vfxFuria.Play();

        if (vfxAura != null)
            vfxAura.Play();

        // Aplica tamanho da faca
        if (sword != null)
            sword.transform.localScale = escalaOriginalFaca * multiplicadorTamanhoFaca;

        Debug.Log("Flask ativado! Fúria de combate por " + duracaoEfeito + " segundos.");

        // Contagem regressiva do efeito
        while (TempoRestante > 0f)
        {
            TempoRestante -= Time.deltaTime;
            yield return null;
        }

        // Reverte os efeitos
        if (sword != null)
            sword.transform.localScale = escalaOriginalFaca;

        if (vfxFuria != null)
            vfxFuria.Stop();

        if (vfxAura != null)
            vfxAura.Stop();

        efeitoAtivo = false;
        TempoRestante = 0f;

        Debug.Log("Flask terminou.");
    }

    // Chamado pelo Sword/PlayerAttack quando acerta um inimigo
    public void OnAcertarInimigo()
    {
        if (!efeitoAtivo || playerHealth == null) return;

        playerHealth.Heal(curaPorAcerto);
    }
}
