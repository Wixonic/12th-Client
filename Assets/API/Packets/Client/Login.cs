using SmartNbt.Tags;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace API {
	public class ClientLoginPacket : ClientPacket {
		public const State STATE = State.Login;

		public ClientLoginPacket(byte[] buffer, int ID) : base(buffer, ID, STATE) { }
	}

	public class ClientLoginDisconnectPacket : ClientLoginPacket {
		public const int ID = 0x00;

		public readonly string reason;

		public ClientLoginDisconnectPacket(byte[] buffer) : base(buffer, ID) {
			reason = ReadChat();
		}
	}

	public class ClientLoginSuccessPacket : ClientLoginPacket {
		public const int ID = 0x02;

		public readonly Guid uuid;
		public readonly string username;

		public ClientLoginSuccessPacket(byte[] buffer) : base(buffer, ID) {
			uuid = ReadUUID();
			username = ReadString(16);

			for (int i = 0; i < ReadVarInt(); ++i) {
				string name = ReadString(32767);
				string value = ReadString(32767);
				bool signed = ReadBoolean();
				Debug.Log($"Property n°{i + 1}: Name {name} - Value {value} - {(signed ? "Signed" : "Unsigned")}");
				if (signed) ReadString(32767);
			}

			bool strictErrorHandling = ReadBoolean();
		}
	}
}