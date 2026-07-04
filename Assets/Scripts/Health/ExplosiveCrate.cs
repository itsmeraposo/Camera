using UnityEngine;
using UnityEngine.VFX;

public class ExplosiveCrate : MonoBehaviour
{
    public float health = 30f;          // Vida da caixa
    public float damage = 15f;          // Dano ao player
    public float radius = 3f;           // Raio da explosão
    public VisualEffect explosionEffect; // 🔥 Agora aceita VFX Graph

    //  Método chamado pela espada (Sword.cs)
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
        //  SOM DE EXPLOSÃO (índice 8) via AudioController
        FindObjectOfType<AudioController>()?.TocarEfeito(8);

        // Verifica se o player está dentro do raio
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

        // Efeito de partículas (se tiver)
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // Destroi a caixa
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}