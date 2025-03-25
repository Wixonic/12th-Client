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

	public void LoadChunkColumn(int chunkX, int chunkZ, List<List<List<List<int>>>> column) {
        GameObject chunkObj = new GameObject($"Chunk_{chunkX}_{chunkZ}");
        chunkObj.transform.parent = map.transform;
        chunkObj.transform.position = new Vector3(chunkX * 16, 0, chunkZ * 16);

        MeshFilter meshFilter = chunkObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = chunkObj.AddComponent<MeshRenderer>();
        meshRenderer.materials = new Material[0];

        ChunkMeshData meshData = new ChunkMeshData();
        int[,,] blocks = Convert4DTo3D(column);

        ProcessDirection(Direction.Top, blocks, meshData);
        ProcessDirection(Direction.Bottom, blocks, meshData);
        ProcessDirection(Direction.North, blocks, meshData);
        ProcessDirection(Direction.South, blocks, meshData);
        ProcessDirection(Direction.East, blocks, meshData);
        ProcessDirection(Direction.West, blocks, meshData);

        if (meshData.IsEmpty()) {
            GameObject.Destroy(chunkObj);
            return;
        }

        var (mesh, materials) = meshData.CreateMesh();
        if (mesh.vertexCount == 0) {
            GameObject.Destroy(chunkObj);
            return;
        }

        meshFilter.mesh = mesh;

        List<Material> materialList = new List<Material>();
        foreach (string matName in materials) {
            Material mat = Resources.Load<Material>($"Blocks/{matName}");
            materialList.Add(mat != null ? mat : Resources.Load<Material>("Blocks/Error"));
        }

        meshRenderer.materials = materialList.ToArray();
    }

    private int[,,] Convert4DTo3D(List<List<List<List<int>>>> column) {
        int[,,] blocks = new int[16, 16 * 24, 16];

        for (int s = 0; s < column.Count; s++) {
            for (int y = 0; y < column[s].Count; y++) {
                for (int z = 0; z < 16; z++) {
                    if (y < column[s].Count && z < column[s][y].Count) {
                        for (int x = 0; x < 16; x++) {
                            if (x < column[s][y][z].Count) {
                                int globalY = s * 16 + y;
                                blocks[x, globalY, z] = column[s][y][z][x];
                            }
                        }
                    }
                }
            }
        }

        return blocks;
    }

    private void ProcessDirection(Direction dir, int[,,] blocks, ChunkMeshData meshData) {
        switch (dir) {
            case Direction.Top:
                ProcessLayer((x, y, z) => y + 1 < 16 * 24 && blocks[x, y + 1, z] != blocks[x, y, z], (x, y, z) => new Vector3[] {
                        new(x, y + 1, z),
                        new(x + 1, y + 1, z),
                        new(x + 1, y + 1, z + 1),
                        new(x, y + 1, z + 1)
                    }, blocks, meshData);
                break;

            case Direction.Bottom:
                ProcessLayer((x, y, z) => y - 1 >= 0 && blocks[x, y - 1, z] != blocks[x, y, z], (x, y, z) => new Vector3[] {
                        new(x, y, z + 1),
                        new(x + 1, y, z + 1),
                        new(x + 1, y, z),
                        new(x, y, z)
                    }, blocks, meshData);
                break;

            case Direction.North:
                ProcessLayer((x, y, z) => z + 1 < 16 && blocks[x, y, z + 1] != blocks[x, y, z], (x, y, z) => new Vector3[] {
                        new(x + 1, y, z + 1),
                        new(x + 1, y + 1, z + 1),
                        new(x, y + 1, z + 1),
                        new(x, y, z + 1)
                    }, blocks, meshData);
                break;

            case Direction.South:
                ProcessLayer((x, y, z) => z - 1 >= 0 && blocks[x, y, z - 1] != blocks[x, y, z], (x, y, z) => new Vector3[] {
                        new(x, y, z),
                        new(x, y + 1, z),
                        new(x + 1, y + 1, z),
                        new(x + 1, y, z)
                    }, blocks, meshData);
                break;

            case Direction.East:
                ProcessLayer((x, y, z) => x + 1 < 16 && blocks[x + 1, y, z] != blocks[x, y, z], (x, y, z) => new Vector3[] {
                        new(x + 1, y, z),
                        new(x + 1, y, z + 1),
                        new(x + 1, y + 1, z + 1),
                        new(x + 1, y + 1, z)
                    }, blocks, meshData);
                break;

            case Direction.West:
                ProcessLayer((x, y, z) => x - 1 >= 0 && blocks[x - 1, y, z] != blocks[x, y, z], (x, y, z) => new Vector3[] {
                        new(x, y, z + 1),
                        new(x, y, z),
                        new(x, y + 1, z),
                        new(x, y + 1, z + 1)
                    }, blocks, meshData);
                break;
        }
    }

    private void ProcessLayer(System.Func<int, int, int, bool> isFaceVisible, System.Func<int, int, int, Vector3[]> getVertices, int[,,] blocks, ChunkMeshData meshData) {
        for (int x = 0; x < 16; x++){
            for (int y = 0; y < 16 * 24; y++) {
                for (int z = 0; z < 16; z++) {
                    int currentBlock = blocks[x, y, z];
                    if (currentBlock == 0) continue;
                    if (!isFaceVisible(x, y, z)) continue;

                    // Greedy meshing in X direction
                    int width = 1;
                    while (x + width < 16 &&  blocks[x + width, y, z] == currentBlock && isFaceVisible(x + width, y, z)) {
                        width++;
                    }

                    // Greedy meshing in Z direction
                    int depth = 1;
                    bool validDepth = true;
                    while (z + depth < 16 && validDepth) {
                        for (int w = 0; w < width; w++) {
                            if (blocks[x + w, y, z + depth] != currentBlock || !isFaceVisible(x + w, y, z + depth))  {
                                validDepth = false;
                                break;
                            }
                        }

                        if (validDepth) depth++;
                    }

                    Vector3[] verts = new Vector3[4];
                    verts[0] = getVertices(x, y, z)[0];
                    verts[1] = getVertices(x + width - 1, y, z)[1];
                    verts[2] = getVertices(x + width - 1, y, z + depth - 1)[2];
                    verts[3] = getVertices(x, y, z + depth - 1)[3];

                    string materialName = GetBlockMaterial(currentBlock);
                    meshData.AddFace(verts, materialName);

                    for (int w = 0; w < width; w++) for (int d = 0; d < depth; d++) blocks[x + w, y, z + d] = -1;
                }
            }
        }
    }

    private string GetBlockMaterial(int blockId) {
        if (Registries.blocks.TryGetValue(blockId, out string blockName)) return blockName;
        return "Error";
    }

    private enum Direction { Top, Bottom, North, South, East, West }
}

