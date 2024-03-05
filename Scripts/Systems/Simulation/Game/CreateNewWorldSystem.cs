using System.Runtime.InteropServices;
using Components.GameWorld;
using Components.GameWorld.GameChunk;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Systems.Simulation.Game
{
    [UpdateInGroup(typeof(GameSystemGroup))]
    [StructLayout(LayoutKind.Auto)]
    public partial struct CreateNewWorldSystem : ISystem, ISystemStartStop //todo: refactor to Job with ECS UI
    {
        private GameWorldGenerationProperties _gameWorldGenerateProperties;
        private Entity _gameWorldEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GenerateGameChunkWaitingBuffer>();
            state.RequireForUpdate<GameWorldGenerationProperties>();
            state.Enabled = false;
        }

        public void OnStartRunning(ref SystemState state)
        {
            _gameWorldGenerateProperties = SystemAPI.GetSingleton<GameWorldGenerationProperties>();
            _gameWorldEntity = SystemAPI.GetSingletonEntity<GenerateGameChunkWaitingBuffer>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var initialWorldSize = _gameWorldGenerateProperties.InitialWorldSize;
            var chunkPositionStart = -(initialWorldSize >> 1);
            var chunkPositionEnd = chunkPositionStart + initialWorldSize;
            var chunkSize = _gameWorldGenerateProperties.SideLengthOfChunk;

            for (var x = chunkPositionStart; x < chunkPositionEnd; x++)
            for (var z = chunkPositionStart; z < chunkPositionEnd; z++)
                ecb.AppendToBuffer(_gameWorldEntity, new GenerateGameChunkWaitingBuffer
                {
                    ChunkPosition = new ChunkPosition(x * chunkSize, z * chunkSize)
                });

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            state.Enabled = false;
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }
}