using UnityEngine;

public class StatusThings : MonoBehaviour
{
    private float life = 10f;

    [Header("Queijo escondido na caixa (opcional)")]
    [Tooltip("Arraste aqui o queijo que está dentro desta caixa. Ele fica escondido e só aparece quando a caixa é destruída.")]
    public GameObject queijo;

    void Awake()
    {
        // O queijo só existe de verdade depois que a caixa quebra (não dá para pegar passando por perto)
        if (queijo != null)
            queijo.SetActive(false);
    }

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

            // Libera o queijo: solta da caixa (para não ser destruído junto) e liga
            if (queijo != null)
            {
                queijo.transform.SetParent(null, true);
                queijo.SetActive(true);
            }

            Destroy(gameObject);
        }
    }
}
