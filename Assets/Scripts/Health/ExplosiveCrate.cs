using UnityEngine;
using UnityEngine.VFX;

public class ExplosiveCrate : MonoBehaviour
{
    [Header("Configurações")]
    public float health = 30f;
    public float damage = 15f;
    public float radius = 3f;

    [Header("Efeitos")]
    public GameObject explosionPrefab; // Arraste o prefab da explosão aqui

    public void TakeDamage(float damageAmount)
    {
        health -= damageAmount;
        Debug.Log($"Caixa levou {damageAmount} de dano. Vida restante: {health}");

        if (health <= 0)
        {
            Explode();
        }
    }

    void Explode()
    {
        // Som da explosão
        FindObjectOfType<AudioController>()?.TocarEfeito(8);

        // Dano ao jogador
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance <= radius)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                    Debug.Log($"Caixa explodiu e causou {damage} de dano ao player!");
                }
            }
        }

        // Instancia o prefab da explosão
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(
                explosionPrefab,
                transform.position,
                Quaternion.identity
            );

            // Procura o VisualEffect no prefab
            VisualEffect vfx = explosion.GetComponent<VisualEffect>();

            if (vfx == null)
                vfx = explosion.GetComponentInChildren<VisualEffect>();

            if (vfx != null)
                vfx.Play();

            // Destrói o objeto da explosão após 2 segundos
            Destroy(explosion, 2f);
        }

        // Destrói a caixa
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}