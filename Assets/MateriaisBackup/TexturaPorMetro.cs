using UnityEngine;

// Mantém o tamanho da textura CONSTANTE no mundo, não importa a escala ou a rotação do objeto.
// Vai no cubo (ou plano) da parede/chão. Descobre sozinho qual é a face grande:
//   - objeto fino em Z  -> face da frente/trás (usa X e Y)
//   - objeto fino em X  -> face dos lados      (usa Z e Y)
//   - objeto fino em Y  -> face de cima/baixo  (usa X e Z)
// Cada Renderer ganha o seu próprio Tiling (sem criar cópias do material).
[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class TexturaPorMetro : MonoBehaviour
{
    [Header("Tamanho da textura no mundo")]
    [Tooltip("Quantos metros a imagem inteira cobre antes de repetir. Maior = textura maior.")]
    public float metrosPorRepeticao = 2f;

    [Header("Textura que cobre a parede inteira na altura (lambri, sanca, rodapé)")]
    [Tooltip("Se maior que 0, a imagem NÃO repete na vertical: ela cobre esta altura em metros (use 3 para as texturas de parede com detalhes). Deixe 0 para repetir normalmente.")]
    public float alturaDaTextura = 0f;

    [Tooltip("Quantos metros da parede estão ENTERRADOS no chão. A imagem é encaixada só na parte visível, então o rodapé e o lambri não somem dentro do chão. Ex.: parede de 3 m com 0,5 m abaixo do chão = 0,5.")]
    public float alturaEnterrada = 0f;

    [Tooltip("LIGADO: a textura repete um número INTEIRO de vezes na largura da parede, então os painéis do lambri não ficam cortados pela metade nas pontas (o tamanho muda um pouco).")]
    public bool repeticaoInteira = true;

    [Tooltip("Marque se a textura aparece de cabeça para baixo (rodapé em cima, sanca embaixo)")]
    public bool inverterVertical = false;

    [Header("Deslocamento (opcional)")]
    public Vector2 offset = Vector2.zero;

    [Header("Diagnóstico")]
    [Tooltip("Mostra o tamanho real em metros e qual face foi usada")]
    public Vector3 tamanhoEmMetros;
    public string faceUsada;

    // Tamanho da malha guardado no editor. Usado no jogo/build quando o objeto é "Static" (Static Batching
    // junta as malhas em uma só, e aí o tamanho lido em tempo de execução ficaria errado).
    [SerializeField, HideInInspector] private Vector3 malhaSalva;

    private Renderer rend;
    private MeshFilter filtro;
    private MaterialPropertyBlock bloco;

    static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");   // URP
    static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");   // Built-in

    void OnEnable() { Aplicar(); }
    void OnValidate() { Aplicar(); }

    void Update()
    {
        // No editor, atualiza enquanto você escala o objeto. No jogo roda uma vez só.
        if (!Application.isPlaying)
            Aplicar();
    }

    public void Aplicar()
    {
        if (rend == null) rend = GetComponent<Renderer>();
        if (filtro == null) filtro = GetComponent<MeshFilter>();
        if (rend == null || metrosPorRepeticao <= 0.0001f) return;

        // Tamanho REAL em metros (tamanho da malha x escala). Cubo = 1 m, Plane = 10 m.
        Vector3 malha;
        bool emBatch = Application.isPlaying && rend.isPartOfStaticBatch;
        if (!emBatch && filtro != null && filtro.sharedMesh != null)
        {
            malha = filtro.sharedMesh.bounds.size;
            if (malha != malhaSalva)
            {
                malhaSalva = malha;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
        else
        {
            malha = malhaSalva != Vector3.zero ? malhaSalva : Vector3.one;
        }
        Vector3 s = transform.lossyScale;
        Vector3 t = new Vector3(Mathf.Abs(malha.x * s.x), Mathf.Abs(malha.y * s.y), Mathf.Abs(malha.z * s.z));
        tamanhoEmMetros = t;

        // Eixo mais fino = direção da face grande
        float u, v;
        bool faceDeParede = true;
        if (t.z <= t.x && t.z <= t.y)      { u = t.x; v = t.y; faceUsada = "frente/trás (X, Y)"; }
        else if (t.x <= t.y)               { u = t.z; v = t.y; faceUsada = "lados (Z, Y)"; }
        else                               { u = t.x; v = t.z; faceUsada = "cima/baixo (X, Z)"; faceDeParede = false; }

        if (bloco == null) bloco = new MaterialPropertyBlock();
        rend.GetPropertyBlock(bloco);

        float repV = alturaDaTextura > 0.0001f ? alturaDaTextura : metrosPorRepeticao;
        float tx = u / metrosPorRepeticao;
        if (repeticaoInteira)
            tx = Mathf.Max(1f, Mathf.Round(tx));
        float ty = v / repV;
        float oy = offset.y;

        // Parte da parede está dentro do chão: encaixa a imagem inteira só na parte visível
        if (faceDeParede && alturaEnterrada > 0.0001f && alturaDaTextura > 0.0001f)
        {
            float visivel = Mathf.Max(0.1f, v - alturaEnterrada);
            ty = v / visivel;
            oy += -alturaEnterrada / visivel;
        }

        if (inverterVertical)
        {
            // Tiling negativo vira a imagem; somar |ty| ao offset mantém o mesmo trecho da imagem
            oy += ty;
            ty = -ty;
        }

        Vector4 st = new Vector4(tx, ty, offset.x, oy);
        bloco.SetVector(BaseMapST, st);
        bloco.SetVector(MainTexST, st);

        rend.SetPropertyBlock(bloco);
    }
}
