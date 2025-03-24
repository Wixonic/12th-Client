using System;
using System.IO;
using System.Text;
using SmartNbt.Tags;
using UnityEngine;

namespace API {
    public class ServerPacket : Packet {
        public static readonly Side SIDE = Side.Server;
        private readonly MemoryStream buffer = new();
        public Span<byte> data { get => buffer.ToArray().AsSpan(); }

        public ServerPacket(int id, State state) : base(id, state, SIDE) {
            WriteVarInt(id);
        }

        public void WriteBool(bool value) => WriteByte((byte)(value ? 1 : 0));

        public void WriteByte(byte b) {
            lock (bufferLock) buffer.WriteByte(b);
        }

        public void WriteByte(int i) => WriteByte((byte)i);

        public void WriteSByte(sbyte b) => WriteByte((byte)b);

        public void WriteBytes(Span<byte> bytes) {
            lock (bufferLock) buffer.Write(bytes);
        }

        public void WriteULong(ulong value) {
            if (BitConverter.IsLittleEndian) {
                WriteByte((byte)((value >> 56) & 0xFF));
                WriteByte((byte)((value >> 48) & 0xFF));
                WriteByte((byte)((value >> 40) & 0xFF));
                WriteByte((byte)((value >> 32) & 0xFF));
                WriteByte((byte)((value >> 24) & 0xFF));
                WriteByte((byte)((value >> 16) & 0xFF));
                WriteByte((byte)((value >> 8) & 0xFF));
                WriteByte((byte)(value & 0xFF));
            } else {
                WriteByte((byte)(value & 0xFF));
                WriteByte((byte)((value >> 8) & 0xFF));
                WriteByte((byte)((value >> 16) & 0xFF));
                WriteByte((byte)((value >> 24) & 0xFF));
                WriteByte((byte)((value >> 32) & 0xFF));
                WriteByte((byte)((value >> 40) & 0xFF));
                WriteByte((byte)((value >> 48) & 0xFF));
                WriteByte((byte)((value >> 56) & 0xFF));
            }
        }

        public void WriteLong(long value) => WriteULong((ulong)value);

        public void WriteUShort(ushort value) {
            if (BitConverter.IsLittleEndian) {
                WriteByte((byte)((value >> 8) & 0xFF));
                WriteByte((byte)(value & 0xFF));
            } else {
                WriteByte((byte)(value & 0xFF));
                WriteByte((byte)((value >> 8) & 0xFF));
            }
        }

        public void WriteShort(short value) => WriteUShort((ushort)value);

        public void WriteString(string value, int maxLength = 0) {
            int length = Encoding.UTF8.GetByteCount(value);
            if (length > maxLength * 4 + 3)
                throw new IndexOutOfRangeException($"Found a string with {length} bytes, but expecting only {maxLength * 4 + 3} bytes - {state}:0x{id:x2}");
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            WriteVarInt(bytes.Length);
            WriteBytes(bytes);
        }

        public void WriteChat(string value, int maxLength) => WriteString(value, maxLength);

        public void WriteUUID(Guid value) {
            byte[] guidBytes = value.ToByteArray();
            byte[] uuidBytes = {
                guidBytes[6], guidBytes[7], guidBytes[4], guidBytes[5],
                guidBytes[0], guidBytes[1], guidBytes[2], guidBytes[3],
                guidBytes[15], guidBytes[14], guidBytes[13], guidBytes[12],
                guidBytes[11], guidBytes[10], guidBytes[9], guidBytes[8]
            };
            WriteLong(BitConverter.ToInt64(uuidBytes, 0));
            WriteLong(BitConverter.ToInt64(uuidBytes, 8));
        }

        public void WriteVarInt(int value) {
            const int SEGMENT_BITS = 0x7F;
            const int CONTINUE_BIT = 0x80;
            while (true) {
                if ((value & ~SEGMENT_BITS) == 0) {
                    WriteByte(value);
                    return;
                }
                WriteByte((byte)((value & SEGMENT_BITS) | CONTINUE_BIT));
                value = (int)((uint)value >> 7);
            }
        }

        public void WriteInt(int value) {
            if (BitConverter.IsLittleEndian) {
                WriteByte((byte)((value >> 24) & 0xFF));
                WriteByte((byte)((value >> 16) & 0xFF));
                WriteByte((byte)((value >> 8) & 0xFF));
                WriteByte((byte)(value & 0xFF));
            } else {
                WriteByte((byte)(value & 0xFF));
                WriteByte((byte)((value >> 8) & 0xFF));
                WriteByte((byte)((value >> 16) & 0xFF));
                WriteByte((byte)((value >> 24) & 0xFF));
            }
        }

        public void WriteFloat(float value) {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            WriteBytes(bytes);
        }

        public void WriteDouble(double value) {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            WriteBytes(bytes);
        }

        public void WriteVarLong(long value) {
            const long SEGMENT_BITS = 0x7F;
            const long CONTINUE_BIT = 0x80;
            while (true) {
                if ((value & ~SEGMENT_BITS) == 0) {
                    WriteByte((byte)value);
                    return;
                }
                WriteByte((byte)((value & SEGMENT_BITS) | CONTINUE_BIT));
                value = (long)((ulong)value >> 7);
            }
        }

        public void WritePosition(Vector3Int vector) {
            WriteULong(((ulong)vector.x & 0x3FFFFFF) << 38 | ((ulong)vector.z & 0x3FFFFFF) << 12 | ((ulong)vector.y & 0xFFF));
        }

        public void WriteAngle(float angle) {
            WriteByte((byte)(angle * 256 / 360));
        }

        public void WriteNBT(NbtCompound tag) => WriteBytes(tag.ByteArrayValue);
    }
}