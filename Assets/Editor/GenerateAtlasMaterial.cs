using UnityEditor;
using UnityEngine;

public class TextureAtlasMaterialGenerator : EditorWindow {
    Texture2D atlasTexture;
    int tilesPerRow = 16;
    int tilesPerColumn = 16;

    [MenuItem("Tools/Generate Atlas Material")]
    static void Init() {
        GetWindow<TextureAtlasMaterialGenerator>("Atlas Material Generator");
    }

    void OnGUI() {
        GUILayout.Label("Paramètres de l'Atlas", EditorStyles.boldLabel);
        atlasTexture = (Texture2D)EditorGUILayout.ObjectField("Texture Atlas", atlasTexture, typeof(Texture2D), false);
        tilesPerRow = EditorGUILayout.IntField("Tuiles par ligne", tilesPerRow);
        tilesPerColumn = EditorGUILayout.IntField("Tuiles par colonne", tilesPerColumn);

        if (GUILayout.Button("Générer le Matériau") && atlasTexture != null) {
            CreateAtlasMaterial();
        }
    }

    void CreateAtlasMaterial() {
        Shader shader = Shader.Find("Standard");
        
        Material material = new Material(shader) {
            name = "BlockAtlasMaterial",
            mainTexture = atlasTexture
        };

        Vector2 tileSize = new Vector2(
            1f / tilesPerRow,
            1f / tilesPerColumn
        );

        material.SetFloat("_Glossiness", 0f);
        material.EnableKeyword("_NORMALMAP");
        material.SetTextureScale("_MainTex", tileSize);

        if (!AssetDatabase.IsValidFolder("Assets/Materials")) {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        AssetDatabase.CreateAsset(material, "Assets/Materials/BlockAtlasMaterial.mat");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(atlasTexture)) as TextureImporter;
        if (importer != null) {
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        Debug.Log("Matériau d'atlas généré avec succès !");
    }
}