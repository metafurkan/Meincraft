using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using Random = System.Random;

[CreateAssetMenu(menuName = "Meincraft/New Terrain Generator",fileName ="New Terrain Generator")]
public class TerrainGenerator : ScriptableObject
{
    [SerializeField] private int BaseHeight = 64;
    [SerializeField] private int WaterHeight = 64;

    public int WorldSizeInChunks = 64;

    [Space(10)] public int Seed = 1337;
    [Serializable] public class NoiseData
    {
        public FastNoiseLite.NoiseType NoiseType;
        public float Frequency = 0.01f;
        public float Amplitude = 1f;
        public bool Exponential = false;
    }
    public NoiseData[] NoiseDatas;

    private FastNoiseLite[] noises;
    private Random random;

    public void Initialize()
    {
        noises = new FastNoiseLite[NoiseDatas.Length];
        for (int i = 0; i < NoiseDatas.Length; i++)
        {
            noises[i] = new FastNoiseLite(Seed);
            noises[i].SetNoiseType(NoiseDatas[i].NoiseType);
            noises[i].SetFrequency(NoiseDatas[i].Frequency);
        }
        random = new Random(Seed);
    }
    public byte[,,] GetBlocks(Vector2Int chunkStackWorldPosition)
    {
        var result = new byte[Globals.ChunkSize, Globals.ChunkHeight, Globals.ChunkSize];
            
            //Initialize the entire chunk to AIR
            for (int x = 0; x < Globals.ChunkSize; x++)
            {
                for (int y = 0; y < Globals.ChunkHeight; y++)
                {
                    for (int z = 0; z < Globals.ChunkSize; z++)
                    {
                        result[x, y, z] = (byte)BlockType.AIR;
                    }
                }
            }
            for (int x = 0; x < Globals.ChunkSize; x++)
            {
                for (int z = 0; z < Globals.ChunkSize; z++)
                {                
                    int globalXPos = chunkStackWorldPosition.x + x;
                    int globalZPos = chunkStackWorldPosition.y + z;
                    float height = GetHeight(globalXPos, globalZPos);
                    int intHeight = Mathf.FloorToInt(height);
                    
                    // Clamp height to prevent array out of bounds
                    intHeight = Mathf.Clamp(intHeight, 1, Globals.ChunkHeight - 1);

                    for (int y = 0; y < intHeight; y++)
                    {
                        // Bedrock layer
                        if (y == 0)
                        {
                            result[x, y, z] = (byte)BlockType.BEDROCK;
                        }
                        else if (y < 7)
                        {
                            if (((random.NextDouble() * random.NextDouble()) + (result[x, y - 1, z] == (byte)BlockType.BEDROCK ? 0.25f : 0f)) > 0.4f)
                            {
                                result[x, y, z] = (byte)BlockType.BEDROCK;
                            }
                            else
                            {
                                result[x, y, z] = (byte)BlockType.STONE;
                            }
                        }
                        // Dirt layer
                        else if (y >= intHeight - 7 && y < intHeight - 1)
                        {
                            int dirtRange = Mathf.Clamp((int)(random.NextDouble() * 7), 3, 7);
                            if (y >= intHeight - dirtRange)
                            {
                                result[x, y, z] = (byte)BlockType.DIRT;
                            }
                            else
                            {
                                result[x, y, z] = (byte)BlockType.STONE;
                            }
                        }
                        // Surface layer
                        else if (y == intHeight - 1)
                        {
                            result[x, y, z] = (byte)BlockType.GRASS;
                        }
                        // Stone for everything else
                        else
                        {
                            result[x, y, z] = (byte)BlockType.STONE;
                        }
                    }

                    for (int y = 0; y < Globals.ChunkHeight; y++)//Water pass
                    {
                        if (y < WaterHeight && y >= intHeight)
                        {
                            result[x, y, z] = (byte)BlockType.WATER;
                        }
                    }

                    for (int y = 0; y < intHeight; y++)//Tree pass
                    {
                        if (y == intHeight - 1 && result[x, y + 1, z] == (byte) BlockType.AIR)
                        {
                            if (x > 2 && z > 2 && x < Globals.ChunkSize - 2 && z < Globals.ChunkSize - 2)
                            {
                                if (random.Next(64) == 0)
                                {
                                    int trunkHeight = 5;
                                    for (int i = 1; i <= trunkHeight; i++)
                                    {
                                        result[x, y + i, z] = (byte)BlockType.OAK_LOG;
                                    }
                                    for (int i = 4; i <= 6; i++)
                                    {
                                        int radius = 6 - i;
                                        for (int width = -radius; width <= radius; width++)
                                        {
                                            for (int depth = -radius; depth <= radius; depth++)
                                            {
                                                if(i<trunkHeight && width == 0 && depth == 0) continue;
                                                result[x + width, y + i, z + depth] = (byte)BlockType.OAK_LEAVES;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return result;
    }
    public float GetHeight(int x, int z)
    {
        float result = BaseHeight;

        for (int i = 0; i < noises.Length; i++)
        {
            float noise = noises[i].GetNoise(x, z);
            noise = (noise + 1) / 2f; //normalize
        
            if (NoiseDatas[i].Exponential) 
            {
                noise = (Mathf.Exp(noise) - 1) / (Mathf.Exp(1) - 1);//Normalize the exp func result back to 0-1
            }
        
            result += noise * NoiseDatas[i].Amplitude;
        }
    
        return result;
    }
}
