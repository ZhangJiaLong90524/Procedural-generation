using Components.GameWorld.GameChunk;
using Unity.Entities;

namespace Components.GameWorld
{
    public struct GenerateGameChunkWaitingBuffer : IBufferElementData
    {
        public ChunkPosition ChunkPosition;
    }
}