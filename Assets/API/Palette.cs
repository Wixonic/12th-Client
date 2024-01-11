using System.Collections.Generic;
using UnityEngine;

namespace API {
	public class Palette {
		public Palette(ClientPacket packet, byte bitsPerEntry, bool biomes = false) {
			if (bitsPerEntry == 0) {
				Debug.Log($"Single-valued {(biomes ? "biomes" : "blocks")} palette");
			} else if (bitsPerEntry >= (biomes ? 1 : 4) && bitsPerEntry <= (biomes ? 3 : 8)) {
				Debug.Log($"Indirect {(biomes ? "biomes" : "blocks")} palette");
			} else if (bitsPerEntry >= (biomes ? 6 : 15)) {
				Debug.Log($"Direct {(biomes ? "biomes" : "blocks")} palette");
			}
		}
	}

	public class PaletteContainer {
		public readonly byte bitsPerEntry;
		public readonly Palette palette;
		public readonly int dataArrayLength;
		public readonly List<long> dataArray;

		public PaletteContainer(ClientPacket packet, bool biomes = false) {
			this.bitsPerEntry = packet.ReadByte();
			this.palette = new(packet, this.bitsPerEntry, biomes);
			this.dataArrayLength = packet.ReadVarInt();
			this.dataArray = new();

			Debug.Log($"Bits per entry: {this.bitsPerEntry}");
		}
	}
}