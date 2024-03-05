using Unity.Collections;
using Unity.Entities;

namespace Components.GameWorld.GameChunk.GameEntity.Generation
{
    public struct GenerationCountLimit : IComponentData
    {
        [ReadOnly] public float Min;
        [ReadOnly] public float Max;
    }
}