using API;
using UnityEngine;

public class World : MonoBehaviour {
	public new Camera camera;
	public GameObject sunlight;

	Client client;
	float time;
	Vector3 position = new(0, 0, 0);
	Quaternion rotation = new(0, 0, 0, 0);

	public async void Start() {
		client = new();
		await client.Connect(null, null, null, null);

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