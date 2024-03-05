using System.Runtime.InteropServices;
using Components.GameWorld.GameChunk.GameEntity.MobEntity;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;

namespace Systems.Initialization
{
    [UpdateInGroup(typeof(InitializationGameSystemGroup))]
    public partial struct InitializeDampingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAllRW<AwaitingInitialDamping>().Build();
            if (query.IsEmpty) return;

            new InitialDampingJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                ECB = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel();
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    public partial struct InitialDampingJob : IJobEntity
    {
        [ReadOnly] public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;

        [BurstCompile]
        private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref PhysicsDamping physicsDamping,
            ref AwaitingInitialDamping awaitingInitialDamping)
        {
            var awaitingSecondsLeft = awaitingInitialDamping.AwaitingSecondsLeft;

            awaitingSecondsLeft -= DeltaTime;

            if (awaitingSecondsLeft > 0)
            {
                awaitingInitialDamping = new AwaitingInitialDamping(awaitingSecondsLeft);
            }
            else
            {
                ECB.RemoveComponent<AwaitingInitialDamping>(chunkIndex, entity);
                physicsDamping = new PhysicsDamping
                {
                    Linear = 0.01f,
                    Angular = 0.05f
                };
            }
        }
    }
}