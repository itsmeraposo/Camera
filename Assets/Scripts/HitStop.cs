using System.Collections;
using UnityEngine;

// Congela o jogo por um instante quando o golpe acerta (sensação de impacto).
// Usa uma escala bem baixa (e não 0) para não ser confundido com a pausa do jogo.
public class HitStop : MonoBehaviour
{
    private const float escalaHitStop = 0.05f;

    private static HitStop instancia;

    private Coroutine rotina;
    private bool emHitStop;
    private float escalaAnterior = 1f;

    public static void Executar(float duracao)
    {
        if (duracao <= 0f || Time.timeScale == 0f)
            return;   // desligado ou jogo pausado

        if (instancia == null)
            instancia = new GameObject("HitStop").AddComponent<HitStop>();

        instancia.Iniciar(duracao);
    }

    void Iniciar(float duracao)
    {
        if (rotina != null)
            StopCoroutine(rotina);

        rotina = StartCoroutine(Rotina(duracao));
    }

    IEnumerator Rotina(float duracao)
    {
        if (!emHitStop)
            escalaAnterior = Time.timeScale;

        emHitStop = true;
        Time.timeScale = escalaHitStop;

        yield return new WaitForSecondsRealtime(duracao);

        Restaurar();
        rotina = null;
    }

    // Só devolve a escala se ninguém mexeu nela (ex.: o jogador pausou durante o hit stop)
    void Restaurar()
    {
        if (emHitStop && Mathf.Approximately(Time.timeScale, escalaHitStop))
            Time.timeScale = escalaAnterior;

        emHitStop = false;
    }

    void OnDestroy()
    {
        Restaurar();
        if (instancia == this)
            instancia = null;
    }
}
