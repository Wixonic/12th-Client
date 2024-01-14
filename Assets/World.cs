using System;
using System.Collections.Generic;
using System.Linq;
using API;
using SmartNbt.Tags;
using UnityEngine;

public class World : MonoBehaviour {
	public static World current;

	public new Camera camera;
	public GameObject sunlight;
	public GameObject map;

	public Client client;
	public float time;
	public Vector3 position = new(0, 0, 0);
	public Quaternion rotation = new(0, 0, 0, 0);

	public NbtCompound registeryCodec;
	public int dimensionId;
	public string dimensionName;
	public string dimensionType;

	private readonly List<ClientChunkDataPacket> loadChunkColumnQueue = new();

	public async void Start() {
		World.current = this;

		Prefabs.Load();

		client = new();
		await client.Connect("server.wixonic.fr", 25565, "12th_Client", Guid.Parse("52e485b75156498e8341dd44f4a38908"));

		Light light = this.sunlight.GetComponent<Light>();
		light.type = LightType.Directional;

		client.manager.AddListener(ClientLoginPlayPacket.ID, ClientLoginPlayPacket.STATE, (ClientPacket p) => {
			ClientLoginPlayPacket packet = (ClientLoginPlayPacket)p;

			this.registeryCodec = packet.registeryCodec;
			this.dimensionName = packet.dimensionName;
			this.dimensionType = packet.dimensionType;
			this.dimensionId = packet.dimensions.IndexOf(this.dimensionType);
		}, true);

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

		client.manager.AddListener(ClientChunkDataPacket.ID, ClientChunkDataPacket.STATE, (ClientPacket p) => {
			ClientChunkDataPacket packet = (ClientChunkDataPacket)p;
			this.loadChunkColumnQueue.Add(packet);
		});
	}

	public void LoadChunkColumn(int chunkX, int chunkZ, List<List<List<List<int>>>> column) {
		GameObject chunkColumn = Instantiate(Prefabs.Get("Chunk"));
		chunkColumn.name = $"chunkColumn_{chunkX}-{chunkZ}";
		chunkColumn.transform.SetParent(this.map.transform);

		for (int chunkY = 0; chunkY < column.Count; ++chunkY) {
			GameObject chunkSection = Instantiate(Prefabs.Get("Chunk"));
			chunkSection.name = $"chunkSection_{chunkY}";
			chunkSection.transform.SetParent(chunkColumn.transform);

			var blocks = column[chunkY];

			for (int y = 0; y < blocks.Count; ++y) {
				for (int z = 0; z < blocks[y].Count; ++z) {
					for (int x = 0; x < blocks[y][z].Count; ++x) {
						int id = blocks[y][z][x];
						string registeryId = Registeries.blocks.GetValueOrDefault(id, "Error");

						if (registeryId != "Air") {
							GameObject prefab = Prefabs.Get($"Blocks/{registeryId}");
							GameObject block = Instantiate(prefab);

							block.name = $"{registeryId}_{x}-{y}-{z}";

							block.transform.SetParent(chunkSection.transform);
							block.transform.position = new(x, y, z);
						}
					}
				}
			}

			chunkSection.transform.position = new(0, chunkY * 16, 0);
		}

		chunkColumn.transform.position = new(chunkX * 16, 0, chunkZ * 16);
	}

	public void FixedUpdate() {
		this.camera.transform.SetPositionAndRotation(this.position, this.rotation);
		this.sunlight.transform.rotation = Quaternion.Euler(this.time / 24000 * 360, 0, 0);

		if (this.loadChunkColumnQueue.Count > 0) {
			ClientChunkDataPacket packet = this.loadChunkColumnQueue.First();
			this.LoadChunkColumn(packet.chunkX, packet.chunkZ, packet.column);
			this.loadChunkColumnQueue.Remove(packet);
		}
	}

	public void OnApplicationQuit() {
		this.client.manager.Disconnect();
	}
}