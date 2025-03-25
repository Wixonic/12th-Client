using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Prefabs {
    public static Dictionary<string, GameObject> prefabs = new();
    public static Dictionary<string, Material> materials = new();

    public static void Load() {
        foreach (string guid in AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs" })) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            prefabs.Add(path, AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" })) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            materials.Add(path, AssetDatabase.LoadAssetAtPath<Material>(path));
        }
    }

    public static GameObject GetPrefab(string path) {
        try {
            return prefabs[$"Assets/Prefabs/{path}.prefab"];
        } catch {
            Debug.LogError($"Prefab not found: {path}");
            return prefabs["Assets/Prefabs/Error.prefab"];
        }
    }

    public static Material GetMaterial(string materialName) {
        try {
            return materials[$"Assets/Materials/{materialName}.mat"];
        } catch {
            Debug.LogError($"Material not found: {materialName}");
            return materials["Assets/Materials/Error.mat"];
        }
    }
}