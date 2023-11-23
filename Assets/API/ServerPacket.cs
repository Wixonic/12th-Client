using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace API {
	public class ServerPacket : Packet {
		public readonly static Side SIDE = Side.Server;

		private readonly MemoryStream buffer = new();

		public Span<byte> data { get => this.buffer.ToArray().AsSpan(); }

		public ServerPacket(int id, State state) : base(id, state, SIDE) {
			this.WriteVarInt(id);
		}

		public void WriteBool(bool value) => this.WriteByte((byte)(value ? 1 : 0));

		public void WriteByte(byte b) {
			lock (this.bufferLock) this.buffer.WriteByte(b);
		}

		public void WriteByte(int i) => this.WriteByte((byte)i);

		public void WriteSByte(sbyte b) => this.WriteByte((byte)b);

		public void WriteBytes(Span<byte> bytes) {
			lock (this.bufferLock) this.buffer.Write(bytes);
		}

		public void WriteULong(ulong value) {
			if (BitConverter.IsLittleEndian) {
				this.WriteByte((byte)(value >> 56 & 0xFF));
				this.WriteByte((byte)(value >> 48 & 0xFF));
				this.WriteByte((byte)(value >> 40 & 0xFF));
				this.WriteByte((byte)(value >> 32 & 0xFF));
				this.WriteByte((byte)(value >> 24 & 0xFF));
				this.WriteByte((byte)(value >> 16 & 0xFF));
				this.WriteByte((byte)(value >> 8 & 0xFF));
				this.WriteByte((byte)(value & 0xFF));
			} else {
				this.WriteByte((byte)(value & 0xFF));
				this.WriteByte((byte)(value >> 8 & 0xFF));
				this.WriteByte((byte)(value >> 16 & 0xFF));
				this.WriteByte((byte)(value >> 24 & 0xFF));
				this.WriteByte((byte)(value >> 32 & 0xFF));
				this.WriteByte((byte)(value >> 40 & 0xFF));
				this.WriteByte((byte)(value >> 48 & 0xFF));
				this.WriteByte((byte)(value >> 56 & 0xFF));
			}
		}

		public void WriteLong(long value) => this.WriteULong((ulong)value);

		public void WriteUShort(ushort value) {
			if (BitConverter.IsLittleEndian) {
				this.WriteByte((byte)(value >> 8 & 0xFF));
				this.WriteByte((byte)(value & 0xFF));
			} else {
				this.WriteByte((byte)(value & 0xFF));
				this.WriteByte((byte)(value >> 8 & 0xFF));
			}
		}

		public void WriteShort(short value) => this.WriteUShort((ushort)value);

		public void WriteString(string value, int maxLength = 0) {
			int length = Encoding.UTF8.GetByteCount(value);
			if (length > maxLength * 4 + 3) throw new IndexOutOfRangeException($"Found a string with {length} bytes, but expecting only {maxLength * 4 + 3} bytes - {this.state}:0x{this.id:x2}");

			byte[] bytes = Encoding.UTF8.GetBytes(value);

			this.WriteVarInt(bytes.Length);
			this.WriteBytes(bytes);
		}

		public void WriteChat(string value, int maxLength) {
			this.WriteString(value, maxLength);
		}

		public void WriteUUID(Guid value) {
			byte[] guidBytes = value.ToByteArray();

			byte[] uuidBytes = {
				guidBytes[6],
				guidBytes[7],
				guidBytes[4],
				guidBytes[5],
				guidBytes[0],
				guidBytes[1],
				guidBytes[2],
				guidBytes[3],
				guidBytes[15],
				guidBytes[14],
				guidBytes[13],
				guidBytes[12],
				guidBytes[11],
				guidBytes[10],
				guidBytes[9],
				guidBytes[8]
			};

			this.WriteLong(BitConverter.ToInt64(uuidBytes, 0));
			this.WriteLong(BitConverter.ToInt64(uuidBytes, 8));
		}

		public void WriteVarInt(int value) {
			const int SEGMENT_BITS = 0x7F;
			const int CONTINUE_BIT = 0x80;

			while (true) {
				if ((value & ~SEGMENT_BITS) == 0) {
					this.WriteByte(value);
					return;
				}

				this.WriteByte((value & SEGMENT_BITS) | CONTINUE_BIT);

				value = (int)((uint)value >> 7);
			}
		}

		public void WritePosition(Vector3Int vector) => this.WriteULong(((ulong)vector.x & 0x3FFFFFF) << 38 | ((ulong)vector.z & 0x3FFFFFF) << 12 | (ulong)vector.y & 0xFFF);

		public void WriteNBT(NBT nbt) => nbt.WriteTo(this);
	}
}