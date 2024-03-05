using Components.GameWorld;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class GameWorldGenerationPropertiesAuthoring : MonoBehaviour
    {
        [Range(1, byte.MaxValue)] public byte initialWorldSizeInChunk = 1;
        [Range(8, 128)] public byte sideLengthOfChunk = 32;
    }

    public class GameWorldGenerationPropertiesBaker : Baker<GameWorldGenerationPropertiesAuthoring>
    {
        public override void Bake(GameWorldGenerationPropertiesAuthoring authoring)
        {
            var gameWorldEntity = GetEntity(TransformUsageFlags.None);

            AddBuffer<GenerateGameChunkWaitingBuffer>(gameWorldEntity);


            var sideLengthOfChunk = authoring.sideLengthOfChunk;
            var chunkSampleGridCellCount = sideLengthOfChunk * sideLengthOfChunk;

            AddComponent(gameWorldEntity, new GameWorldGenerationProperties
            {
                InitialWorldSize = authoring.initialWorldSizeInChunk,
                SideLengthOfChunk = sideLengthOfChunk,
                SideLengthOfChunkMinusOne = authoring.sideLengthOfChunk - 1,
                ChunkSampleGridCellCount = chunkSampleGridCellCount
            });
        }
    }
}