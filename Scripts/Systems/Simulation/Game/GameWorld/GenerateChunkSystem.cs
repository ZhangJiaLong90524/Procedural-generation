using Components.GameWorld;
using Components.GameWorld.GameChunk;
using Components.Tags.ChunkStates;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Systems.Simulation.Game.GameWorld
{
    [UpdateInGroup(typeof(GameWorldSystemGroup))]
    public partial struct GenerateChunkSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameWorldGenerationProperties>();
            state.RequireForUpdate<GenerateGameChunkWaitingBuffer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var generateGameChunkWaitingBuffer = SystemAPI.GetSingletonBuffer<GenerateGameChunkWaitingBuffer>();
            if (generateGameChunkWaitingBuffer.IsEmpty) return;


            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            foreach (var generateWaitingGameChunk in generateGameChunkWaitingBuffer)
            {
                var chunkEntity = ecb.CreateEntity();

                var chunkPosition = generateWaitingGameChunk.ChunkPosition;
                ecb.AddComponent(chunkEntity, new ChunkPosition
                {
                    X = chunkPosition.X,
                    Z = chunkPosition.Z
                });


                ecb.AddBuffer<GameChunkEntityBuffer>(chunkEntity);


                ecb.AddComponent(chunkEntity, new GeneratingStaticEntities());
            }


            var gameWorldEntity = SystemAPI.GetSingletonEntity<GameWorldGenerationProperties>();
            ecb.SetBuffer<GenerateGameChunkWaitingBuffer>(gameWorldEntity).Clear();


            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }
}