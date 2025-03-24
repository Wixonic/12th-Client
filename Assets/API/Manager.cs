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

		public int compression = -1;

		public async Task Connect(string ip, ushort port) {
			await tcp.ConnectAsync(ip ?? "90.91.236.96", port);
			stream = tcp.GetStream();
			Thread listener = new(Listen);
			listener.Start();
		}

		public void Disconnect() {
			tcp.Dispose();
			stream.Dispose();
		}

		public void AddListener(int id, State state, Action<ClientPacket> func, bool once = false) => listeners.Add(new(id, state, func, once));

		private void Listen() {
			new Thread(() => {
				while (tcp.Connected && stream.CanRead) {
					// try {
						int length = ReadVarInt();
						int alreadyRead = 0;

						byte[] data = new byte[length];

						while (alreadyRead < length) {
							alreadyRead += stream.Read(data, alreadyRead, length - alreadyRead);
						}

						// try {
							ClientPacket packet = ClientPacket.Parse(data, state);

							if (packet != null) {
								List<Tuple<int, State, Action<ClientPacket>, bool>> listenersToRemove = new();

								foreach (Tuple<int, State, Action<ClientPacket>, bool> listener in listeners) {
									if (packet.id == listener.Item1 && packet.state == listener.Item2) {
										listener.Item3(packet);
										if (listener.Item4) listenersToRemove.Add(listener);
									}
								}

								foreach (Tuple<int, State, Action<ClientPacket>, bool> listener in listenersToRemove) listeners.Remove(listener);
							}
						/* } catch (Exception e) {
							Debug.LogError($"Failed to parse client-packet: {e.Message}");
						} */
					/* } catch (Exception e) {
						Debug.LogError($"Failed to read network stream: {e.Message}");
					} */
				}
			}).Start();
		}

		public void Send(ServerPacket packet) {
			if (state.Equals(packet.state) && Side.Server.Equals(packet.side)) {
				lock (bufferLock) {
					buffer = new();
					WriteVarInt(packet.data.Length);
					buffer.Write(packet.data);
					stream.Write(buffer.ToArray());
					buffer.Dispose();
				}
			} else if (Side.Server.Equals(packet.side)) Debug.LogError($"Packet {packet.state}:0x{packet.id:x2} is sent when in an invalid state ({packet.state} when {state})");
			else Debug.LogError($"Packet {packet.state}:0x{packet.id:x2} is sent to server when should be recieved");
		}

		private int ReadVarInt() {
			int value = 0;
			int shift = 0;

			while (true) {
				byte b = (byte)stream.ReadByte();
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
					buffer.WriteByte((byte)value);
					break;
				} else {
					buffer.WriteByte((byte)(value & 0x7F | 0x80));
					value >>= 7;
				}
			}
		}
	}
}