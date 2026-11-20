using UnityEditor;
using UnityEngine;

// Pasar materiales Standard a URP
public static class CharacterMaterialFixer
{
    static readonly string[] Folders =
    {
        "Assets/Gang members - characters pack",
        "Assets/BodyGuards",
    };

    public static void ConvertCharacterMaterials()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            Debug.LogWarning("No se encontró el shader URP Lit.");
            return;
        }

        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", Folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == lit) continue;

            // Leer propiedades del shader viejo
            Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Texture normal = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            Texture occlusion = mat.HasProperty("_OcclusionMap") ? mat.GetTexture("_OcclusionMap") : null;
            float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.3f;

            mat.shader = lit;
            mat.SetTexture("_BaseMap", mainTex);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", Mathf.Min(smoothness, 0.5f));
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }
            if (occlusion != null)
            {
                mat.SetTexture("_OcclusionMap", occlusion);
                mat.EnableKeyword("_OCCLUSIONMAP");
            }

            EditorUtility.SetDirty(mat);
            count++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Materiales de personajes pasados a URP: {count}.");
    }
}
