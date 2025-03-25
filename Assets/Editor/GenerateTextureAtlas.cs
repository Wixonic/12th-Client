using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class TextureAtlasGenerator : EditorWindow
{
    private string inputFolder = "Assets/Textures/Blocks/";
    private string outputPath = "Assets/Textures/Atlas/BlockAtlas.png";
    private int tileSize = 64;
    private int tilesPerRow = 16;
    private Texture2D atlasTexture;
    private Dictionary<string, Rect> uvCoordinates = new Dictionary<string, Rect>();

    [MenuItem("Tools/Generate Texture Atlas")]
    public static void ShowWindow()
    {
        GetWindow<TextureAtlasGenerator>("Texture Atlas Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Texture Atlas Settings", EditorStyles.boldLabel);
        inputFolder = EditorGUILayout.TextField("Input Folder", inputFolder);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        tileSize = EditorGUILayout.IntField("Tile Size", tileSize);
        tilesPerRow = EditorGUILayout.IntField("Tiles Per Row", tilesPerRow);

        if (GUILayout.Button("Generate Atlas"))
        {
            GenerateAtlas();
        }
    }

    void GenerateAtlas()
    {
        // Récupère toutes les textures
        string[] texturePaths = Directory.GetFiles(inputFolder, "*.png");
        List<Texture2D> textures = new List<Texture2D>();

        foreach (string path in texturePaths)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                textures.Add(tex);
            }
        }

        // Calcule la taille de l'atlas
        int totalTiles = textures.Count;
        int rows = Mathf.CeilToInt((float)totalTiles / tilesPerRow);
        int atlasWidth = Math.Max(tileSize * tilesPerRow, 16);
        int atlasHeight = Math.Max(tileSize * rows, 16);

        // Crée la texture atlas
        atlasTexture = new Texture2D(atlasWidth, atlasHeight, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        // Remplit l'atlas
        for (int i = 0; i < textures.Count; i++)
        {
            int row = i / tilesPerRow;
            int col = i % tilesPerRow;

            // Redimensionne la texture si nécessaire
            Texture2D resized = ResizeTexture(textures[i], tileSize, tileSize);
            
            // Copie les pixels
            atlasTexture.SetPixels(
                col * tileSize,
                row * tileSize,
                tileSize,
                tileSize,
                resized.GetPixels()
            );

            // Enregistre les UV
            string textureName = Path.GetFileNameWithoutExtension(texturePaths[i]);
            uvCoordinates[textureName] = new Rect(
                (float)col / tilesPerRow,
                1f - ((float)(row + 1) / rows),
                (float)tileSize / atlasWidth,
                (float)tileSize / atlasHeight
            );
        }

        // Applique les changements
        atlasTexture.Apply();

        // Sauvegarde le fichier
        byte[] bytes = atlasTexture.EncodeToPNG();
        File.WriteAllBytes(outputPath, bytes);
        AssetDatabase.Refresh();

        // Met à jour les paramètres d'import
        TextureImporter importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        // Crée les données UV
        CreateUVData();

        Debug.Log($"Atlas généré avec succès! {textures.Count} textures combinées.");
    }

    Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        
        Texture2D resized = new Texture2D(newWidth, newHeight);
        resized.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        resized.Apply();
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return resized;
    }

    void CreateUVData()
    {
        UVData uvData = ScriptableObject.CreateInstance<UVData>();
        uvData.uvCoordinates = uvCoordinates;

        if (!Directory.Exists("Assets/Data"))
            Directory.CreateDirectory("Assets/Data");

        AssetDatabase.CreateAsset(uvData, "Assets/Data/UVData.asset");
        AssetDatabase.SaveAssets();
    }
}

public class UVData : ScriptableObject
{
    public Dictionary<string, Rect> uvCoordinates = new Dictionary<string, Rect>();
}