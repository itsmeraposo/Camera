using UnityEngine;
using System.Collections;

// Coloque este script NA PORTA (não no objeto que manda ela abrir).
// Ele troca "a porta desaparece" por "a porta toca uma animação de abertura",
// e depois desativa o Collider dela, pra o player poder atravessar.
public class DoorController : MonoBehaviour
{
    [Header("Animação")]
    public Animator animator;
    public string nomeDoTrigger = "Abrir"; // nome do parâmetro Trigger no Animator Controller

    [Header("Colisão")]
    public Collider colisorDaPorta; // o Collider que hoje bloqueia o player
    public float tempoParaLiberarColisao = 1f; // duração aproximada da animação, em segundos

    [Header("Áudio (opcional)")]
    public int indiceSomAbrindo = 18; // mesmo índice que já era usado antes de abrir

    private bool jaAbriu = false;

    // Chame esse método de qualquer script (KeyCollectible, EnemyGroup, etc.)
    // em vez de dar Destroy() na porta.
    public void Abrir()
    {
        if (jaAbriu) return;
        jaAbriu = true;

        FindObjectOfType<AudioController>()?.TocarEfeito(indiceSomAbrindo);

        if (animator != null)
            animator.SetTrigger(nomeDoTrigger);

        StartCoroutine(LiberarColisaoAposAnimacao());
    }

    IEnumerator LiberarColisaoAposAnimacao()
    {
        yield return new WaitForSeconds(tempoParaLiberarColisao);

        if (colisorDaPorta != null)
            colisorDaPorta.enabled = false;
    }
}
