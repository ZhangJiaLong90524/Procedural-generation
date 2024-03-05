using Unity.Collections;
using Unity.Entities;

namespace Components.GameWorld.GameChunk.GameEntity
{
    public struct AverageErasureInteractionCountShared : ISharedComponentData
    {
        [ReadOnly] public uint Value;
    }
}