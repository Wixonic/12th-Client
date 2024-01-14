using SmartNbt;
using SmartNbt.Tags;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace API {
	public class ClientPacket : Packet {
		public readonly static List<Tuple<int, State, Func<byte[], ClientPacket>>> list = new(){
			new(ClientDisconnectLoginPacket.ID, ClientDisconnectLoginPacket.STATE, (byte[] buffer) => new ClientDisconnectLoginPacket(buffer)),
			new(ClientDisconnectPlayPacket.ID, ClientDisconnectPlayPacket.STATE, (byte[] buffer) => new ClientDisconnectPlayPacket(buffer)),

			new(ClientEncryptionRequestPacket.ID, ClientEncryptionRequestPacket.STATE, (byte[] buffer) => new ClientEncryptionRequestPacket(buffer)),
			new(ClientLoginSuccessPacket.ID, ClientLoginSuccessPacket.STATE, (byte[] buffer) => new ClientLoginSuccessPacket(buffer)),
			new(ClientSetCompressionPacket.ID, ClientSetCompressionPacket.STATE, (byte[] buffer) => new ClientSetCompressionPacket(buffer)),
			new(ClientLoginPluginRequestPacket.ID, ClientLoginPluginRequestPacket.STATE, (byte[] buffer) => new ClientLoginPluginRequestPacket(buffer)),

			new(ClientLoginPlayPacket.ID, ClientLoginPlayPacket.STATE, (byte[] buffer) => new ClientLoginPlayPacket(buffer)),

			new(ClientBundleDelimiterPacket.ID, ClientBundleDelimiterPacket.STATE, (byte[] buffer) => new ClientBundleDelimiterPacket(buffer)),
			new(ClientKeepAlivePacket.ID, ClientKeepAlivePacket.STATE, (byte[] buffer) => new ClientKeepAlivePacket(buffer)),
			new(ClientChunkDataPacket.ID, ClientChunkDataPacket.STATE, (byte[] buffer) => new ClientChunkDataPacket(buffer)),
			new(ClientSynchronizePositionPacket.ID, ClientSynchronizePositionPacket.STATE, (byte[] buffer) => new ClientSynchronizePositionPacket(buffer)),
			new(ClientRespawnPacket.ID, ClientRespawnPacket.STATE, (byte[] buffer) => new ClientRespawnPacket(buffer)),
			new(ClientUpdateTimePacket.ID, ClientUpdateTimePacket.STATE, (byte[] buffer) => new ClientUpdateTimePacket(buffer))
		};

		internal MemoryStream buffer;

		public static ClientPacket Parse(byte[] buffer, State state) {
			// try {
			int id = ReadVarInt(buffer);

			string[] ignored = { };

			if (!ignored.Contains($"{state}:0x{id.ToString("x2").ToUpper()}")) {
				Tuple<int, State, Func<byte[], ClientPacket>> tuple;

				try {
					tuple = ClientPacket.list.First(tuple => tuple.Item1.Equals(id) && tuple.Item2.Equals(state));
					// Debug.Log($"Recieved packed {state}:0x{id:x2}");
				} catch {
					tuple = null;
					// Debug.Log($"Recieved packed {state}:0x{id:x2}");
				}

				if (tuple != null) return tuple.Item3(buffer);
			}
			/* } catch (Exception e) {
				Debug.LogError($"Unvalid packet: {e.Message}");
			} */

			return null;
		}

		public static int ReadVarInt(byte[] buffer) {
			int value = 0;
			int shift = 0;

			int position = 0;

			while (true) {
				if (buffer.Length > position) {
					byte b = buffer[position];
					position++;

					value |= (b & 0x7f) << shift;
					if ((b & 0x80) == 0x00) break;

					shift += 7;
					if (shift >= 32) throw new Exception("VarInt overflow");
				}
			}

			return value;
		}

		public readonly static Side SIDE = Side.Client;

		public ClientPacket(byte[] buffer, int id, State state) : base(id, state, SIDE) {
			this.buffer = new(buffer);
			this.ReadVarInt(); // Remove the Packet ID
		}

		public byte ReadByte() => (byte)this.buffer.ReadByte();

		public byte[] ReadBytes(int count) {
			MemoryStream bytes = new();
			for (int x = 0; x < count; ++x) bytes.WriteByte(this.ReadByte());
			return bytes.ToArray();
		}

		public sbyte ReadSByte() => (sbyte)this.ReadByte();

		public bool ReadBoolean() => this.ReadByte() == 1;

		public ushort ReadUShort(bool isBigEndian = false) {
			int b0 = this.ReadByte();
			int b1 = this.ReadByte();

			if (BitConverter.IsLittleEndian && !isBigEndian) return (ushort)(b0 << 8 | b1);
			return (ushort)(b0 | b1 << 8);
		}

		public short ReadShort(bool isBigEndian = false) => (short)this.ReadUShort(isBigEndian);

		public uint ReadUInt(bool isBigEndian = false) {
			int b0 = this.ReadByte();
			int b1 = this.ReadByte();
			int b2 = this.ReadByte();
			int b3 = this.ReadByte();

			if (BitConverter.IsLittleEndian && !isBigEndian) return (uint)(b0 << 24 | b1 << 16 | b2 << 8 | b3);
			return (uint)(b0 | b1 << 8 | b2 << 16 | b3 << 24);
		}

		public int ReadInt(bool isBigEndian = false) => (int)this.ReadUInt(isBigEndian);

		public ulong ReadULong(bool isBigEndian = false) {
			long b0 = this.ReadByte();
			long b1 = this.ReadByte();
			long b2 = this.ReadByte();
			long b3 = this.ReadByte();
			long b4 = this.ReadByte();
			long b5 = this.ReadByte();
			long b6 = this.ReadByte();
			long b7 = this.ReadByte();

			if (BitConverter.IsLittleEndian && !isBigEndian) return (ulong)(b0 << 56 | b1 << 48 | b2 << 40 | b3 << 32 | b4 << 24 | b5 << 16 | b6 << 8 | b7);
			return (ulong)(b0 | b1 << 8 | b2 << 16 | b3 << 24 | b4 << 32 | b5 << 40 | b6 << 48 | b7 << 56);
		}

		public long ReadLong(bool isBigEndian = false) => (long)this.ReadULong(isBigEndian);

		public float ReadFloat(bool isBigEndian = false) {
			byte[] bytes = this.ReadBytes(sizeof(float));

			if (BitConverter.IsLittleEndian && !isBigEndian) bytes.Reverse();
			return BitConverter.ToSingle(bytes);
		}

		public double ReadDouble(bool isBigEndian = false) {
			byte[] bytes = this.ReadBytes(sizeof(double));

			if (BitConverter.IsLittleEndian && !isBigEndian) bytes.Reverse();
			return BitConverter.ToDouble(bytes);
		}

		public string ReadString(int maxLength = 0) {
			int length = this.ReadVarInt();

			if (maxLength > 0 && length > maxLength * 4 + 3) throw new IndexOutOfRangeException($"Found a string with {length} bytes, but expecting only {maxLength * 4 + 3} bytes - {this.state}:0x{this.id:x2}");

			byte[] bytes = this.ReadBytes(length);

			return Encoding.UTF8.GetString(bytes);
		}

		public string ReadChat() {
			return this.ReadString(262144);
		}

		public string ReadIdentifier() {
			return this.ReadString(32767);
		}

		public Guid ReadUUID() {
			byte[] UUIDBytes = this.ReadBytes(16);

			byte[] GuidBytes = {
				UUIDBytes[4],
				UUIDBytes[5],
				UUIDBytes[6],
				UUIDBytes[7],
				UUIDBytes[2],
				UUIDBytes[3],
				UUIDBytes[0],
				UUIDBytes[1],
				UUIDBytes[15],
				UUIDBytes[14],
				UUIDBytes[13],
				UUIDBytes[12],
				UUIDBytes[11],
				UUIDBytes[10],
				UUIDBytes[9],
				UUIDBytes[8]
			};

			return new Guid(GuidBytes);
		}

		public int ReadVarInt() {
			int value = 0;
			int shift = 0;

			while (true) {
				byte b = this.ReadByte();
				value |= (b & 0x7f) << shift;
				if ((b & 0x80) == 0x00) break;

				shift += 7;
				if (shift >= 32) throw new Exception("VarInt overflow");
			}

			return value;
		}

		public Vector3Int ReadPosition() {
			ulong value = this.ReadULong();
			Vector3Int vector = new((int)value >> 38, (int)value & 0xFFF, (int)value >> 12 & 0x3FFFFFF);

			if (vector.x >= Math.Pow(2, 25)) { vector.x -= (int)Math.Pow(2, 26); }
			if (vector.y >= Math.Pow(2, 11)) { vector.y -= (int)Math.Pow(2, 12); }
			if (vector.z >= Math.Pow(2, 25)) { vector.z -= (int)Math.Pow(2, 26); }

			return vector;
		}

		public NbtCompound ReadNBT() {
			if (this.ReadByte() != 0x0a) return new NbtCompound("Empty or Broken");
			else this.buffer.Position--;

			NbtFile file = new();

			// try {
			file.LoadFromStream(this.buffer, NbtCompression.None);
			return file.RootTag;
			/* } catch (Exception e) {
				Debug.LogError($"Failed to parse NBT in packet {this.state}:0x{this.id:x2}: {e.Message}");
				return new();
			} */
		}
	}
}