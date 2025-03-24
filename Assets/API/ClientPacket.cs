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
        public const Side SIDE = Side.Client;
        private const int STRING_MAX = 32767;
        private const int CHAT_MAX = 262144;
        
        public static readonly List<Tuple<int, State, Func<byte[], ClientPacket>>> list = new() {
            // Login packets
            new(ClientLoginDisconnectPacket.ID, ClientLoginDisconnectPacket.STATE, b => new ClientLoginDisconnectPacket(b)),
            new(ClientLoginSuccessPacket.ID, ClientLoginSuccessPacket.STATE, b => new ClientLoginSuccessPacket(b)),

            // Config packets
            new(ClientConfigDisconnectPacket.ID, ClientConfigDisconnectPacket.STATE, b => new ClientConfigDisconnectPacket(b)),
            new(ClientConfigFinishPacket.ID, ClientConfigFinishPacket.STATE, b => new ClientConfigFinishPacket(b)),
            new(ClientConfigKeepAlivePacket.ID, ClientConfigKeepAlivePacket.STATE, b => new ClientConfigKeepAlivePacket(b)),
            new(ClientConfigPingPacket.ID, ClientConfigPingPacket.STATE, b => new ClientConfigPingPacket(b)),
            new(ClientConfigRegistryDataPacket.ID, ClientConfigRegistryDataPacket.STATE, b => new ClientConfigRegistryDataPacket(b)),
            new(ClientConfigKnownPacksPacket.ID, ClientConfigKnownPacksPacket.STATE, b => new ClientConfigKnownPacksPacket(b)),

            // Play packets
            new(ClientPlayChunkBatchFinishedPacket.ID, ClientPlayChunkBatchFinishedPacket.STATE, b => new ClientPlayChunkBatchFinishedPacket(b)),
            new(ClientPlayDisconnectPacket.ID, ClientPlayDisconnectPacket.STATE, b => new ClientPlayDisconnectPacket(b)),
            new(ClientPlayChangeDifficultyPacket.ID, ClientPlayChangeDifficultyPacket.STATE, b => new ClientPlayChangeDifficultyPacket(b)),
            new(ClientPlayLoginPacket.ID, ClientPlayLoginPacket.STATE, b => new ClientPlayLoginPacket(b)),
            new(ClientPlayChunkDataPacket.ID, ClientPlayChunkDataPacket.STATE, b => new ClientPlayChunkDataPacket(b)),
            new(ClientPlayKeepAlivePacket.ID, ClientPlayKeepAlivePacket.STATE, b => new ClientPlayKeepAlivePacket(b)),
            new(ClientPlayPingPacket.ID, ClientPlayPingPacket.STATE, b => new ClientPlayPingPacket(b)),
            new(ClientPlaySynchronizePositionPacket.ID, ClientPlaySynchronizePositionPacket.STATE, b => new ClientPlaySynchronizePositionPacket(b)),
            new(ClientPlayUpdateTimePacket.ID, ClientPlayUpdateTimePacket.STATE, b => new ClientPlayUpdateTimePacket(b))
        };

        internal MemoryStream buffer;

        public static ClientPacket Parse(byte[] buffer, State state) {
            try {
                using var ms = new MemoryStream(buffer);
                int id = ReadVarIntStatic(ms);
                
                Debug.Log($"Recieved packed {state}:0x{id:x2}");
                
                byte[] remainingBuffer = new byte[buffer.Length - ms.Position];
                Buffer.BlockCopy(buffer, (int)ms.Position, remainingBuffer, 0, remainingBuffer.Length);

                var tuple = list.FirstOrDefault(t => t.Item1 == id && t.Item2 == state);
                if (tuple != null) return tuple.Item3(remainingBuffer);
                
                Debug.LogWarning($"[Net] Unknown {state} packet: 0x{id:X2}");
            } catch (Exception e) {
                Debug.LogError($"[Net] Parse error: {e.Message}\n{e.StackTrace}");
            }
            return null;
        }

        public ClientPacket(byte[] buffer, int id, State state) : base(id, state, SIDE) {
            this.buffer = new MemoryStream(buffer);
        }

        public byte ReadByte() => (byte)buffer.ReadByte();
        public sbyte ReadSByte() => (sbyte)ReadByte();
        public bool ReadBoolean() => ReadByte() != 0;
        
        public byte[] ReadBytes(int count) {
            var result = new byte[count];
            if (buffer.Read(result, 0, count) != count) throw new EndOfStreamException();
            return result;
        }

        public short ReadShort(bool bigEndian = false) => BitConverter.ToInt16(AdjustEndian(2, bigEndian), 0);
        public ushort ReadUShort(bool bigEndian = false) => BitConverter.ToUInt16(AdjustEndian(2, bigEndian), 0);
        public int ReadInt(bool bigEndian = false) => BitConverter.ToInt32(AdjustEndian(4, bigEndian), 0);
        public uint ReadUInt(bool bigEndian = false) => BitConverter.ToUInt32(AdjustEndian(4, bigEndian), 0);
        public long ReadLong(bool bigEndian = false) => BitConverter.ToInt64(AdjustEndian(8, bigEndian), 0);
        public ulong ReadULong(bool bigEndian = false) => BitConverter.ToUInt64(AdjustEndian(8, bigEndian), 0);
        public float ReadFloat(bool bigEndian = false) => BitConverter.ToSingle(AdjustEndian(4, bigEndian), 0);
        public double ReadDouble(bool bigEndian = false) => BitConverter.ToDouble(AdjustEndian(8, bigEndian), 0);

        public int ReadVarInt() {
            uint value = 0;
            int shift = 0;
            byte b;
            do {
                b = ReadByte();
                value |= (uint)(b & 0x7F) << shift;
                if ((shift += 7) > 35) throw new OverflowException("VarInt >5 bytes");
            } while ((b & 0x80) != 0);
            return (int)value;
        }

        public long ReadVarLong() {
            ulong value = 0;
            int shift = 0;
            byte b;
            do {
                b = ReadByte();
                value |= (ulong)(b & 0x7F) << shift;
                if ((shift += 7) > 70) throw new OverflowException("VarLong >10 bytes");
            } while ((b & 0x80) != 0);
            return (long)value;
        }

        public Vector3Int ReadPosition() {
            long val = ReadLong();
            return new Vector3Int((int)(val >> 38), (int)(val & 0xFFF), (int)((val >> 12) & 0x3FFFFFF));
        }

        public Guid ReadUUID() {
            byte[] bytes = ReadBytes(16);
            return new Guid(new[] {
                bytes[3], bytes[2], bytes[1], bytes[0], bytes[5], bytes[4], 
                bytes[7], bytes[6], bytes[8], bytes[9], bytes[10], bytes[11],
                bytes[12], bytes[13], bytes[14], bytes[15]
            });
        }

        public string ReadString(int maxLength = STRING_MAX) {
            int byteLength = ReadVarInt();
            if (byteLength > maxLength * 4 + 3) throw new InvalidDataException($"String too long ({maxLength})");
            return Encoding.UTF8.GetString(ReadBytes(byteLength));
        }

        public string ReadChat() => ReadString(CHAT_MAX);
        public string ReadIdentifier() => ReadString(STRING_MAX);
        
        public long[] ReadLongArray() {
            var arr = new long[ReadVarInt()];
            for (int i = 0; i < arr.Length; i++) arr[i] = ReadLong();
            return arr;
        }
        
        public NbtCompound ReadNBT() {
            byte type = ReadByte();
            if (type != 0x0A) throw new InvalidDataException($"Invalid NBT: Expected 0x0A, but got instead 0x{type:X2}");
			buffer.Position--;

			NbtFile file = new();

			try {
				file.LoadFromStream(buffer, NbtCompression.None);
				return file.RootTag;
			} catch (Exception e) {
				Debug.LogError($"Failed to parse NBT in packet {state}:0x{id:x2}: {e.Message}");
				return new();
			}
        }

        private byte[] AdjustEndian(int byteSize, bool bigEndian) {
            var bytes = ReadBytes(byteSize);
            if (bigEndian != BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return bytes;
        }

        private static int ReadVarIntStatic(Stream stream) {
            int value = 0, shift = 0, b;
        	while ((b = stream.ReadByte()) != -1) {
                value |= (b & 0x7F) << shift;
                if ((shift += 7) > 35) throw new OverflowException();
                if ((b & 0x80) == 0) break;
            }
            return value;
        }
    }
}