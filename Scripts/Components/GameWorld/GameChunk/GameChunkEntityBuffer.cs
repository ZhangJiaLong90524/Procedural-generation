using Unity.Entities;

namespace Components.GameWorld.GameChunk
{
    public struct GameChunkEntityBuffer : IBufferElementData
    {
        public Entity Entity;
    }
}