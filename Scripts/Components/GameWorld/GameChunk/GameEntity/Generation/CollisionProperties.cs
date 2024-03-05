using Unity.Collections;
using Unity.Entities;

namespace Components.GameWorld.GameChunk.GameEntity.Generation
{
    public struct CollisionProperties : IComponentData
    {
        [ReadOnly] public float CollisionRadius;
        [ReadOnly] public float ApproximateCollisionSquareSideLengthHalf;
    }
}