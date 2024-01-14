using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace API {
	public class Palette {
		public readonly string mode;
		public readonly string type;

		public readonly int length;
		public readonly List<int> data = new();

		public Palette(ClientPacket packet, byte bitsPerEntry, bool biomes = false) {
			this.type = biomes ? "biome" : "block";

			if (bitsPerEntry == 0) {
				this.mode = "single-valued";
				this.length = packet.ReadVarInt();
			} else if (bitsPerEntry >= (biomes ? 1 : 4) && bitsPerEntry <= (biomes ? 3 : 8)) {
				this.mode = "indirect";
				this.length = packet.ReadVarInt();
				for (int i = 0; i < this.length; ++i) this.data.Add(packet.ReadVarInt());
			} else if (bitsPerEntry >= (biomes ? 6 : 15)) this.mode = "direct";
			else Debug.LogError($"Invalid palette: {(biomes ? "biomes" : "blocks")} - {bitsPerEntry} bits");
		}
	}

	public class PaletteContainer {
		public readonly byte bitsPerEntry;
		public readonly Palette palette;
		public readonly int dataArrayLength;
		public readonly List<List<List<int>>> dataArray;

		public PaletteContainer(ClientPacket packet, bool biomes = false) {
			this.bitsPerEntry = packet.ReadByte();
			this.palette = new(packet, this.bitsPerEntry, biomes);
			this.dataArrayLength = packet.ReadVarInt();
			this.dataArray = new();

			this.dataArrayLength = Mathf.CeilToInt(this.bitsPerEntry * (biomes ? 64 : 4096) / 64); // bitsPerEntry * volume / longSize

			switch (this.palette.mode) {
				case "single-valued":

					break;

				case "indirect":
					List<int> ids = new();

					for (int i = 0; i < this.dataArrayLength; ++i) {
						long container = packet.ReadLong(true);

						for (int j = 0; j < 64 / this.bitsPerEntry; ++j) {
							for (int k = 0; k < this.bitsPerEntry; ++k) ids.Add((int)(container >> (j * this.bitsPerEntry + k) & 1));
						}
					}

					for (int y = 0; y < 16; ++y) {
						this.dataArray.Add(new());

						for (int z = 0; z < 16; ++z) {
							this.dataArray[y].Add(new());

							for (int x = 0; x < 16; ++x) {
								try {
									this.dataArray[y][z].Add(this.palette.data[ids[y * 256 + z * 16 + x]]);
								} catch { }
							}
						}
					}
					break;

				case "direct":

					break;
			}
		}
	}
}