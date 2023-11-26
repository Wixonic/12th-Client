using SmartNbt.Tags;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace API {
	// Disconnected

	public class ClientDisconnectPacket : ClientPacket {
		public readonly string reason;

		public ClientDisconnectPacket(byte[] buffer, int id, State state) : base(buffer, id, state) {
			this.reason = this.ReadChat();
		}
	}

	public class ClientDisconnectLoginPacket : ClientDisconnectPacket {
		public readonly static int ID = 0x00;
		public readonly static State STATE = State.Login;

		public ClientDisconnectLoginPacket(byte[] buffer) : base(buffer, ID, STATE) { }
	}

	public class ClientDisconnectPlayPacket : ClientDisconnectPacket {
		public readonly static int ID = 0x1A;
		public readonly static State STATE = State.Play;

		public ClientDisconnectPlayPacket(byte[] buffer) : base(buffer, ID, STATE) { }
	}

	// Login packets

	public class ClientEncryptionRequestPacket : ClientPacket {
		public readonly static int ID = 0x01;
		public readonly static State STATE = State.Login;

		public readonly string serverId; // Appears to be empty
		public readonly byte[] key;
		public readonly byte[] token;

		public ClientEncryptionRequestPacket(byte[] buffer) : base(buffer, ID, STATE) {
			this.serverId = this.ReadString(20);
			this.key = this.ReadBytes(this.ReadVarInt());
			this.token = this.ReadBytes(this.ReadVarInt());
		}
	}

	public class ClientLoginSuccessPacket : ClientPacket {
		public readonly static int ID = 0x02;
		public readonly static State STATE = State.Login;

		public readonly Guid uuid;
		public readonly string username;

		public ClientLoginSuccessPacket(byte[] buffer) : base(buffer, ID, STATE) {
			this.uuid = this.ReadUUID();
			this.username = this.ReadString(16);

			for (int i = 0; i < this.ReadVarInt(); ++i) {
				bool signed = this.ReadBoolean();
				Debug.Log($"Property n°{i + 1}: Name {this.ReadString(32767)} - Value {this.ReadString(32767)} - {(signed ? "Signed" : "Unsigned")}");
				if (signed) this.ReadString(32767);
			}
		}
	}

	public class ClientSetCompressionPacket : ClientPacket {
		public readonly static int ID = 0x03;
		public readonly static State STATE = State.Login;

		public readonly int threshold;

		public ClientSetCompressionPacket(byte[] buffer) : base(buffer, ID, STATE) {
			this.threshold = this.ReadVarInt();
		}
	}

	public class ClientLoginPluginRequestPacket : ClientPacket {
		public readonly static int ID = 0x04;
		public readonly static State STATE = State.Login;

		public readonly int messageId;
		public readonly string channel;
		public readonly byte[] data;

		public ClientLoginPluginRequestPacket(byte[] buffer) : base(buffer, ID, STATE) {
			this.messageId = this.ReadVarInt();
			this.channel = this.ReadIdentifier();
			this.data = this.ReadBytes((int)(this.buffer.Length - this.buffer.Position));
		}
	}

	// Between Login and Play
	public class ClientLoginPlayPacket : ClientPacket {
		public readonly static int ID = 0x28;
		public readonly static State STATE = State.Login;

		public readonly int entityId;
		public readonly bool isHardcore;
		public readonly byte gamemode;
		public readonly byte previousGamemode;
		public readonly List<string> dimensions = new();
		public readonly NbtCompound registeryCodec;
		public readonly string dimensionType;
		public readonly string dimensionName;
		public readonly long hashedSeed;
		public readonly int maxPlayers;
		public readonly int viewDistance;
		public readonly int simulationDistance;
		public readonly bool reducedDebugInfo;
		public readonly bool enableRespawnScreen;
		public readonly bool isDebug;
		public readonly bool isFlat;
		public readonly Tuple<string, Vector3Int> death;
		public readonly int portalCooldown;

		public ClientLoginPlayPacket(byte[] buffer) : base(buffer, ID, STATE) {
			this.entityId = this.ReadInt();
			this.isHardcore = this.ReadBoolean();
			this.gamemode = this.ReadByte();
			this.previousGamemode = this.ReadByte();
			for (int i = 0; i < this.ReadVarInt(); ++i) this.dimensions.Add(this.ReadIdentifier());
			this.registeryCodec = this.ReadNBT();
			this.dimensionType = this.ReadIdentifier();
			this.dimensionName = this.ReadIdentifier();
			this.hashedSeed = this.ReadLong();
			this.maxPlayers = this.ReadVarInt();
			this.viewDistance = this.ReadVarInt();
			this.simulationDistance = this.ReadVarInt();
			this.reducedDebugInfo = this.ReadBoolean();
			this.enableRespawnScreen = this.ReadBoolean();
			this.isDebug = this.ReadBoolean();
			this.isFlat = this.ReadBoolean();
			if (this.ReadBoolean()) this.death = new(this.ReadString(), this.ReadPosition());
			else this.death = null;
			this.portalCooldown = this.ReadVarInt();
		}
	}

	// Play packets

	public class ClientKeepAlivePacket : ClientPacket {
		public readonly static int ID = 0x23;
		public readonly static State STATE = State.Play;

		public readonly long keepAlive;

		public ClientKeepAlivePacket(byte[] buffer) : base(buffer, ID, STATE) {
			this.keepAlive = this.ReadLong();
		}
	}

	public class ClientChunkDataPacket : ClientPacket {
		public static int sectionCount = 24;

		public readonly static int ID = 0x24;
		public readonly static State STATE = State.Play;

		public readonly int chunkX;
		public readonly int chunkZ;
		public readonly NbtCompound heightmaps;
		public readonly List<Tuple<Vector3, int, NbtCompound>> blockEntities = new();

		public ClientChunkDataPacket(byte[] buffer) : base(buffer, ID, STATE) {
			this.chunkX = this.ReadInt();
			this.chunkZ = this.ReadInt();
			this.heightmaps = this.ReadNBT();

			int dataLength = this.ReadVarInt();
			long start = this.buffer.Position;

			for (int i = 0; i < 1/* ClientChunkDataPacket.sectionCount */; ++i) {
				short blockCount = this.ReadShort();
				Debug.Log($"Block count: {blockCount}");

				// Block states
				byte bitsPerEntry = this.ReadByte();
				Debug.Log($"Bits per entry: {bitsPerEntry}");

				// Biomes
			}

			this.buffer.Position = start + dataLength;

			for (int i = 0; i < this.ReadVarInt(); ++i) {
				byte packedXZ = this.ReadByte();
				short y = this.ReadShort();
				int type = this.ReadVarInt();
				NbtCompound blockEntityData = this.ReadNBT();

				this.blockEntities.Add(new(new(packedXZ >> 4, y, packedXZ & 15), type, blockEntityData));
			}
		}
	}

	public class ClientSynchronizePositionPacket : ClientPacket {
		public readonly static int ID = 0x3C;
		public readonly static State STATE = State.Play;

		public Vector3 playerPosition;
		public Quaternion playerRotation;
		public bool absolute;
		public int teleportId;

		public ClientSynchronizePositionPacket(byte[] buffer) : base(buffer, ID, STATE) {
			float x = (float)this.ReadDouble();
			float y = (float)this.ReadDouble();
			float z = (float)this.ReadDouble();

			float ry = -this.ReadFloat();
			float rx = this.ReadFloat();

			byte flag = this.ReadByte();

			this.teleportId = this.ReadVarInt();

			if ((flag & 0x01) == 1) this.playerPosition.x += x;
			else this.playerPosition.x = x;

			if ((flag & 0x02) == 1) this.playerPosition.y += y;
			else this.playerPosition.y = y;

			if ((flag & 0x04) == 1) this.playerPosition.z += z;
			else this.playerPosition.z = z;

			if ((flag & 0x08) == 1) this.playerRotation = Quaternion.Euler(this.playerRotation.eulerAngles.x, this.playerRotation.eulerAngles.y + ry, this.playerRotation.eulerAngles.z);
			else this.playerRotation = Quaternion.Euler(this.playerRotation.eulerAngles.x, ry, this.playerRotation.eulerAngles.z);

			if ((flag & 0x10) == 1) this.playerRotation = Quaternion.Euler(this.playerRotation.eulerAngles.x + rx, this.playerRotation.eulerAngles.y, this.playerRotation.eulerAngles.z);
			else this.playerRotation = Quaternion.Euler(rx, this.playerRotation.eulerAngles.y, this.playerRotation.eulerAngles.z);
		}
	}

	public class ClientUpdateTimePacket : ClientPacket {
		public readonly static int ID = 0x5E;
		public readonly static State STATE = State.Play;

		public readonly long worldAge;
		public readonly long time;

		public ClientUpdateTimePacket(byte[] buffer) : base(buffer, ID, STATE) {
			this.worldAge = this.ReadLong();
			this.time = this.ReadLong();
		}
	}
}