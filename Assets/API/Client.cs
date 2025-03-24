using SmartNbt.Tags;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace API {
	public class Client {
		public readonly int protocolVersion = 767;
		public readonly Manager manager;

		public Client() => manager = new();

		public async Task Connect(string ip, ushort? port, string username, Guid? uuid) {
			await manager.Connect(ip, port ?? 25565);

			// Handshake
			manager.Send(new ServerHandshakePacket(protocolVersion, ip, port ?? 25565, State.Login));

			// Login
			manager.state = State.Login;
			manager.Send(new ServerLoginStartPacket(username, uuid));

			manager.AddListener(ClientLoginDisconnectPacket.ID, ClientLoginDisconnectPacket.STATE, (ClientPacket p) => {
				ClientLoginDisconnectPacket packet = (ClientLoginDisconnectPacket)p;

				Disconnect(packet.reason);
			}, true);

			manager.AddListener(ClientLoginSuccessPacket.ID, ClientLoginSuccessPacket.STATE, (ClientPacket p) => {
				ClientLoginSuccessPacket packet = (ClientLoginSuccessPacket)p;

				manager.Send(new ServerLoginAcknowledgedPacket());
				manager.state = State.Config;
			}, true);

			// Configuration
			manager.AddListener(ClientConfigDisconnectPacket.ID, ClientConfigDisconnectPacket.STATE, (ClientPacket p) => {
				ClientConfigDisconnectPacket packet = (ClientConfigDisconnectPacket)p;

				Disconnect(packet.reason.Contains("text") ? packet.reason.Get<NbtString>("text")?.Value ?? "Unknown reason" : "Unknown reason");
			}, true);
			
			manager.AddListener(ClientConfigFinishPacket.ID, ClientConfigFinishPacket.STATE, (ClientPacket p) => {
				manager.Send(new ServerConfigAcknowledgedPacket());
				manager.state = State.Play;
			}, true);

			manager.AddListener(ClientConfigKeepAlivePacket.ID, ClientConfigKeepAlivePacket.STATE, (ClientPacket p) => {
				ClientConfigKeepAlivePacket packet = (ClientConfigKeepAlivePacket)p;

				manager.Send(new ServerConfigKeepAlivePacket(packet.id));
			});

			manager.AddListener(ClientConfigPingPacket.ID, ClientConfigPingPacket.STATE, (ClientPacket p) => {
				ClientConfigPingPacket packet = (ClientConfigPingPacket)p;

				manager.Send(new ServerConfigPongPacket(packet.pingId));
			});

			manager.AddListener(ClientConfigKnownPacksPacket.ID, ClientConfigKnownPacksPacket.STATE, (ClientPacket p) => {
				ClientConfigKnownPacksPacket packet = (ClientConfigKnownPacksPacket)p;
				
				foreach (var pack in packet.knownPacks) Debug.Log($"Pack {pack.Item1}:{pack.Item2} v{pack.Item3}");
				manager.Send(new ServerConfigKnownPacksPacket());
			}, true);

			// Play packets
			manager.AddListener(ClientPlayDisconnectPacket.ID, ClientPlayDisconnectPacket.STATE, (ClientPacket p) => {
				ClientPlayDisconnectPacket packet = (ClientPlayDisconnectPacket)p;
				
				Disconnect(packet.reason.Contains("text") ? packet.reason.Get<NbtString>("text")?.Value ?? "Unknown reason" : "Unknown reason");
			}, true);

			manager.AddListener(ClientPlayKeepAlivePacket.ID, ClientPlayKeepAlivePacket.STATE, (ClientPacket p) => {
				ClientPlayKeepAlivePacket packet = (ClientPlayKeepAlivePacket)p;

				manager.Send(new ServerPlayKeepAlivePacket(packet.keepAliveId));
			});

			manager.AddListener(ClientPlayPingPacket.ID, ClientPlayPingPacket.STATE, (ClientPacket p) => {
				ClientPlayPingPacket packet = (ClientPlayPingPacket)p;

				manager.Send(new ServerPlayPongPacket(packet.pingId));
			});
		}

		public void Disconnect(string reason) {
			manager.Disconnect();
			Debug.LogError($"Disconnected: {reason}");
		}
	}
}