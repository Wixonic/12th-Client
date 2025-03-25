using System.Collections.Generic;

public static class Registries {
	public static Dictionary<int, string> blocks = new() {
		{-1, "Error"},
		{0, "Air"},
		{1, "Stone"}
	};
}