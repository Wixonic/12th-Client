using System.Collections.Generic;
using UnityEngine;

public class ChunkRenderer : MonoBehaviour {
    const int SECTION_SIZE = 16;
    const float BLOCK_SIZE = 1f;
    
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();
    
    bool[,,] solidMap = new bool[SECTION_SIZE, SECTION_SIZE, SECTION_SIZE];

    public void BuildChunkMesh(List<List<List<List<int>>>> column, int chunkX, int chunkZ) {
        foreach (var section in column) {
            PreprocessSection(section);
            GenerateSectionMesh(section);
        }
        
        FinalizeMesh();
        transform.position = new Vector3(chunkX * SECTION_SIZE, 0, chunkZ * SECTION_SIZE);
    }

    void PreprocessSection(List<List<List<int>>> section) {
        for (int y = 0; y < SECTION_SIZE; y++) {
            for (int z = 0; z < SECTION_SIZE; z++) {
                for (int x = 0; x < SECTION_SIZE; x++) {
                    int blockId = section[y][z][x];
                    solidMap[x, y, z] = blockId != 0 && IsBlockSolid(blockId);
                }
            }
        }
    }

    bool IsBlockSolid(int blockId) {
        if (API.Registries.blocks.TryGetValue(blockId, out string blockName)) {
            return blockName != "Air" && blockName != "Water" && blockName != "Glass";
        }
        return false;
    }

    void GenerateSectionMesh(List<List<List<int>>> section) {
        for (int y = 0; y < SECTION_SIZE; y++) {
            for (int z = 0; z < SECTION_SIZE; z++) {
                for (int x = 0; x < SECTION_SIZE; x++) {
                    int blockId = section[y][z][x];
                    if (blockId == 0) continue;

                    CheckFace(x, y, z, Direction.Up);
                    CheckFace(x, y, z, Direction.Down);
                    CheckFace(x, y, z, Direction.North);
                    CheckFace(x, y, z, Direction.South);
                    CheckFace(x, y, z, Direction.East);
                    CheckFace(x, y, z, Direction.West);
                }
            }
        }
    }

    void CheckFace(int x, int y, int z, Direction dir) {
        if (!IsBlockOccluded(x, y, z, dir)) {
            AddFace(x, y, z, dir, section[y][z][x]);
        }
    }

    bool IsBlockOccluded(int x, int y, int z, Direction dir) {
        var (dx, dy, dz) = DirectionToOffset(dir);
        int nx = x + dx, ny = y + dy, nz = z + dz;
        
        if (nx < 0 || nx >= SECTION_SIZE || 
            ny < 0 || ny >= SECTION_SIZE || 
            nz < 0 || nz >= SECTION_SIZE) return false;
            
        return solidMap[nx, ny, nz];
    }

    (int, int, int) DirectionToOffset(Direction dir) {
        return dir switch {
            Direction.Up => (0, 1, 0),
            Direction.Down => (0, -1, 0),
            Direction.North => (0, 0, 1),
            Direction.South => (0, 0, -1),
            Direction.East => (1, 0, 0),
            Direction.West => (-1, 0, 0),
            _ => (0, 0, 0)
        };
    }

    Vector3[] GetFaceVertices(Direction dir) {
        return dir switch {
            Direction.Up => new[] {
                new Vector3(0, 1, 0),
                new Vector3(1, 1, 0),
                new Vector3(1, 1, 1),
                new Vector3(0, 1, 1)
            },
            Direction.Down => new[] {
                new Vector3(0, 0, 0),
                new Vector3(0, 0, 1),
                new Vector3(1, 0, 1),
                new Vector3(1, 0, 0)
            },
            Direction.North => new[] {
                new Vector3(1, 0, 1),
                new Vector3(1, 1, 1),
                new Vector3(0, 1, 1),
                new Vector3(0, 0, 1)
            },
            Direction.South => new[] {
                new Vector3(0, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(1, 1, 0),
                new Vector3(1, 0, 0)
            },
            Direction.East => new[] {
                new Vector3(1, 0, 0),
                new Vector3(1, 1, 0),
                new Vector3(1, 1, 1),
                new Vector3(1, 0, 1)
            },
            Direction.West => new[] {
                new Vector3(0, 0, 1),
                new Vector3(0, 1, 1),
                new Vector3(0, 1, 0),
                new Vector3(0, 0, 0)
            },
            _ => null
        };
    }

    void AddFace(int x, int y, int z, Direction dir, int blockId) {
        int vCount = vertices.Count;
        Vector3[] faceVerts = GetFaceVertices(dir);
        
        foreach (Vector3 vert in faceVerts) {
            vertices.Add(new Vector3(x, y, z) + vert);
            uvs.Add(GetTextureCoordinates(blockId, dir));
        }
        
        triangles.AddRange(new[] { vCount, vCount+1, vCount+2, vCount, vCount+2, vCount+3 });
    }

    Vector2 GetTextureCoordinates(int blockId, Direction dir) {
        return new Vector2(0, 0);
    }

    void FinalizeMesh() {
        Mesh mesh = new Mesh {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray()
        };
        
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    enum Direction { Up, Down, North, South, East, West }
}