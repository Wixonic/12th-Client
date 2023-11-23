using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Palmmedia.ReportGenerator.Core.Common;
using Unity.VisualScripting;
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

		public readonly Dictionary<string, object> data;

		public NBT(ClientPacket packet) {
			NBT.Type type = (NBT.Type)packet.ReadByte();
			if (type != NBT.Type.Compound) return;

			packet.position--;

			Dictionary<string, object> parse() {
				object getValueFor(byte id) {
					switch (id) {
						case (byte)NBT.Type.End:
							return null;

						case (byte)NBT.Type.Byte:
							return packet.ReadByte();

						case (byte)NBT.Type.Short:
							return packet.ReadShort();

						case (byte)NBT.Type.Int:
							return packet.ReadInt();

						case (byte)NBT.Type.Long:
							return packet.ReadLong();

						case (byte)NBT.Type.Float:
							return packet.ReadFloat();

						case (byte)NBT.Type.Double:
							return packet.ReadDouble();

						case (byte)NBT.Type.ByteArray:
							return packet.ReadBytes(packet.ReadInt());

						case (byte)NBT.Type.String:
							ushort length = packet.ReadUShort();
							if (length > 0) {
								byte[] bytes = packet.ReadBytes(length);
								return Encoding.UTF8.GetString(bytes);
							} else return "";

						case (byte)NBT.Type.List:
							List<object> list = new();

							byte type = packet.ReadByte();

							int listLength = packet.ReadInt();
							if (listLength == 0) packet.ReadByte();
							else for (int i = 0; i < listLength; i++) list.Add(getValueFor(type));

							return list.ToArray();

						case (byte)NBT.Type.Compound:
							return parse();

						case (byte)NBT.Type.IntArray:
							List<int> intList = new();

							int intListLength = packet.ReadInt();

							for (int i = 0; i < intListLength; i++) intList.Add((int)getValueFor((byte)NBT.Type.Int));

							return intList.ToArray();

						case (byte)NBT.Type.LongArray:
							List<long> longList = new();

							int longListLength = packet.ReadInt();

							for (int i = 0; i < longListLength; i++) longList.Add((long)getValueFor((byte)NBT.Type.Long));

							return longList.ToArray();

						default:
							Debug.LogWarning($"Unknown NBT Type: 0x{id:x2}");
							return null;
					}
				}

				Dictionary<string, object> dict = new();

				byte id;

				while ((id = packet.ReadByte()) != (byte)NBT.Type.End) {
					string name = (string)getValueFor((byte)NBT.Type.String);
					object value = getValueFor(id);
					dict.Add(name, value);
				}

				return dict;
			}

			data = parse();
		}

		public void WriteTo(ServerPacket packet) {

		}
	}
}