using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public float maxHealth = 100f;

    [Header("Animação de dano")]
    [Tooltip("Deixe vazio para procurar o Animator do rato automaticamente")]
    public Animator animator;
    [Tooltip("Nome do Trigger no Animator")]
    public string hitTrigger = "Hit";
    [Tooltip("Tempo mínimo entre uma animação de dano e outra (evita ficar travado tomando hit)")]
    public float hitCooldown = 0.4f;

    private float currentHealth;
    private float lastHitTime = -999f;

    public System.Action<float, float> OnHealthChanged;

    public bool useDeathScreen = false;
    public System.Action OnPlayerDied;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogWarning("[PlayerHealth] Nenhum Animator encontrado: arraste o Animator do rato no campo Animator.");
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0f)
            return;

        currentHealth -= damage;

        if (currentHealth < 0f)
            currentHealth = 0f;

        // 🔥 SOM DE DANO (índice 11 - GemidoDor)
        FindObjectOfType<AudioController>()?.TocarEfeito(11);

        Debug.Log($"Dano recebido: {damage}. Vida atual: {currentHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            PlayHitAnimation();
        }
    }

    void PlayHitAnimation()
    {
        if (animator == null) return;
        if (Time.time - lastHitTime < hitCooldown) return;
        if (!HasParameter(hitTrigger)) return;

        lastHitTime = Time.time;
        animator.SetTrigger(hitTrigger);
    }

    bool HasParameter(string paramName)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == paramName && p.type == AnimatorControllerParameterType.Trigger)
                return true;

        return false;
    }

    // 🔥 NOVO: Cura o jogador (usado pelo Flask de Fúria)
    public void Heal(float amount)
    {
        if (currentHealth <= 0f)
            return;

        currentHealth += amount;

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        Debug.Log($"Curou {amount}. Vida atual: {currentHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Die()
    {
        // 🔥 SOM DE MORTE DO JOGADOR (índice 14 - Morte)
        FindObjectOfType<AudioController>()?.TocarEfeito(14);

        Debug.Log("Jogador morreu! Recarregando cena...");

        if (!useDeathScreen)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            OnPlayerDied?.Invoke();
        }
    }
}
