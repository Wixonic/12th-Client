using System;

namespace API {
	// Handshaking packet

	public class ServerHandshakePacket : ServerPacket {
		public readonly static int ID = 0x00;
		public readonly static State STATE = State.Handshake;

		public ServerHandshakePacket(int protocolVersion, string ip, ushort? port, State nextState) : base(ID, STATE) {
			this.WriteVarInt(protocolVersion);
			this.WriteString(ip ?? "90.91.236.96", 255);
			this.WriteUShort(port ?? 25565);
			this.WriteVarInt((int)nextState);
		}
	}

	// Login packets

	public class ServerLoginStartPacket : ServerPacket {
		public readonly static int ID = 0x00;
		public readonly static State STATE = State.Login;

		public ServerLoginStartPacket(string username, Guid? uuid = null) : base(ID, STATE) {
			this.WriteString(username ?? "Test", 16);
			if (uuid != null) {
				this.WriteBool(true);
				this.WriteUUID(uuid ?? Guid.NewGuid());
			} else this.WriteBool(false);
		}
	}

	public class ServerEncryptionResponsePacket : ServerPacket {
		public readonly static int ID = 0x01;
		public readonly static State STATE = State.Login;

		public ServerEncryptionResponsePacket(int secretLength, byte[] secret, int tokenLength, byte[] token) : base(ID, STATE) {
			this.WriteVarInt(secretLength);
			this.WriteBytes(secret);
			this.WriteVarInt(tokenLength);
			this.WriteBytes(token);
		}
	}

	public class ServerLoginPluginResponsePacket : ServerPacket {
		public readonly static int ID = 0x02;
		public readonly static State STATE = State.Login;

		public ServerLoginPluginResponsePacket(int messageID, bool successful = false, byte[] data = null) : base(ID, STATE) {
			this.WriteVarInt(messageID);
			this.WriteBool(successful);
			if (successful && data != null) this.WriteBytes(data);
		}
	}

	// Play packets

	public class ServerConfirmTeleportationPacket : ServerPacket {
		public readonly static int ID = 0x00;
		public readonly static State STATE = State.Play;

		public ServerConfirmTeleportationPacket(int teleportId) : base(ID, STATE) {
			this.WriteVarInt(teleportId);
		}
	}

	public class ServerKeepAlivePacket : ServerPacket {
		public readonly static int ID = 0x12;
		public readonly static State STATE = State.Play;

		public ServerKeepAlivePacket(long keepAlive) : base(ID, STATE) {
			this.WriteLong(keepAlive);
		}
	}
}