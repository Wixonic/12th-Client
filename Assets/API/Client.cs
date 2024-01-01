using System;
using System.Threading.Tasks;
using UnityEngine;

namespace API {
	public class Client {
		public readonly int protocolVersion = 763;
		public readonly Manager manager;

		public Client() => this.manager = new();

		public async Task Connect(string ip, ushort? port, string username, Guid? uuid) {
			await this.manager.Connect(ip, port ?? 25565);

			// Disconnected
			this.manager.AddListener(ClientDisconnectLoginPacket.ID, ClientDisconnectLoginPacket.STATE, (ClientPacket p) => this.Disconnect((ClientDisconnectPacket)p), true);
			this.manager.AddListener(ClientDisconnectPlayPacket.ID, ClientDisconnectPlayPacket.STATE, (ClientPacket p) => this.Disconnect((ClientDisconnectPacket)p), true);


			// Encryption Request
			this.manager.AddListener(ClientEncryptionRequestPacket.ID, ClientEncryptionRequestPacket.STATE, (ClientPacket p) => Debug.LogError("The server is in online mode"), true);

			// Login Success
			this.manager.AddListener(ClientLoginSuccessPacket.ID, ClientLoginSuccessPacket.STATE, (ClientPacket p) => {
				ClientLoginSuccessPacket packet = (ClientLoginSuccessPacket)p;
			}, true);

			// Set Compression
			this.manager.AddListener(ClientSetCompressionPacket.ID, ClientSetCompressionPacket.STATE, (ClientPacket p) => {
				ClientSetCompressionPacket packet = (ClientSetCompressionPacket)p;
				this.manager.compression = packet.threshold;
				Debug.LogWarning($"The server asks for compression: {packet.threshold}");
			}, true);

			// Login Plugin Request
			this.manager.AddListener(ClientLoginPluginRequestPacket.ID, ClientLoginPluginRequestPacket.STATE, (ClientPacket p) => {
				ClientLoginPluginRequestPacket packet = (ClientLoginPluginRequestPacket)p;
				this.manager.Send(new ServerLoginPluginResponsePacket(packet.messageId, false));
			});

			// Login (Play)
			this.manager.AddListener(ClientLoginPlayPacket.ID, ClientLoginPlayPacket.STATE, (ClientPacket p) => {
				this.manager.state = State.Play;
			}, true);

			// Keep-Alive
			this.manager.AddListener(ClientKeepAlivePacket.ID, ClientKeepAlivePacket.STATE, (ClientPacket p) => {
				ClientKeepAlivePacket packet = (ClientKeepAlivePacket)p;
				this.manager.Send(new ServerKeepAlivePacket(packet.keepAlive));
			});

			// Handshake
			this.manager.Send(new ServerHandshakePacket(this.protocolVersion, ip, port ?? 25565, State.Login));
			this.manager.state = State.Login;

			// Login Start
			this.manager.Send(new ServerLoginStartPacket(username, uuid));
		}

		public void Disconnect(ClientDisconnectPacket packet) {
			this.manager.Disconnect();
			Debug.LogError($"Disconnected: {packet.reason}");
		}
	}
}