using System;
using Components;
using Unity.Burst;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace Systems.Initialization
{
    [UpdateInGroup(typeof(InitializationGameSystemGroup))]
    public partial struct InitializeGameSystem : ISystem, ISystemStartStop
    {
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstDiscard]
        public void OnStartRunning(ref SystemState state)
        {
            var entityManager = state.EntityManager;

            var gameEntity = entityManager.CreateSingleton<MathematicsRandom>();

            entityManager.SetComponentData(gameEntity,
                new MathematicsRandom(
                    new Random((uint)DateTime.Now.Ticks))); //System.DateTime is not supported by Burst
        }


        public void OnUpdate(ref SystemState state)
        {
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }
}