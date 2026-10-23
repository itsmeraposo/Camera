using UnityEngine;

// Tremor curto da câmera. Criado sozinho na Main Camera na primeira vez que é chamado.
// Funciona com a câmera presa ao Player ou seguindo por script (o tremor é aplicado depois).
[DefaultExecutionOrder(1000)]
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private float intensidade;
    private float duracao;
    private float tempoRestante;

    private bool aplicado;
    private Vector3 ultimoOffset;
    private Vector3 ultimaPosicao;

    void Awake()
    {
        Instance = this;
    }

    public static void Tremer(float intensidade, float duracao)
    {
        if (intensidade <= 0f || duracao <= 0f)
            return;

        if (Instance == null)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            cam.gameObject.AddComponent<CameraShake>();   // o Awake define o Instance
        }

        Instance.Iniciar(intensidade, duracao);
    }

    void Iniciar(float novaIntensidade, float novaDuracao)
    {
        float forcaAtual = duracao > 0f ? intensidade * Mathf.Clamp01(tempoRestante / duracao) : 0f;

        intensidade = Mathf.Max(forcaAtual, novaIntensidade);
        duracao = novaDuracao;
        tempoRestante = novaDuracao;
    }

    // Desfaz o tremor do frame anterior (só se ninguém mexeu na câmera nesse meio tempo)
    void Update()
    {
        if (!aplicado)
            return;

        if ((transform.localPosition - ultimaPosicao).sqrMagnitude < 1e-8f)
            transform.localPosition -= ultimoOffset;

        aplicado = false;
    }

    // Depois dos outros scripts da câmera: aplica o tremor
    void LateUpdate()
    {
        if (tempoRestante <= 0f)
            return;

        tempoRestante -= Time.unscaledDeltaTime;   // continua durante o hit stop

        float forca = tempoRestante > 0f ? intensidade * (tempoRestante / duracao) : 0f;
        if (forca <= 0f)
            return;

        Vector3 offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * forca;

        transform.localPosition += offset;
        ultimoOffset = offset;
        ultimaPosicao = transform.localPosition;
        aplicado = true;
    }
}
