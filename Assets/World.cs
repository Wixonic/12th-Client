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

	public Dictionary<string, NbtCompound> registryCodec;
	public string dimensionId;
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

			registryCodec = packet.entries;
		}, true);

		client.manager.AddListener(ClientPlayChunkBatchFinishedPacket.ID, ClientPlayChunkBatchFinishedPacket.STATE, (ClientPacket p) => client.manager.Send(new ServerPlayChunkBatchReceivedPacket(25)));

		client.manager.AddListener(ClientPlayLoginPacket.ID, ClientPlayLoginPacket.STATE, (ClientPacket p) => {
			ClientPlayLoginPacket packet = (ClientPlayLoginPacket)p;

			dimensionName = packet.dimensionName;
			dimensionType = packet.dimensionType;
		}, true);

		client.manager.AddListener(ClientPlayChunkDataPacket.ID, ClientPlayChunkDataPacket.STATE, (ClientPacket p) => {
			ClientPlayChunkDataPacket packet = (ClientPlayChunkDataPacket)p;

			loadChunkColumnQueue.Add(packet);
		});

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
		GameObject chunkColumn = Instantiate(Prefabs.Get("Chunk"));
		chunkColumn.name = $"chunkColumn_{chunkX}-{chunkZ}";
		chunkColumn.transform.SetParent(map.transform);

		for (int chunkY = 0; chunkY < column.Count; ++chunkY) {
			GameObject chunkSection = Instantiate(Prefabs.Get("Chunk"));
			chunkSection.name = $"chunkSection_{chunkY}";
			chunkSection.transform.SetParent(chunkColumn.transform);

			var blocks = column[chunkY];

			for (int y = 0; y < blocks.Count; ++y) {
				for (int z = 0; z < blocks[y].Count; ++z) {
					for (int x = 0; x < blocks[y][z].Count; ++x) {
						int id = blocks[y][z][x];
						string registryId = Registries.blocks.GetValueOrDefault(id, "Error");

						if (registryId != "Air") {
							GameObject prefab = Prefabs.Get($"Blocks/{registryId}");
							GameObject block = Instantiate(prefab);

							block.name = $"{registryId}_{x}-{y}-{z}";

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

	private string FormatNbt(NbtTag tag, int indent = 0)
{
    StringBuilder sb = new StringBuilder();
    string indentStr = new string(' ', indent * 2);

    if (tag is NbtCompound compound)
    {
        sb.AppendLine($"{indentStr}{{");
        foreach (var child in compound)
        {
            sb.Append($"{indentStr}  {child.Name}: ");
            sb.AppendLine(FormatNbt(child, indent + 1));
        }
        sb.Append($"{indentStr}}}");
    }
    else if (tag is NbtList list)
    {
        sb.AppendLine($"{indentStr}[");
        foreach (var item in list)
        {
            sb.AppendLine(FormatNbt(item, indent + 1));
        }
        sb.Append($"{indentStr}]");
    }
    else
    {
        sb.AppendLine($"{indentStr}{tag.ToString()}");
    }

    return sb.ToString();
}
}