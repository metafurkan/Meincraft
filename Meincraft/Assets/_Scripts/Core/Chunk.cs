using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Profiling;
using UnityEngine;

public enum ChunkState {
    NotLoaded,
    LoadingData,
    DataReady,
    GeneratingMesh,
    Complete
}
public class Chunk
{
    public ChunkState State;
    
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    public GameObject GameObj;
    private BlockLibrary _blockLibrary;
    
    private Dictionary<Globals.Direction, Chunk> _chunkNeighbors = new Dictionary<Globals.Direction, Chunk>();
    public Dictionary<Globals.Direction, Chunk> ChunkNeighbors { get { return _chunkNeighbors; } }
    
    private ChunkData _chunkData;
    public ChunkData Data => _chunkData;
    
    
    private CancellationTokenSource chunkDataCts;
    private CancellationTokenSource chunkMeshingCts;
    
    public Chunk(ChunkData data, GameObject obj, BlockLibrary blockLibrary)
    {
        _chunkData = data;
        GameObj = obj;
        _blockLibrary = blockLibrary;
        
        meshFilter = GameObj.GetComponent<MeshFilter>();
        meshCollider = GameObj.GetComponent<MeshCollider>();

        chunkDataCts = new CancellationTokenSource();
        chunkMeshingCts = new CancellationTokenSource();
        State = ChunkState.NotLoaded;
    }

    public override bool Equals(object obj)
    {
        if (obj is Chunk chunk)
        {
            return chunk.Data.ChunkPosition == _chunkData.ChunkPosition;
        }
        return default;
    }
    
    public async Task<TerrainMeshData> LoadMeshDataAsync(BlockLibrary blockLibrary)
    {
        if (chunkMeshingCts.IsCancellationRequested) return null;

        State = ChunkState.GeneratingMesh;
        var res = await WorldMeshGenerator.BuildMesh(this, blockLibrary);
        State = ChunkState.Complete;
        return res;
    }
    public void GenerateMeshData()
    {
  
    }
    
    public void Load()
    {
        //OnMeshDataReceived();
    }

    public void UnLoad()
    {
        meshFilter.mesh.Clear();
        meshFilter.mesh = null;
        meshCollider.sharedMesh = null;
    }


    public void OnMeshDataReceived(TerrainMeshData meshData)
    {
        Profiler.BeginSample("Apply Mesh Data");
        List<CombineInstance> combineInstances = new List<CombineInstance>();
        combineInstances.Add(new CombineInstance()
        {
            mesh = meshData.SolidMeshBuilder.Build(),
            subMeshIndex = 0
        });
        combineInstances.Add(new CombineInstance()
        {
            mesh = meshData.WaterMeshBuilder.Build(),
            subMeshIndex = 0
        });
        Mesh finalMesh = new Mesh
        {
            subMeshCount = 2
        };
        finalMesh.CombineMeshes(combineInstances.ToArray(), false, false);
        
        meshFilter.mesh = finalMesh;
        //meshCollider.sharedMesh = finalMesh;
        Profiler.EndSample();
    }
    public void Clear()
    {
        meshFilter.mesh.Clear();
    }

    public void SetNeighbor(Globals.Direction dir, Chunk c)
    {
        _chunkNeighbors[dir] = c;
    }

    public void UpdateNeighbors(int x, int z)
    {
        if (x == 0 && _chunkNeighbors.TryGetValue(Globals.Direction.LEFT, out var leftChunk))
        {
            if(!World.Instance.CheckCoordIsInWorldBorders(leftChunk.Data.ChunkPosition.x ,0 ,leftChunk.Data.ChunkPosition.y)) return;
            leftChunk.Clear();
            leftChunk.GenerateMeshData();
            leftChunk.Load();
        }
        else if (x == Globals.ChunkSize - 1 && _chunkNeighbors.TryGetValue(Globals.Direction.RIGHT, out var rightChunk))
        {
            if(!World.Instance.CheckCoordIsInWorldBorders(rightChunk.Data.ChunkPosition.x ,0 ,rightChunk.Data.ChunkPosition.y)) return;
            rightChunk.Clear();
            rightChunk.GenerateMeshData();
            rightChunk.Load();
        }
    
        if (z == 0 && _chunkNeighbors.TryGetValue(Globals.Direction.BACK, out var backChunk))
        {
            if(!World.Instance.CheckCoordIsInWorldBorders(backChunk.Data.ChunkPosition.x ,0 ,backChunk.Data.ChunkPosition.y)) return;
            backChunk.Clear();
            backChunk.GenerateMeshData();
            backChunk.Load();
        }
        else if (z == Globals.ChunkSize - 1 && _chunkNeighbors.TryGetValue(Globals.Direction.FRONT, out var frontChunk))
        {
            if(!World.Instance.CheckCoordIsInWorldBorders(frontChunk.Data.ChunkPosition.x ,0 ,frontChunk.Data.ChunkPosition.y)) return;
            frontChunk.Clear();
            frontChunk.GenerateMeshData();
            frontChunk.Load();
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(GameObj.transform.position.x + Globals.ChunkSize / 2, Globals.ChunkSize, GameObj.transform.position.z + Globals.ChunkSize / 2), new Vector3(1, Globals.ChunkHeight/Globals.ChunkSize, 1) * Globals.ChunkSize);
    }
}
