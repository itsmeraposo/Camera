using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class EstiloSlidersMinas : EditorWindow
{
    Sprite fundo, preenchimento, alca;

    // Tamanhos em unidades do Canvas
    const float AlturaTrilho = 24f;
    const float TamanhoAlca = 44f;

    [MenuItem("Tools/Estilo dos Sliders (Minas)")]
    static void Abrir() => GetWindow<EstiloSlidersMinas>("Estilo dos Sliders");

    void OnEnable()
    {
        if (fundo == null) fundo = Buscar("slider_fundo", 24);
        if (preenchimento == null) preenchimento = Buscar("slider_fill", 16);
        if (alca == null) alca = Buscar("slider_handle", 0);
    }

    void OnGUI()
    {
        fundo = (Sprite)EditorGUILayout.ObjectField("Fundo (trilho)", fundo, typeof(Sprite), false);
        preenchimento = (Sprite)EditorGUILayout.ObjectField("Preenchimento", preenchimento, typeof(Sprite), false);
        alca = (Sprite)EditorGUILayout.ObjectField("Alça", alca, typeof(Sprite), false);

        using (new EditorGUI.DisabledScope(fundo == null || preenchimento == null || alca == null))
        {
            if (GUILayout.Button("Aplicar na cena aberta")) AplicarCena();
            if (GUILayout.Button("Aplicar em todos os prefabs")) AplicarPrefabs();
        }
    }

    // Procura o PNG pelo nome e configura como Sprite (com 9-slice se borda > 0)
    static Sprite Buscar(string nome, int borda)
    {
        foreach (var guid in AssetDatabase.FindAssets(nome + " t:Texture2D"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(nome + ".png")) continue;

            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                var b = new Vector4(borda, borda, borda, borda);
                if (imp.textureType != TextureImporterType.Sprite || imp.spriteBorder != b)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.spriteBorder = b;
                    imp.alphaIsTransparency = true;
                    imp.mipmapEnabled = false;
                    imp.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }

    bool Aplicar(Slider s)
    {
        bool mudou = false;

        // Trilho (objeto filho "Background")
        var bg = s.transform.Find("Background");
        if (bg != null)
        {
            var img = bg.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = fundo;
                img.type = Image.Type.Sliced;
                img.color = Color.white;

                var rt = bg.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(1, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0, AlturaTrilho);
                EditorUtility.SetDirty(img);
                EditorUtility.SetDirty(rt);
                mudou = true;
            }
        }

        // Preenchimento
        if (s.fillRect != null)
        {
            var img = s.fillRect.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = preenchimento;
                img.type = Image.Type.Sliced;
                img.color = Color.white;

                // Área do preenchimento: mesma margem da alça
                var area = s.fillRect.parent as RectTransform;
                if (area != null)
                {
                    area.anchorMin = new Vector2(0, 0.5f);
                    area.anchorMax = new Vector2(1, 0.5f);
                    area.pivot = new Vector2(0.5f, 0.5f);
                    area.anchoredPosition = Vector2.zero;
                    area.sizeDelta = new Vector2(-TamanhoAlca, AlturaTrilho - 8f);
                    EditorUtility.SetDirty(area);
                }

                // Estende o preenchimento até a borda esquerda do trilho
                s.fillRect.offsetMin = new Vector2(-TamanhoAlca / 2f, 0);
                s.fillRect.offsetMax = new Vector2(0, 0);
                EditorUtility.SetDirty(img);
                EditorUtility.SetDirty(s.fillRect);
                mudou = true;
            }
        }

        // Alça
        if (s.handleRect != null)
        {
            var img = s.handleRect.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = alca;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.color = Color.white;

                var area = s.handleRect.parent as RectTransform;
                if (area != null)
                {
                    area.offsetMin = new Vector2(TamanhoAlca / 2f, 0);
                    area.offsetMax = new Vector2(-TamanhoAlca / 2f, 0);
                    EditorUtility.SetDirty(area);
                }

                s.handleRect.anchorMin = new Vector2(0, 0.5f);
                s.handleRect.anchorMax = new Vector2(0, 0.5f);
                s.handleRect.pivot = new Vector2(0.5f, 0.5f);
                s.handleRect.sizeDelta = new Vector2(TamanhoAlca, TamanhoAlca);
                s.handleRect.anchoredPosition = Vector2.zero;
                EditorUtility.SetDirty(img);
                EditorUtility.SetDirty(s.handleRect);
                mudou = true;
            }
        }

        if (mudou)
        {
            s.targetGraphic = s.handleRect != null ? s.handleRect.GetComponent<Image>() : s.targetGraphic;
            s.SetValueWithoutNotify(s.value); // reposiciona preenchimento e alça
            EditorUtility.SetDirty(s);
        }
        return mudou;
    }

    void AplicarCena()
    {
        var sliders = FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int total = 0;
        foreach (var s in sliders)
        {
            Undo.RegisterFullObjectHierarchyUndo(s.gameObject, "Estilo do slider");
            if (Aplicar(s)) total++;
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Estilo aplicado em {total} sliders da cena.");
    }

    void AplicarPrefabs()
    {
        int total = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            var sliders = root.GetComponentsInChildren<Slider>(true);

            int alterados = 0;
            foreach (var s in sliders)
                if (Aplicar(s)) alterados++;

            if (alterados > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                total += alterados;
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Estilo aplicado em {total} sliders nos prefabs.");
    }
}
