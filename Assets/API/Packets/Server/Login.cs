using SmartNbt.Tags;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace API {
	public class ServerLoginPacket : ServerPacket {
		public const State STATE = State.Login;

		public ServerLoginPacket(int ID) : base(ID, STATE) { }
	}

	public class ServerLoginStartPacket : ServerLoginPacket {
		public const int ID = 0x00;

		public ServerLoginStartPacket(string username, Guid? uuid = null) : base(ID) {
			WriteString(username ?? "Test", 16);
			WriteUUID(uuid ?? Guid.NewGuid());
		}
	}

	public class ServerLoginAcknowledgedPacket : ServerLoginPacket {
		public const int ID = 0x03;

		public ServerLoginAcknowledgedPacket() : base(ID) { }
	}
}