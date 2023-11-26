using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace API {
	public class Manager {
		private readonly List<Tuple<int, State, Action<ClientPacket>, bool>> listeners = new();

		private MemoryStream buffer;
		private readonly TcpClient tcp = new();
		private NetworkStream stream;
		public State state;
		private readonly object bufferLock = new();

		public async Task Connect(string ip, ushort port) {
			await this.tcp.ConnectAsync(ip ?? "90.91.236.96", port);
			this.stream = this.tcp.GetStream();
			Thread listener = new(this.Listen);
			listener.Start();
		}

		public void Disconnect() {
			this.tcp.Dispose();
			this.stream.Dispose();
		}

		public void AddListener(int id, State state, Action<ClientPacket> func, bool once = false) => this.listeners.Add(new(id, state, func, once));

		private void Listen() {
			new Thread(() => {
				while (this.tcp.Connected && this.stream.CanRead) {
					int length = this.ReadVarInt();
					int alreadyRead = 0;

					byte[] data = new byte[length];

					while (alreadyRead < length) {
						alreadyRead += this.stream.Read(data, alreadyRead, length - alreadyRead);
					}

					// try {
					ClientPacket packet = ClientPacket.Parse(data, this.state);

					if (packet != null) {
						List<Tuple<int, State, Action<ClientPacket>, bool>> listenersToRemove = new();

						foreach (Tuple<int, State, Action<ClientPacket>, bool> listener in this.listeners) {
							if (packet.id == listener.Item1 && packet.state == listener.Item2) {
								listener.Item3(packet);
								if (listener.Item4) listenersToRemove.Add(listener);
							}
						}

						foreach (Tuple<int, State, Action<ClientPacket>, bool> listener in listenersToRemove) this.listeners.Remove(listener);
					}
					/* } catch (Exception e) {
						Debug.LogError($"Failed to parse client-packet: {e.Message}");
					} */
				}
			}).Start();
		}

		public void Send(ServerPacket packet) {
			if (this.state.Equals(packet.state) && Side.Server.Equals(packet.side)) {
				// Debug.Log($"Sending packet {this.state}:0x{packet.id:x2}");

				lock (this.bufferLock) {
					this.buffer = new();
					this.WriteVarInt(packet.data.Length);
					this.buffer.Write(packet.data);
					this.stream.Write(this.buffer.ToArray());
					this.buffer.Dispose();
				}
			} else if (Side.Server.Equals(packet.side)) Debug.LogError($"Packet {packet.state}:0x{packet.id:x2} is sent when in an invalid state ({packet.state} when {this.state})");
			else Debug.LogError($"Packet {packet.state}:0x{packet.id:x2} is sent to server when should be recieved");
		}

		private int ReadVarInt() {
			int value = 0;
			int shift = 0;

			while (true) {
				byte b = (byte)this.stream.ReadByte();
				value |= (b & 0x7f) << shift;
				if ((b & 0x80) == 0x00) break;

				shift += 7;
				if (shift >= 32) throw new Exception("VarInt overflow");
			}

			return value;
		}

		private void WriteVarInt(int value) {
			while (true) {
				if ((value & ~0x7F) == 0) {
					this.buffer.WriteByte((byte)value);
					break;
				} else {
					this.buffer.WriteByte((byte)(value & 0x7F | 0x80));
					value >>= 7;
				}
			}
		}
	}
}