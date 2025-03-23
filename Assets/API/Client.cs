using System;
using System.Threading.Tasks;
using UnityEngine;

namespace API {
	public class Client {
		public readonly int protocolVersion = 767;
		public readonly Manager manager;

		public Client() => this.manager = new();

		public async Task Connect(string ip, ushort? port, string username, Guid? uuid) {
			await this.manager.Connect(ip, port ?? 25565);

			// Disconnects
			this.manager.AddListener(ClientDisconnectLoginPacket.ID, ClientDisconnectLoginPacket.STATE, (ClientPacket p) => {
				ClientDisconnectLoginPacket packet = (ClientDisconnectLoginPacket)p;
				this.Disconnect(packet.reason);
			}, true);

			this.manager.AddListener(ClientDisconnectConfigurationPacket.ID, ClientDisconnectConfigurationPacket.STATE, (ClientPacket p) => {
				ClientDisconnectConfigurationPacket packet = (ClientDisconnectConfigurationPacket)p;
				this.Disconnect("Unknown reason");
			}, true);

			this.manager.AddListener(ClientDisconnectPlayPacket.ID, ClientDisconnectPlayPacket.STATE, (ClientPacket p) => {
				ClientDisconnectPlayPacket packet = (ClientDisconnectPlayPacket)p;
				this.Disconnect(packet.reason);
			}, true);

			// Login
			this.manager.AddListener(ClientEncryptionRequestPacket.ID, ClientEncryptionRequestPacket.STATE, (ClientPacket p) => Debug.LogError("The server is in online mode"), true);

			this.manager.AddListener(ClientLoginSuccessPacket.ID, ClientLoginSuccessPacket.STATE, (ClientPacket p) => {
				ClientLoginSuccessPacket packet = (ClientLoginSuccessPacket)p;

				this.manager.Send(new ServerLoginAcknowledgedPacket());
				this.manager.state = State.Configuration;
			}, true);

			this.manager.AddListener(ClientSetCompressionPacket.ID, ClientSetCompressionPacket.STATE, (ClientPacket p) => {
				ClientSetCompressionPacket packet = (ClientSetCompressionPacket)p;
				this.manager.compression = packet.threshold;
				Debug.LogWarning($"The server asks for compression: {packet.threshold}");
			}, true);

			// Configuration

			// TODO: "Clientbound Known Packs", answer with "Serverbound Known Packs"

			// TODO: "Finish Configuration", answer with "Acknowledge Finish Configuration" and set state to Play

			// Keep-Alive
			// TODO: Add Clientbound Keep Alive when config also
			this.manager.AddListener(ClientKeepAlivePacket.ID, ClientKeepAlivePacket.STATE, (ClientPacket p) => {
				ClientKeepAlivePacket packet = (ClientKeepAlivePacket)p;
				this.manager.Send(new ServerKeepAlivePacket(packet.keepAlive));
			});

			// Ping
			// TODO: Add Ping for Config and Play

			// Handshake
			this.manager.Send(new ServerHandshakePacket(this.protocolVersion, ip, port ?? 25565, State.Login));

			// Login
			this.manager.state = State.Login;
			this.manager.Send(new ServerLoginStartPacket(username, uuid));
		}

		public void Disconnect(string reason) {
			this.manager.Disconnect();
			Debug.LogError($"Disconnected: {reason}");
		}
	}
}