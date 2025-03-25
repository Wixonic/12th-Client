using SmartNbt.Tags;
using SmartNbt.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace API {
	public class ClientPlayPacket : ClientPacket {
		public const State STATE = State.Play;

		public ClientPlayPacket(byte[] buffer, int ID) : base(buffer, ID, STATE) { }
	}

	public class ClientPlayChunkBatchFinishedPacket : ClientPlayPacket {
		public const int ID = 0x0C;

		public readonly int batchSize;

		public ClientPlayChunkBatchFinishedPacket(byte[] buffer) : base(buffer, ID) {
			batchSize = ReadVarInt();
		}
	}

	public class ClientPlayDisconnectPacket : ClientPlayPacket {
		public const int ID = 0x1D;

		public readonly NbtCompound reason;

		public ClientPlayDisconnectPacket(byte[] buffer) : base(buffer, ID) {
			reason = ReadNBT();
		}
	}

	public class ClientPlayChangeDifficultyPacket : ClientPlayPacket {
		public const int ID = 0x0B;

		public readonly byte difficulty;
		public readonly bool locked;

		public ClientPlayChangeDifficultyPacket(byte[] buffer) : base(buffer, ID) {
			difficulty = ReadByte();
			locked = ReadBoolean();
		}	
	}

	public class ClientPlayKeepAlivePacket : ClientPlayPacket {
		public const int ID = 0x26;

		public readonly long keepAliveId;

		public ClientPlayKeepAlivePacket(byte[] buffer) : base(buffer, ID) {
			keepAliveId = ReadLong();
		}
	}

	public class ClientPlayChunkDataPacket : ClientPlayPacket {
		public const int ID = 0x27;

		public readonly int chunkX;
		public readonly int chunkZ;
		public readonly NbtCompound heightmaps;
		public readonly List<List<List<List<int>>>> column = new();
		public readonly List<Tuple<Vector3, int, NbtCompound>> blockEntities = new();

		public ClientPlayChunkDataPacket(byte[] buffer) : base(buffer, ID) {
			chunkX = ReadInt();
			chunkZ = ReadInt();

			try {
				heightmaps = ReadNBT();
			} catch (Exception e) {
				Debug.LogError($"[Chunk] Heightmaps NBT error: {e.Message}");
				heightmaps = new NbtCompound("heightmaps");
			}

			int dataLength = ReadVarInt();
			long dataEnd = this.buffer.Position + dataLength;

			if (World.current.registries.TryGetValue("minecraft:dimension_type", out var registry)) {
				if (registry.TryGetValue(World.current.dimensionName, out var dimensionEntry)) {
					int height = dimensionEntry["height"].IntValue;
					int sectionCount = height / 16;

					column = new List<List<List<List<int>>>>(sectionCount);

					for (int sectionY = 0; sectionY < sectionCount; sectionY++) {
						short blockCount = ReadShort();
						column.Add(ReadPalette());
						ReadPalette(isBiome: true);
					}
				} else Debug.LogError($"[Chunk] Dimension entry missing");
			} else Debug.LogError($"[Chunk] Dimension registry missing");

			this.buffer.Position = dataEnd;

			int blockEntityCount = ReadVarInt();
			blockEntities = new List<Tuple<Vector3, int, NbtCompound>>(blockEntityCount);
			for (int i = 0; i < blockEntityCount; i++) {
				byte packedXZ = ReadByte();
				short yPos = ReadShort();
				int type = ReadVarInt();
				
				try {
					Vector3 pos = new Vector3(
						chunkX * 16 + ((packedXZ >> 4) & 0x0F),
						yPos,
						chunkZ * 16 + (packedXZ & 0x0F)
					);
					NbtCompound data = ReadNBT();
					blockEntities.Add(Tuple.Create(pos, type, data));
				} catch (Exception e) {
					Debug.LogError($"[Chunk] Block entity error: {e.Message}");
				}
			}
		}

		private List<List<List<int>>> ReadPalette(bool isBiome = false) {
			int bitsPerBlock = ReadByte();
			int paletteSize = ReadVarInt();
			List<int> palette = new List<int>();

			if (bitsPerBlock == 0) {
				if (paletteSize > 0) palette.Add(ReadVarInt());
				else palette.Add(0);
			} else {
				for (int i = 0; i < paletteSize; i++) palette.Add(ReadVarInt());
				if (palette.Count == 0) palette.Add(0);
			}

			int ySize = isBiome ? 4 : 16;
			int zSize = isBiome ? 4 : 16;
			int xSize = isBiome ? 4 : 16;

			List<List<List<int>>> sectionData = new List<List<List<int>>>();

			if (bitsPerBlock != 0) {
				long[] blockStates = ReadLongArray();
				int valuesPerLong = 64 / bitsPerBlock;

				for (int y = 0; y < ySize; y++) {
					List<List<int>> yLayer = new List<List<int>>();
					for (int z = 0; z < zSize; z++) {
						List<int> zRow = new List<int>();
						for (int x = 0; x < xSize; x++) {
							int index = y * (zSize * xSize) + z * xSize + x;
							int longIdx = index / valuesPerLong;
							int bitOffset = (index % valuesPerLong) * bitsPerBlock;
							int state = 0;

							if (longIdx < blockStates.Length) {
								state = (int)((blockStates[longIdx] >> bitOffset) & ((1 << bitsPerBlock) - 1));
								if (state < 0 || state >= palette.Count) state = 0;
							}

							zRow.Add(palette[state]);
						}

						yLayer.Add(zRow);
					}

					sectionData.Add(yLayer);
				}
			} else {
				int value = palette.Count > 0 ? palette[0] : 0;
				for (int y = 0; y < ySize; y++) {
					List<List<int>> yLayer = new List<List<int>>();
					
					for (int z = 0; z < zSize; z++) {
						List<int> zRow = Enumerable.Repeat(value, xSize).ToList();
						yLayer.Add(zRow);
					}

					sectionData.Add(yLayer);
				}
			}

			return sectionData;
		}
	}

	public class ClientPlayLoginPacket : ClientPlayPacket {
		public static readonly int ID = 0x2B;

		public readonly int entityId;
		public readonly bool isHardcore;
		public readonly int dimensionCount;
		public readonly string[] dimensionNames;
		public readonly int maxPlayers;
		public readonly int viewDistance;
		public readonly int simulationDistance;
		public readonly bool reducedDebugInfo;
		public readonly bool enableRespawnScreen;
		public readonly bool doLimitedCrafting;
		public readonly int dimensionType;
		public readonly string dimensionName;
		public readonly long hashedSeed;
		public readonly byte gameMode;
		public readonly sbyte previousGameMode;
		public readonly bool isDebug;
		public readonly bool isFlat;
		public readonly bool hasDeathLocation;
		public readonly string? deathDimensionName;
		public readonly Vector3Int? deathLocation;
		public readonly int portalCooldown;
		public readonly bool enforcesSecureChat;

		public ClientPlayLoginPacket(byte[] buffer) : base(buffer, ID) {
			entityId = ReadInt();
			isHardcore = ReadBoolean();
			dimensionCount = ReadVarInt();
			dimensionNames = new string[dimensionCount];
			for (int x = 0; x < dimensionCount; x++) dimensionNames[x] = ReadIdentifier();
			maxPlayers = ReadVarInt();
			viewDistance = ReadVarInt();
			simulationDistance = ReadVarInt();
			reducedDebugInfo = ReadBoolean();
			enableRespawnScreen = ReadBoolean();
			doLimitedCrafting = ReadBoolean();
			dimensionType = ReadVarInt();
			dimensionName = ReadIdentifier();
			hashedSeed = ReadLong();
			gameMode = ReadByte();
			previousGameMode = ReadSByte();
			isDebug = ReadBoolean();
			isFlat = ReadBoolean();
			hasDeathLocation = ReadBoolean();
			if (hasDeathLocation) {
				deathDimensionName = ReadIdentifier();
				deathLocation = ReadPosition();
			} else {
				deathDimensionName = null;
				deathLocation = null;
			}
			portalCooldown = ReadVarInt();
			enforcesSecureChat = ReadBoolean();
		}
	}

	public class ClientPlayPingPacket : ClientConfigPacket {
		public const int ID = 0x35;

		public readonly int pingId;

		public ClientPlayPingPacket(byte[] buffer) : base(buffer, ID) {
			pingId = ReadInt();
		}
	}

	public class ClientPlaySynchronizePositionPacket : ClientPlayPacket {
		public const int ID = 0x40;

		public readonly Vector3 playerPosition;
		public readonly Quaternion playerRotation;
		public readonly bool absolute;
		public readonly int teleportId;

		public ClientPlaySynchronizePositionPacket(byte[] buffer) : base(buffer, ID) {
			float x = (float)ReadDouble();
			float y = (float)ReadDouble();
			float z = (float)ReadDouble();

			float ry = -ReadFloat();
			float rx = ReadFloat();

			byte flag = ReadByte();

			teleportId = ReadVarInt();

			if ((flag & 0x01) == 1) playerPosition.x += x;
			else playerPosition.x = x;

			if ((flag & 0x02) == 1) playerPosition.y += y;
			else playerPosition.y = y;

			if ((flag & 0x04) == 1) playerPosition.z += z;
			else playerPosition.z = z;

			if ((flag & 0x08) == 1) playerRotation = Quaternion.Euler(playerRotation.eulerAngles.x, playerRotation.eulerAngles.y + ry, playerRotation.eulerAngles.z);
			else playerRotation = Quaternion.Euler(playerRotation.eulerAngles.x, ry, playerRotation.eulerAngles.z);

			if ((flag & 0x10) == 1) playerRotation = Quaternion.Euler(playerRotation.eulerAngles.x + rx, playerRotation.eulerAngles.y, playerRotation.eulerAngles.z);
			else playerRotation = Quaternion.Euler(rx, playerRotation.eulerAngles.y, playerRotation.eulerAngles.z);
		}
	}

	public class ClientPlayRespawnPacket : ClientPlayPacket {
		public const int ID = 0x47;

		public readonly int dimensionType;
		public readonly string dimensionName;
		public readonly long hashedSeed;
		public readonly byte gamemode;
		public readonly sbyte previousGamemode;
		public readonly bool isDebug;
		public readonly bool isFlat;
		public readonly Tuple<string, Vector3Int> death;
		public readonly byte dataKept;
		public readonly int portalCooldown;

		public ClientPlayRespawnPacket(byte[] buffer) : base(buffer, ID) {
			dimensionType = ReadVarInt();
			dimensionName = ReadIdentifier();
			hashedSeed = ReadLong();
			gamemode = ReadByte();
			previousGamemode = ReadSByte();
			isDebug = ReadBoolean();
			isFlat = ReadBoolean();
			if (ReadBoolean()) death = new(ReadIdentifier(), ReadPosition());
			else death = null;
			portalCooldown = ReadVarInt();
			dataKept = ReadByte();
		}
	}

	public class ClientPlayUpdateTimePacket : ClientPlayPacket {
		public const int ID = 0x64;

		public readonly long worldAge;
		public readonly long time;

		public ClientPlayUpdateTimePacket(byte[] buffer) : base(buffer, ID) {
			worldAge = ReadLong();
			time = ReadLong();
		}
	}
}