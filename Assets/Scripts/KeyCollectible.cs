using UnityEngine;
using System.Collections;

public class KeyCollectible : MonoBehaviour
{
    [Header("Porta que será aberta")]
    public GameObject doorToOpen;

    [Header("Tempo para a porta desaparecer após destravar")]
    public float unlockDelay = 0.5f;

    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (other.CompareTag("Player"))
        {
            collected = true;
            StartCoroutine(OpenDoorSequence());
        }
    }

    private IEnumerator OpenDoorSequence()
    {
        // Som da chave sendo coletada (índice 3)
        FindObjectOfType<AudioController>()?.TocarEfeito(3);

        yield return new WaitForSeconds(0.2f);

        // Som da fechadura destravando (índice 7)
        FindObjectOfType<AudioController>()?.TocarEfeito(7);

        yield return new WaitForSeconds(unlockDelay);

        // Som de abrir porta (índice 18) — já toca de novo dentro do DoorController também,
        // então removi a duplicata daqui; o som só dispara uma vez, no Abrir().
        yield return new WaitForSeconds(0.5f);

        // Abre a porta com animação em vez de simplesmente destruí-la.
        // Se a porta não tiver o DoorController (ex: você ainda não configurou),
        // cai no comportamento antigo como segurança.
        if (doorToOpen != null)
        {
            DoorController porta = doorToOpen.GetComponent<DoorController>();
            if (porta != null)
                porta.Abrir();
            else
                Destroy(doorToOpen);
        }

        // Remove a chave
        Destroy(gameObject);
    }
}
