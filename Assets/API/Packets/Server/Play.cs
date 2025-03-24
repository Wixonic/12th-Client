using SmartNbt.Tags;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace API {
	public class ServerPlayPacket : ServerPacket {
		public const State STATE = State.Play;

		public ServerPlayPacket(int ID) : base(ID, STATE) { }
	}

	public class ServerPlayConfirmTeleportationPacket : ServerPlayPacket {
		public const int ID = 0x00;

		public ServerPlayConfirmTeleportationPacket(int teleportId) : base(ID) {
			WriteVarInt(teleportId);
		}
	}

	public class ServerPlayChunkBatchReceivedPacket : ServerPlayPacket {
		public const int ID = 0x08;

		public ServerPlayChunkBatchReceivedPacket(float chunkPerTick) : base(ID) {
			WriteFloat(chunkPerTick);
		}
	}

	public class ServerPlayKeepAlivePacket : ServerPlayPacket {
		public const int ID = 0x18;

		public ServerPlayKeepAlivePacket(long keepAliveId) : base(ID) {
			WriteLong(keepAliveId);
		}
	}

	public class ServerPlayPongPacket : ServerPlayPacket {
		public const int ID = 0x27;

		public ServerPlayPongPacket(int pingId) : base(ID) {
			WriteInt(pingId);
		}
	}
}