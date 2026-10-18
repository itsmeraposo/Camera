using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class TrocarSpriteBotoes : EditorWindow
{
    Sprite sprite;

    [MenuItem("Tools/Trocar Sprite dos Botões")]
    static void Abrir() => GetWindow<TrocarSpriteBotoes>("Sprite dos Botões");

    void OnGUI()
    {
        sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", sprite, typeof(Sprite), false);

        using (new EditorGUI.DisabledScope(sprite == null))
        {
            if (GUILayout.Button("Aplicar na cena aberta")) AplicarCena();
            if (GUILayout.Button("Aplicar em todos os prefabs")) AplicarPrefabs();
        }
    }

    // Aplica o sprite no Image de um Button. Retorna true se alterou.
    bool Aplicar(Button botao)
    {
        var img = botao.targetGraphic as Image;
        if (img == null) img = botao.GetComponent<Image>();
        if (img == null) return false;

        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        EditorUtility.SetDirty(img);
        return true;
    }

    void AplicarCena()
    {
        var botoes = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int total = 0;
        foreach (var b in botoes)
        {
            var img = b.targetGraphic as Image;
            if (img == null) img = b.GetComponent<Image>();
            if (img != null) Undo.RecordObject(img, "Trocar sprite do botão");
            if (Aplicar(b)) total++;
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Sprite aplicado em {total} botões da cena.");
    }

    void AplicarPrefabs()
    {
        int total = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            var botoes = root.GetComponentsInChildren<Button>(true);

            int alterados = 0;
            foreach (var b in botoes)
                if (Aplicar(b)) alterados++;

            if (alterados > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                total += alterados;
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Sprite aplicado em {total} botões nos prefabs.");
    }
}
