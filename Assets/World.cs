using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

	public Dictionary<string, Dictionary<string, NbtCompound>> registries = new();
	public string dimensionName;
	public int dimensionType;

	private readonly List<ClientPlayChunkDataPacket> loadChunkColumnQueue = new();

	public async void Start() {
		World.current = this;

		Prefabs.Load();

		client = new();
		await client.Connect("server.wixonic.fr", 25565, "12th_Client_2", Guid.NewGuid());

		Light light = sunlight.GetComponent<Light>();
		light.type = LightType.Directional;

		client.manager.AddListener(ClientConfigRegistryDataPacket.ID, ClientConfigRegistryDataPacket.STATE, (ClientPacket p) => {
			ClientConfigRegistryDataPacket packet = (ClientConfigRegistryDataPacket)p;

			registries[packet.registryId] = packet.entries;
			Debug.Log($"Registry n°{registries.Keys.ToArray().Length}, \"{packet.registryId}\" added.");
		});

		client.manager.AddListener(ClientPlayChunkBatchFinishedPacket.ID, ClientPlayChunkBatchFinishedPacket.STATE, (ClientPacket p) => client.manager.Send(new ServerPlayChunkBatchReceivedPacket(25)));

		client.manager.AddListener(ClientPlayLoginPacket.ID, ClientPlayLoginPacket.STATE, (ClientPacket p) => {
			ClientPlayLoginPacket packet = (ClientPlayLoginPacket)p;

			dimensionName = packet.dimensionName;
			dimensionType = packet.dimensionType;
		}, true);

		client.manager.AddListener(ClientPlayChunkDataPacket.ID, ClientPlayChunkDataPacket.STATE, (ClientPacket p) => {
			ClientPlayChunkDataPacket packet = (ClientPlayChunkDataPacket)p;

			loadChunkColumnQueue.Add(packet);
		}, true);

		client.manager.AddListener(ClientPlayUpdateTimePacket.ID, ClientPlayUpdateTimePacket.STATE, (ClientPacket p) => {
			ClientPlayUpdateTimePacket packet = (ClientPlayUpdateTimePacket)p;
			time = packet.time % 24000;
		});

		client.manager.AddListener(ClientPlaySynchronizePositionPacket.ID, ClientPlaySynchronizePositionPacket.STATE, (ClientPacket p) => {
			ClientPlaySynchronizePositionPacket packet = (ClientPlaySynchronizePositionPacket)p;

			position = packet.playerPosition;
			rotation = packet.playerRotation;

			client.manager.Send(new ServerPlayConfirmTeleportationPacket(packet.teleportId));
		});
	}

	public void LoadChunkColumn(int chunkX, int chunkZ, List<List<List<List<int>>>> column) {
		Material atlasMaterial = Prefabs.GetMaterial("BlockAtlas");
		GameObject chunk = new GameObject($"Chunk_{chunkX}_{chunkZ}");
		
		MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
		meshRenderer.material = atlasMaterial;

		List<CombineInstance> combines = new List<CombineInstance>();

		foreach (var section in column) {
			for (int y = 0; y < section.Count; y++) {
				for (int z = 0; z < section[y].Count; z++) {
					for (int x = 0; x < section[y][z].Count; x++) {
						int blockId = section[y][z][x];
						if (Registries.blocks.TryGetValue(blockId, out string blockName)) {
							AddBlockToCombine(blockName, x, y, z, ref combines);
						}
					}
				}
			}
		}

		Mesh mesh = new Mesh();
		mesh.CombineMeshes(combines.ToArray());
		meshFilter.mesh = mesh;
		chunk.AddComponent<MeshCollider>();
	}

	void AddBlockToCombine(string blockName, int x, int y, int z, ref List<CombineInstance> combines) {
		GameObject prefab = Prefabs.GetPrefab($"Blocks/{blockName}");
		if (!prefab) return;

		MeshFilter prefabFilter = prefab.GetComponent<MeshFilter>();
		if (!prefabFilter) return;

		CombineInstance combine = new CombineInstance {
			mesh = prefabFilter.sharedMesh,
			transform = Matrix4x4.TRS(
				new Vector3(x, y, z), 
				Quaternion.identity, 
				Vector3.one
			)
		};
		
		combines.Add(combine);
	}

	public void FixedUpdate() {
		camera.transform.SetPositionAndRotation(position, rotation);
		sunlight.transform.rotation = Quaternion.Euler(time / 24000 * 360, 0, 0);

		if (loadChunkColumnQueue.Count > 0) {
			ClientPlayChunkDataPacket packet = loadChunkColumnQueue.First();
			LoadChunkColumn(packet.chunkX, packet.chunkZ, packet.column);
			loadChunkColumnQueue.Remove(packet);
		}
	}

	public void OnApplicationQuit() {
		if (client?.manager != null) client.manager.Disconnect();
	}
}