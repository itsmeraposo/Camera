#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Menu: GameObject > Hotel > Criar Porta com Marco
// Monta na cena (e salva como prefab) uma porta com marco, folha com painéis, maçanetas,
// HingeJoint e o script PortaHinge. Medidas para o vão de 1,0 x 2,1 m das paredes do projeto.
//
// Este script deve ficar numa pasta chamada "Editor" (Assets/Editor/CriarPorta.cs).
public static class CriarPorta
{
    // >>> MUDE AQUI para o tamanho do vão da sua parede (em metros) <<<
    const float LarguraVao = 1.5f;
    const float AlturaVao = 2.7f;

    [MenuItem("GameObject/Hotel/Criar Porta com Marco", false, 10)]
    static void Criar()
    {
        Material madeira = ObterMaterial("Porta_Madeira", new Color(0.78f, 0.62f, 0.42f), "porta_madeira", 0.25f, 0f);
        Material metal = ObterMaterial("Porta_Metal", new Color(0.75f, 0.70f, 0.55f), null, 0.7f, 0.8f);

        // Posição: no objeto selecionado, se houver, senão na origem
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;
        if (Selection.activeTransform != null)
        {
            pos = Selection.activeTransform.position;
            rot = Selection.activeTransform.rotation;
        }

        GameObject raiz = new GameObject("Porta");
        raiz.transform.SetPositionAndRotation(pos, rot);

        float L = LarguraVao;
        float A = AlturaVao;
        float w = L - 0.10f;    // largura da folha
        float h = A - 0.05f;    // altura da folha

        // ---------- Marco ----------
        GameObject marco = new GameObject("Marco");
        marco.transform.SetParent(raiz.transform, false);

        float hb = A + 0.06f;
        Caixa("Batente esquerdo", marco.transform, new Vector3(-(L / 2f + 0.01f), hb / 2f, 0f), new Vector3(0.08f, hb, 0.24f), madeira, false);
        Caixa("Batente direito", marco.transform, new Vector3(L / 2f + 0.01f, hb / 2f, 0f), new Vector3(0.08f, hb, 0.24f), madeira, false);
        Caixa("Verga", marco.transform, new Vector3(0f, A + 0.03f, 0f), new Vector3(L + 0.10f, 0.06f, 0.24f), madeira, false);

        float hg = A + 0.09f;
        foreach (float z in new float[] { -0.135f, 0.135f })
        {
            Caixa("Guarnicao esquerda", marco.transform, new Vector3(-(L / 2f + 0.085f), hg / 2f, z), new Vector3(0.07f, hg, 0.03f), madeira, false);
            Caixa("Guarnicao direita", marco.transform, new Vector3(L / 2f + 0.085f, hg / 2f, z), new Vector3(0.07f, hg, 0.03f), madeira, false);
            Caixa("Guarnicao topo", marco.transform, new Vector3(0f, hg, z), new Vector3(L + 0.24f, 0.06f, 0.03f), madeira, false);
        }

        // ---------- Dobradiça (pivô na borda esquerda da folha) ----------
        GameObject dobradica = new GameObject("Dobradica");
        dobradica.transform.SetParent(raiz.transform, false);
        dobradica.transform.localPosition = new Vector3(-w / 2f, 0f, 0f);

        Caixa("Folha", dobradica.transform, new Vector3(w / 2f, h / 2f + 0.01f, 0f), new Vector3(w, h, 0.04f), madeira, true);

        foreach (float lado in new float[] { -1f, 1f })
        {
            foreach (float x in new float[] { w * 0.256f, w * 0.744f })
            {
                Caixa("Painel baixo", dobradica.transform, new Vector3(x, h * 0.293f, lado * 0.026f), new Vector3(w * 0.378f, h * 0.39f, 0.012f), madeira, false);
                Caixa("Painel alto", dobradica.transform, new Vector3(x, h * 0.756f, lado * 0.026f), new Vector3(w * 0.378f, h * 0.39f, 0.012f), madeira, false);
            }
            Caixa("Macaneta", dobradica.transform, new Vector3(w - 0.11f, h * 0.49f, lado * 0.045f), new Vector3(0.12f, 0.03f, 0.03f), metal, false);
            Caixa("Macaneta base", dobradica.transform, new Vector3(w - 0.06f, h * 0.49f, lado * 0.032f), new Vector3(0.04f, 0.04f, 0.012f), metal, false);
        }

        // Adiciona o PortaHinge (que traz Rigidbody e HingeJoint junto)
        dobradica.AddComponent<PortaHinge>();

        Undo.RegisterCreatedObjectUndo(raiz, "Criar Porta");

        // Salva como prefab para reaproveitar nos outros quartos
        EnsureFolder("Assets/Hotel");
        string caminho = AssetDatabase.GenerateUniqueAssetPath("Assets/Hotel/Porta.prefab");
        PrefabUtility.SaveAsPrefabAssetAndConnect(raiz, caminho, InteractionMode.UserAction);

        Selection.activeGameObject = raiz;
        Debug.Log("[Hotel] Porta criada e salva em " + caminho + ". Posicione no vão da parede (frente da porta = eixo Z azul).");
    }

    static GameObject Caixa(string nome, Transform pai, Vector3 posLocal, Vector3 tamanho, Material mat, bool comColisor)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nome;
        g.transform.SetParent(pai, false);
        g.transform.localPosition = posLocal;
        g.transform.localScale = tamanho;
        g.GetComponent<MeshRenderer>().sharedMaterial = mat;

        if (!comColisor)
        {
            Collider c = g.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }

        return g;
    }

    static Material ObterMaterial(string nome, Color cor, string texturaNome, float suavidade, float metalico)
    {
        EnsureFolder("Assets/Hotel");
        EnsureFolder("Assets/Hotel/Materiais");
        string caminho = "Assets/Hotel/Materiais/" + nome + ".mat";

        Material existente = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (existente != null)
            return existente;

        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");

        Material mat = new Material(sh);
        mat.name = nome;
        mat.color = cor;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", cor);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", suavidade);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metalico);

        if (!string.IsNullOrEmpty(texturaNome))
        {
            string[] guids = AssetDatabase.FindAssets(texturaNome + " t:Texture2D");
            if (guids.Length > 0)
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
                if (tex != null)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                    mat.mainTexture = tex;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
                    mat.color = Color.white;
                }
            }
        }

        AssetDatabase.CreateAsset(mat, caminho);
        return mat;
    }

    static void EnsureFolder(string caminho)
    {
        if (AssetDatabase.IsValidFolder(caminho)) return;
        int i = caminho.LastIndexOf('/');
        string pai = caminho.Substring(0, i);
        string nome = caminho.Substring(i + 1);
        EnsureFolder(pai);
        AssetDatabase.CreateFolder(pai, nome);
    }
}
#endif
