using SmartNbt.Tags;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace API {
	public class ClientConfigPacket : ClientPacket {
		public const State STATE = State.Config;

		public ClientConfigPacket(byte[] buffer, int ID) : base(buffer, ID, STATE) { }
	}

	public class ClientConfigDisconnectPacket : ClientConfigPacket {
		public const int ID = 0x02;

		public readonly NbtCompound reason;

		public ClientConfigDisconnectPacket(byte[] buffer) : base(buffer, ID) {
			reason = ReadNBT();
		}
	}

	public class ClientConfigFinishPacket : ClientConfigPacket {
		public const int ID = 0x03;

		public ClientConfigFinishPacket(byte[] buffer) : base(buffer, ID) { }
	}

	public class ClientConfigKeepAlivePacket : ClientConfigPacket {
		public const int ID = 0x04;

		public readonly long keepAliveId;

		public ClientConfigKeepAlivePacket(byte[] buffer) : base(buffer, ID) {
			keepAliveId = ReadLong();
		}
	}

	public class ClientConfigPingPacket : ClientConfigPacket {
		public const int ID = 0x05;

		public readonly int pingId;

		public ClientConfigPingPacket(byte[] buffer) : base(buffer, ID) {
			pingId = ReadInt();
		}
	}

	public class ClientConfigRegistryDataPacket : ClientConfigPacket {
		public const int ID = 0x07;
		
		public readonly string registryId;
		public readonly Dictionary<string, NbtCompound> entries;

		public ClientConfigRegistryDataPacket(byte[] buffer) : base(buffer, ID) {
			registryId = ReadIdentifier();
			
			int entryCount = ReadVarInt();
			entries = new(entryCount);
			
			for (int i = 0; i < entryCount; i++) {
				string entryName = ReadIdentifier();
				bool hasData = ReadBoolean();
				NbtCompound entryData = hasData ? ReadNBT() : new NbtCompound();
				
				entries[entryName] = entryData;
			}
		}
	}

	public class ClientConfigKnownPacksPacket : ClientConfigPacket {
		public const int ID = 0x0E;

		public readonly (string, string, string)[] knownPacks;

		public ClientConfigKnownPacksPacket(byte[] buffer) : base(buffer, ID) {
			int count = ReadVarInt();
			knownPacks = new (string, string, string)[count];

			for (int x = 0; x < count; x++) {
				string namespaceValue = ReadString();
				string id = ReadString();
				string version = ReadString();

				knownPacks[x] = (namespaceValue, id, version);
			}
		}
	}
}