public class ChunkMeshData {
    private List<string> materialOrder = new List<string>();
    private Dictionary<string, List<Vector3>> verticesByMaterial = new Dictionary<string, List<Vector3>>();
    private Dictionary<string, List<int>> trianglesByMaterial = new Dictionary<string, List<int>>();
    private Dictionary<string, List<Vector2>> uvsByMaterial = new Dictionary<string, List<Vector2>>();

    public void AddFace(Vector3[] verts, string materialName) {
        if (!verticesByMaterial.ContainsKey(materialName)) {
            materialOrder.Add(materialName);
            verticesByMaterial[materialName] = new List<Vector3>();
            trianglesByMaterial[materialName] = new List<int>();
            uvsByMaterial[materialName] = new List<Vector2>();
        }

        int vertexIndex = verticesByMaterial[materialName].Count;
        
        // Add vertices
        verticesByMaterial[materialName].AddRange(verts);
        
        // Add triangles
        trianglesByMaterial[materialName].AddRange(new int[] {
            vertexIndex, vertexIndex + 1, vertexIndex + 2,
            vertexIndex, vertexIndex + 2, vertexIndex + 3
        });
        
        // Simple UV mapping (assumes 1x1 texture)
        uvsByMaterial[materialName].AddRange(new Vector2[] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        });
    }

    public (Mesh mesh, List<string> materials) CreateMesh() {
        Mesh mesh = new Mesh();
        List<Vector3> allVertices = new List<Vector3>();
        List<Vector2> allUVs = new List<Vector2>();
        List<string> materials = new List<string>(materialOrder);

        mesh.subMeshCount = materials.Count;

        for (int i = 0; i < materials.Count; i++) {
            string matName = materials[i];
            List<Vector3> verts = verticesByMaterial[matName];
            List<int> tris = trianglesByMaterial[matName];
            List<Vector2> uv = uvsByMaterial[matName];

            int vertexOffset = allVertices.Count;
            allVertices.AddRange(verts);
            allUVs.AddRange(uv);

            mesh.SetTriangles(tris.Select(t => t + vertexOffset).ToArray(), i);
        }

        mesh.vertices = allVertices.ToArray();
        mesh.uv = allUVs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return (mesh, materials);
    }

    public bool IsEmpty() {
        return verticesByMaterial.Count == 0;
    }
}