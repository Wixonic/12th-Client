using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace API {
	public static class Prefabs {
		public static Dictionary<string, GameObject> dictionnary = new();

		public static void Load() {
			foreach (string guid in AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs" })) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				Prefabs.dictionnary.Add(path, AssetDatabase.LoadAssetAtPath<GameObject>(path));
			}
		}

		public static GameObject Get(string path = "Error") {
			try {
				return Prefabs.dictionnary[$"Assets/Prefabs/{path}.prefab"];
			} catch {
				return Prefabs.dictionnary["Assets/Prefabs/Error.prefab"];
			}
		}
	}
}