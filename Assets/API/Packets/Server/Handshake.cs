using SmartNbt.Tags;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace API {
	public class ServerHandshakePacket : ServerPacket {
		public const int ID = 0x00;
		public const State STATE = State.Handshake;

		public ServerHandshakePacket(int protocolVersion, string ip, ushort? port, State nextState) : base(ID, STATE) {
			WriteVarInt(protocolVersion);
			WriteString(ip ?? "90.91.236.96", 255);
			WriteUShort(port ?? 25565);
			WriteVarInt((int)nextState);
		}
	}
}