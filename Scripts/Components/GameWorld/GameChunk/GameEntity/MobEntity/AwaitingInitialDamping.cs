using Unity.Entities;

namespace Components.GameWorld.GameChunk.GameEntity.MobEntity
{
    public struct AwaitingInitialDamping : IComponentData
    {
        public readonly float AwaitingSecondsLeft;

        public AwaitingInitialDamping(float awaitingSecondsLeft)
        {
            AwaitingSecondsLeft = awaitingSecondsLeft;
        }
    }
}