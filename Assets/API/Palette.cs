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
			type = biomes ? "biome" : "block";

			if (bitsPerEntry == 0) {
				mode = "single-valued";
				length = packet.ReadVarInt();
			} else if (bitsPerEntry >= (biomes ? 1 : 4) && bitsPerEntry <= (biomes ? 3 : 8)) {
				mode = "indirect";
				length = packet.ReadVarInt();
				for (int i = 0; i < length; ++i) data.Add(packet.ReadVarInt());
			} else if (bitsPerEntry >= (biomes ? 6 : 15)) mode = "direct";
			else Debug.LogError($"Invalid palette: {(biomes ? "biomes" : "blocks")} - {bitsPerEntry} bits");
		}
	}

	public class PaletteContainer {
		public readonly byte bitsPerEntry;
		public readonly Palette palette;
		public readonly int dataArrayLength;
		public readonly List<List<List<int>>> dataArray;

		public PaletteContainer(ClientPacket packet, bool biomes = false) {
			bitsPerEntry = packet.ReadByte();
			palette = new(packet, bitsPerEntry, biomes);
			dataArrayLength = packet.ReadVarInt();
			dataArray = new();

			dataArrayLength = Mathf.CeilToInt(bitsPerEntry * (biomes ? 64 : 4096) / 64); // bitsPerEntry * volume / longSize

			switch (palette.mode) {
				case "single-valued":

					break;

				case "indirect":
					List<int> ids = new();

					for (int i = 0; i < dataArrayLength; ++i) {
						long container = packet.ReadLong(true);

						for (int j = 0; j < 64 / bitsPerEntry; ++j) {
							for (int k = 0; k < bitsPerEntry; ++k) ids.Add((int)(container >> (j * bitsPerEntry + k) & 1));
						}
					}

					for (int y = 0; y < 16; ++y) {
						dataArray.Add(new());

						for (int z = 0; z < 16; ++z) {
							dataArray[y].Add(new());

							for (int x = 0; x < 16; ++x) {
								try {
									dataArray[y][z].Add(palette.data[ids[y * 256 + z * 16 + x]]);
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