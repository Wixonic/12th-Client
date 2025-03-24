using SmartNbt.Tags;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace API {
	public class ServerConfigPacket : ServerPacket {
		public const State STATE = State.Config;

		public ServerConfigPacket(int ID) : base(ID, STATE) { }
	}

	public class ServerConfigAcknowledgedPacket : ServerConfigPacket {
		public const int ID = 0x03;

		public ServerConfigAcknowledgedPacket() : base(ID) { }
	}

	public class ServerConfigKeepAlivePacket : ServerConfigPacket {
		public const int ID = 0x04;

		public ServerConfigKeepAlivePacket(long keepAliveId) : base(ID) {
			WriteLong(keepAliveId);
		}
	}
	
	public class ServerConfigPongPacket : ServerConfigPacket {
		public const int ID = 0x05;

		public ServerConfigPongPacket(int pingId) : base(ID) {
			WriteInt(pingId);
		}
	}

	public class ServerConfigKnownPacksPacket : ServerConfigPacket {
		public const int ID = 0x07;

		public ServerConfigKnownPacksPacket() : base(ID) {
			WriteVarInt(0);
		}
	}
}