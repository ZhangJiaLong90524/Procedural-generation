using Unity.Entities;

namespace Components.GameWorld.GameChunk.GameEntity
{
    [ChunkSerializable]
    public struct ObservedGameEntitiesDataBuffer : IBufferElementData
    {
        public Entity Entity;
        public float SecondsSinceLastObservation;

        public ObservedGameEntitiesDataBuffer(ObservedGameEntitiesDataBuffer observedGameEntitiesDataBuffer,
            float secondsSinceLastObservation)
        {
            Entity = observedGameEntitiesDataBuffer.Entity;
            SecondsSinceLastObservation = secondsSinceLastObservation;
        }

        public ObservedGameEntitiesDataBuffer(Entity entity)
        {
            Entity = entity;
            SecondsSinceLastObservation = 0;
        }
    }
}