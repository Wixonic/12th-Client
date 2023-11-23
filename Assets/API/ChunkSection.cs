using System.IO;
using UnityEngine;

namespace API {
	public class ChunkSection {
		public static readonly int AmountInChunk = 24;
		public ChunkSection(MemoryStream data) {
			Debug.Log($"Recieved chunk section ({data.Length} bytes)");
		}
	}
}