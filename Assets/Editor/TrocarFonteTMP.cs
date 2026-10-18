using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public class TrocarFonteTMP : EditorWindow
{
    TMP_FontAsset fonte;

    [MenuItem("Tools/Trocar Fonte TMP")]
    static void Abrir() => GetWindow<TrocarFonteTMP>("Trocar Fonte TMP");

    void OnGUI()
    {
        fonte = (TMP_FontAsset)EditorGUILayout.ObjectField("Font Asset", fonte, typeof(TMP_FontAsset), false);

        using (new EditorGUI.DisabledScope(fonte == null))
        {
            if (GUILayout.Button("Aplicar na cena aberta")) AplicarCena();
            if (GUILayout.Button("Aplicar em todos os prefabs")) AplicarPrefabs();
        }
    }

    void AplicarCena()
    {
        var textos = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in textos)
        {
            Undo.RecordObject(t, "Trocar fonte");
            t.font = fonte;
            EditorUtility.SetDirty(t);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Fonte aplicada em {textos.Length} textos da cena.");
    }

    void AplicarPrefabs()
    {
        int total = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            var textos = root.GetComponentsInChildren<TMP_Text>(true);

            if (textos.Length > 0)
            {
                foreach (var t in textos) t.font = fonte;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                total += textos.Length;
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Fonte aplicada em {total} textos nos prefabs.");
    }
}
