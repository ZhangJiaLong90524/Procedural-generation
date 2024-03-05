using Components;
using Components.GameWorld;
using Components.GameWorld.GameChunk;
using Components.GameWorld.GameChunk.GameEntity.Generation;
using Components.GameWorld.GameChunk.GameEntity.MobEntity;
using Components.Tags.ChunkStates;
using Components.Tags.GameEntity;
using Components.Tags.GameEntity.MobEntity;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Systems.Simulation.Game.GameWorld.GameEntity.MobEntity
{
    [UpdateInGroup(typeof(MobEntitySystemGroup))]
    public partial struct GenerateMobEntitySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Monster>();
            state.RequireForUpdate<Villager>();
            state.RequireForUpdate<GameWorldGenerationProperties>();
            state.RequireForUpdate<MathematicsRandom>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var chunkEntitiesQuery = SystemAPI.QueryBuilder().WithAll<GeneratingMobEntities>().Build();
            if (chunkEntitiesQuery.IsEmpty) return;

            var entityManager = state.EntityManager;

            var random = new Random(SystemAPI.GetSingletonRW<MathematicsRandom>().ValueRW.Random.NextUInt());

            var chunkEntities = chunkEntitiesQuery.ToEntityArray(Allocator.Temp);

            var gameWorldGenerationProperties = SystemAPI.GetSingleton<GameWorldGenerationProperties>();
            var sideLengthOfChunkMinusOne = gameWorldGenerationProperties.SideLengthOfChunkMinusOne;

            var villagerPrefabPackageEntity = SystemAPI.QueryBuilder()
                .WithAll<PrefabPackageEntity, Villager>().Build().GetSingletonEntity();
            var villagerPrefabEntity =
                entityManager.GetComponentData<EntityPrefab>(villagerPrefabPackageEntity).Entity;
            var villagerGenerationCountLimit =
                entityManager.GetComponentData<GenerationCountLimit>(villagerPrefabPackageEntity);
            var villagerMinGenerationCount = (int)villagerGenerationCountLimit.Min;
            var villagerGenerateTargetNumber = random.NextInt(villagerMinGenerationCount,
                (int)villagerGenerationCountLimit.Max);

            var monsterPrefabPackageEntity = SystemAPI.QueryBuilder()
                .WithAll<PrefabPackageEntity, Monster>().Build().GetSingletonEntity();
            var monsterPrefabEntity =
                entityManager.GetComponentData<EntityPrefab>(monsterPrefabPackageEntity).Entity;
            var monsterGenerationCountLimit =
                entityManager.GetComponentData<GenerationCountLimit>(monsterPrefabPackageEntity);
            var monsterMinGenerationCount = (int)monsterGenerationCountLimit.Min;
            var monsterGenerateTargetNumber = random.NextInt(monsterMinGenerationCount,
                (int)monsterGenerationCountLimit.Max);


            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var chunkEntity in chunkEntities)
            {
                ecb.RemoveComponent<GeneratingMobEntities>(chunkEntity);

                var chunkPosition = entityManager.GetComponentData<ChunkPosition>(chunkEntity);

                if (chunkPosition.X.Equals(0) && chunkPosition.Z.Equals(0))
                {
                    var sideLengthOfChunkHalfMinusOne = gameWorldGenerationProperties.SideLengthOfChunk / 2 - 1;
                    var chunkCenterPosition = new float2(sideLengthOfChunkHalfMinusOne, sideLengthOfChunkHalfMinusOne);
                    GenerateMobEntityInChunk(ecb, ref random, chunkEntity, chunkPosition,
                        villagerPrefabEntity,
                        villagerMinGenerationCount, villagerGenerateTargetNumber, chunkCenterPosition, 4);
                }

                const int monsterGenerationAreaSizeHalf = 8;
                var monsterGenerationAreaCenterPosition = GetGenerationAreaCenterPosition(ref random,
                    sideLengthOfChunkMinusOne, monsterGenerationAreaSizeHalf);
                GenerateMobEntityInChunk(ecb, ref random, chunkEntity, chunkPosition, monsterPrefabEntity,
                    monsterMinGenerationCount, monsterGenerateTargetNumber, monsterGenerationAreaCenterPosition,
                    monsterGenerationAreaSizeHalf);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            chunkEntities.Dispose();
        }

        [BurstCompile]
        private void GenerateMobEntityInChunk(EntityCommandBuffer ecb, ref Random random, Entity chunkEntity,
            ChunkPosition chunkPosition, Entity mobEntityPrefabEntity, int minGenerationCount, int generateTargetNumber,
            float2 generateAreaCenterPosition, int generationAreaSizeHalf)
        {
            var mobEntityCount = 0;
            while (mobEntityCount < generateTargetNumber)
            {
                var newMobEntity = ecb.Instantiate(mobEntityPrefabEntity);

                var randomPosition =
                    new float3(
                        generateAreaCenterPosition.x +
                        random.NextFloat(-generationAreaSizeHalf, generationAreaSizeHalf), 0,
                        generateAreaCenterPosition.y +
                        random.NextFloat(-generationAreaSizeHalf, generationAreaSizeHalf));
                ecb.AddComponent(newMobEntity, LocalTransform.FromPosition(randomPosition));

                ecb.AddComponent(newMobEntity, new AwaitingInitialDamping(3));

                ecb.AppendToBuffer(chunkEntity, new GameChunkEntityBuffer
                {
                    Entity = newMobEntity
                });

                mobEntityCount++;
            }

#if UNITY_EDITOR

            if (mobEntityCount < minGenerationCount)
                Debug.Log(
                    $"Failed to generate enough {mobEntityPrefabEntity.ToFixedString()} in chunk {chunkPosition}, only {mobEntityCount}/{generateTargetNumber} generated.");
#endif
        }

        private float2 GetGenerationAreaCenterPosition(ref Random random, int sideLengthOfChunkMinusOne, int areaSize)
        {
            var generateAreaLowerBound = 0 + areaSize;
            var generateAreaUpperBound = sideLengthOfChunkMinusOne - areaSize;

            return new float2(random.NextFloat(generateAreaLowerBound, generateAreaUpperBound),
                random.NextFloat(generateAreaLowerBound, generateAreaUpperBound));
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }
}