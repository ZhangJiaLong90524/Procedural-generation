using Unity.Collections;
using Unity.Entities;

namespace Components.GameWorld.GameChunk.GameEntity.Generation
{
    public struct EntityPrefab : IComponentData
    {
        [ReadOnly] public Entity Entity;
    }
}