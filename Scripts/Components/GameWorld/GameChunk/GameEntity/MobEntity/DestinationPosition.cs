using Unity.Entities;
using Unity.Mathematics;

namespace Components.GameWorld.GameChunk.GameEntity.MobEntity
{
    public struct DestinationPosition : IComponentData
    {
        public float3 Value;
    }
}