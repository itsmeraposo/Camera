using UnityEngine;

public class StatusThings : MonoBehaviour
{
    private float life = 10f;

    public void TakeDamage(float damage)
    {
        life -= damage;
        Debug.Log("Objeto genérico levou " + damage + " de dano. Vida restante: " + life);

        if (life <= 0)
        {
            //  SOM DE CAIXA (índice 0)
            FindObjectOfType<AudioController>()?.TocarEfeito(0);

            // Spawna os inimigos aqui, explicitamente, só quando a caixa realmente morre por dano
            CaixaSpawn caixaSpawn = GetComponent<CaixaSpawn>();
            if (caixaSpawn != null)
            {
                caixaSpawn.SpawnInimigos();
            }

            Destroy(gameObject);
        }
    }
}