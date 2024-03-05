using Unity.Entities;

namespace Components.GameWorld.GameChunk
{
    public struct ChunkPosition : IComponentData
    {
        public float X;
        public float Z;

        public ChunkPosition(float x, float z)
        {
            X = x;
            Z = z;
        }
    }
}