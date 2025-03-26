using UnityEngine;

public class Chunk {
	public readonly Vector3 position;
	public readonly List<Block> blocks = new();

	public Chunk(Vector3 position) {
		this.position = position;
		
	}
}