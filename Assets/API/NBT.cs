using UnityEngine;

namespace API {
	public class NBT {
		public enum Type {
			End = 0,
			Byte = 1,
			Short = 2,
			Int = 3,
			Long = 4,
			Float = 5,
			Double = 6,
			ByteArray = 7,
			String = 8,
			List = 9,
			Compound = 10,
			IntArray = 11,
			LongArray = 12
		}

		public NBT(ClientPacket packet) {
			NBT.Type type = (NBT.Type)packet.ReadByte();
			if (type != NBT.Type.Compound) return;

			packet.position--;

			Debug.Log("Compound");
		}

		public void WriteTo(ServerPacket packet) {

		}
	}
}