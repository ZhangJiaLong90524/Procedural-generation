using Unity.Collections;
using Unity.Entities;

namespace Components.GameWorld
{
    public struct GameWorldGenerationProperties : IComponentData
    {
        [ReadOnly] public byte InitialWorldSize;
        [ReadOnly] public int SideLengthOfChunk;
        [ReadOnly] public int SideLengthOfChunkMinusOne;
        [ReadOnly] public int ChunkSampleGridCellCount;
    }
}