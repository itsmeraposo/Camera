using UnityEngine;

public class EnemyGroup : MonoBehaviour
{
    public GameObject portaParaAbrir; // Arraste a porta aqui no Inspetor

    void Update()
    {
        if (GameObject.FindGameObjectWithTag("Enemy") == null)
        {
            // Se houver uma porta referenciada, abre com animação (ou destrói, se não tiver o script)
            if (portaParaAbrir != null)
            {
                DoorController porta = portaParaAbrir.GetComponent<DoorController>();
                if (porta != null)
                    porta.Abrir();
                else
                {
                    FindObjectOfType<AudioController>()?.TocarEfeito(18);
                    Destroy(portaParaAbrir);
                }
            }

            // Destroi o grupo de inimigos
            Destroy(this.gameObject);
        }
    }
}