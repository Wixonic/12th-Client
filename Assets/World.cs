using System;
using API;
using SmartNbt.Tags;
using UnityEngine;

public class World : MonoBehaviour {
	public static World current;

	public new Camera camera;
	public GameObject sunlight;

	public Client client;
	public float time;
	public Vector3 position = new(0, 0, 0);
	public Quaternion rotation = new(0, 0, 0, 0);

	public NbtCompound registeryCodec;
	public string dimensionType;
	public string dimensionName;

	public async void Start() {
		World.current = this;

		client = new();
		await client.Connect("localhost", 25565, "Wixi", Guid.Parse("52e485b75156498e8341dd44f4a38908"));

		Light light = this.sunlight.GetComponent<Light>();
		light.type = LightType.Directional;

		client.manager.AddListener(ClientUpdateTimePacket.ID, ClientUpdateTimePacket.STATE, (ClientPacket p) => {
			ClientUpdateTimePacket packet = (ClientUpdateTimePacket)p;
			this.time = packet.time % 24000;
		});

		client.manager.AddListener(ClientSynchronizePositionPacket.ID, ClientSynchronizePositionPacket.STATE, (ClientPacket p) => {
			ClientSynchronizePositionPacket packet = (ClientSynchronizePositionPacket)p;

			this.position = packet.playerPosition;
			this.rotation = packet.playerRotation;

			this.client.manager.Send(new ServerConfirmTeleportationPacket(packet.teleportId));
		});
	}

	public void FixedUpdate() {
		this.camera.transform.SetPositionAndRotation(this.position, this.rotation);
		this.sunlight.transform.rotation = Quaternion.Euler(this.time / 24000 * 360, 0, 0);
	}

	public void OnApplicationQuit() {
		this.client.manager.Disconnect();
	}
}