using Unity.Collections;
using Unity.Entities;

namespace Components.GameWorld.GameChunk.GameEntity.MobEntity
{
    public struct MovementPropertiesShared : ISharedComponentData
    {
        [ReadOnly] public float MoveSpeedMetersPerSec;
        [ReadOnly] public float TurnSpeedDegreesPerSec;
    }
}