using System.Runtime.InteropServices;
using Authoring.EditorOnly;
using Components;
using Components.GameWorld;
using Components.GameWorld.GameChunk;
using Components.GameWorld.GameChunk.GameEntity.Generation;
using Components.Tags.ChunkStates;
using Components.Tags.GameEntity;
using Components.Tags.GameEntity.StaticEntity;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Systems.Simulation.Game.GameWorld.GameEntity
{
    [UpdateInGroup(typeof(GameEntitySystemGroup))]
    [StructLayout(LayoutKind.Auto)]
    public partial struct GenerateStaticEntitySystem : ISystem, ISystemStartStop
    {
        private Random _random;
        private GameWorldGenerationProperties _gameWorldGenerationProperties;
        private Entity _groundUnitPrefabEntity;
        private Entity _obstaclePrefabEntityPackageEntity;
        private Entity _foodPrefabPackageEntity;
        private NativeArray<float3x2> _chunkSampleGrid;
        private NativeList<ushort> _emptyOrPossibleCollideCellIndexesOfChunkSampleGrid;
        private NativeArray<float3x2> _initializedChunkSampleGrid;
        private NativeList<ushort> _initializedEmptyOrPossibleCollideCellIndexesOfGridArray;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
#if UNITY_EDITOR
            state.RequireForUpdate<GameDebugDisplayData>();
#endif
            state.RequireForUpdate<Food>();
            state.RequireForUpdate<Obstacle>();
            state.RequireForUpdate<GroundUnit>();
            state.RequireForUpdate<GameWorldGenerationProperties>();
            state.RequireForUpdate<MathematicsRandom>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            _random = new Random(SystemAPI.GetSingletonRW<MathematicsRandom>().ValueRW.Random.NextUInt());
            _gameWorldGenerationProperties = SystemAPI.GetSingleton<GameWorldGenerationProperties>();

            var groundUnitPrefabPackageEntity = SystemAPI.QueryBuilder().WithAll<PrefabPackageEntity, GroundUnit>()
                .Build().GetSingletonEntity();
            _groundUnitPrefabEntity =
                state.EntityManager.GetComponentData<EntityPrefab>(groundUnitPrefabPackageEntity).Entity;
            _obstaclePrefabEntityPackageEntity = SystemAPI.QueryBuilder().WithAll<PrefabPackageEntity, Obstacle>()
                .Build().GetSingletonEntity();
            _foodPrefabPackageEntity = SystemAPI.QueryBuilder().WithAll<PrefabPackageEntity, Food>().Build()
                .GetSingletonEntity();

            var chunkSampleGridCellCount = _gameWorldGenerationProperties.ChunkSampleGridCellCount;

            _chunkSampleGrid = new NativeArray<float3x2>(chunkSampleGridCellCount, Allocator.Persistent);
            _emptyOrPossibleCollideCellIndexesOfChunkSampleGrid =
                new NativeList<ushort>(chunkSampleGridCellCount, Allocator.Persistent);

            _initializedChunkSampleGrid = new NativeArray<float3x2>(chunkSampleGridCellCount, Allocator.Persistent);
            _initializedEmptyOrPossibleCollideCellIndexesOfGridArray =
                new NativeList<ushort>(chunkSampleGridCellCount + 1, Allocator.Persistent);

            for (ushort index = 0; index < chunkSampleGridCellCount; index++)
            {
                _initializedChunkSampleGrid[index] = float.NaN;
                _initializedEmptyOrPossibleCollideCellIndexesOfGridArray.Add(index);
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var chunkEntitiesQuery = SystemAPI.QueryBuilder().WithAll<GeneratingStaticEntities>().Build();
            if (chunkEntitiesQuery.IsEmpty) return;


            var chunkEntities = chunkEntitiesQuery.ToEntityArray(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var chunkEntity in chunkEntities)
            {
                ecb.RemoveComponent<GeneratingStaticEntities>(chunkEntity);

                var chunkPosition = state.EntityManager.GetComponentData<ChunkPosition>(chunkEntity);

                for (var x = 0; x < _gameWorldGenerationProperties.SideLengthOfChunk; x++)
                for (var z = 0; z < _gameWorldGenerationProperties.SideLengthOfChunk; z++)
                    ecb.SetComponent(ecb.Instantiate(_groundUnitPrefabEntity),
                        LocalTransform.FromPosition(new float3(chunkPosition.X + x, -1, chunkPosition.Z + z)));

                _chunkSampleGrid.CopyFrom(_initializedChunkSampleGrid);

                _emptyOrPossibleCollideCellIndexesOfChunkSampleGrid.CopyFrom(
                    _initializedEmptyOrPossibleCollideCellIndexesOfGridArray);

                GenerateStaticEntityInChunk(state.EntityManager, ecb, ref _random, chunkEntity, chunkPosition,
                    _chunkSampleGrid, _emptyOrPossibleCollideCellIndexesOfChunkSampleGrid,
                    _obstaclePrefabEntityPackageEntity);

                GenerateStaticEntityInChunk(state.EntityManager, ecb, ref _random, chunkEntity, chunkPosition,
                    _chunkSampleGrid, _emptyOrPossibleCollideCellIndexesOfChunkSampleGrid, _foodPrefabPackageEntity);

                ecb.AddComponent<GeneratingMobEntities>(chunkEntity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            chunkEntities.Dispose();
        }

        [BurstCompile]
        private void GenerateStaticEntityInChunk(EntityManager entityManager, EntityCommandBuffer ecb,
            ref Random random, Entity chunkEntity, ChunkPosition chunkPosition, NativeArray<float3x2> chunkSampleGrid,
            NativeList<ushort> emptyOrPossibleCollideCellIndexesOfChunkSampleGrid, Entity prefabEntityPackageEntity)
        {
            var maxChunkSampleGridBoundary = _gameWorldGenerationProperties.SideLengthOfChunkMinusOne;
            var sideLengthOfChunk = _gameWorldGenerationProperties.SideLengthOfChunk;


            var prefabEntity = entityManager.GetComponentData<EntityPrefab>(prefabEntityPackageEntity).Entity;

            var gameEntitySampleGridGenerationProperties =
                entityManager.GetComponentData<CollisionProperties>(
                    prefabEntityPackageEntity);
            var entityCollisionRadius = gameEntitySampleGridGenerationProperties.CollisionRadius;

            var generationCountLimit = entityManager.GetComponentData<GenerationCountLimit>(prefabEntityPackageEntity);
            var generateTargetNumber = random.NextInt((int)generationCountLimit.Min,
                (int)generationCountLimit.Max);

            var staticEntityCount = 0;
            var generateTries = 0;
            var maxGenerateTries = generateTargetNumber * 30;
            do
            {
                generateTries++;

                var randomCellIndex =
                    emptyOrPossibleCollideCellIndexesOfChunkSampleGrid[
                        random.NextInt(0, emptyOrPossibleCollideCellIndexesOfChunkSampleGrid.Length)];


                var randomCellPositionX = randomCellIndex % sideLengthOfChunk;
                var randomCellPositionZ = randomCellIndex / sideLengthOfChunk;

                var randomLocalPositionX = randomCellPositionX + random.NextFloat(0, 1);
                var randomLocalPositionZ = randomCellPositionZ + random.NextFloat(0, 1);


                var maxCollisionRadiusAABBPositionX = randomLocalPositionX + entityCollisionRadius;
                var minCollisionRadiusAABBPositionX = randomLocalPositionX - entityCollisionRadius;
                var maxCollisionRadiusAABBPositionZ = randomLocalPositionZ + entityCollisionRadius;
                var minCollisionRadiusAABBPositionZ = randomLocalPositionZ - entityCollisionRadius;

                if (maxCollisionRadiusAABBPositionX > maxChunkSampleGridBoundary ||
                    minCollisionRadiusAABBPositionX < 0) continue;
                if (maxCollisionRadiusAABBPositionZ > maxChunkSampleGridBoundary ||
                    minCollisionRadiusAABBPositionZ < 0) continue;

                var maxOuterSquareOfCollisionRadiusCellPositionX = (int)math.floor(maxCollisionRadiusAABBPositionX);
                var minOuterSquareOfCollisionRadiusCellPositionX = (int)math.floor(minCollisionRadiusAABBPositionX);
                var manOuterSquareOfCollisionRadiusCellPositionZ = (int)math.floor(maxCollisionRadiusAABBPositionZ);
                var minOuterSquareOfCollisionRadiusCellPositionZ = (int)math.floor(minCollisionRadiusAABBPositionZ);

                var maxInnerCellAreaPositionX = maxOuterSquareOfCollisionRadiusCellPositionX - 1;
                var minInnerCellAreaPositionX = minOuterSquareOfCollisionRadiusCellPositionX + 1;
                var maxInnerCellAreaPositionZ = manOuterSquareOfCollisionRadiusCellPositionZ - 1;
                var minInnerCellAreaPositionZ = minOuterSquareOfCollisionRadiusCellPositionZ + 1;

                var outerSquareCellCount = (maxOuterSquareOfCollisionRadiusCellPositionX -
                                               minOuterSquareOfCollisionRadiusCellPositionX + 1) *
                                           (manOuterSquareOfCollisionRadiusCellPositionZ -
                                               minOuterSquareOfCollisionRadiusCellPositionZ + 1);

                #region Get checking collision cell indexes

                var cellIndexesOfApproximatePossibleCollisionArea =
                    new NativeList<ushort>(outerSquareCellCount, Allocator.Temp);
                for (var x = minOuterSquareOfCollisionRadiusCellPositionX;
                     x <= maxOuterSquareOfCollisionRadiusCellPositionX;
                     x++)
                {
                    cellIndexesOfApproximatePossibleCollisionArea.AddNoResize(
                        (ushort)(minOuterSquareOfCollisionRadiusCellPositionZ * sideLengthOfChunk + x));

                    cellIndexesOfApproximatePossibleCollisionArea.AddNoResize(
                        (ushort)(manOuterSquareOfCollisionRadiusCellPositionZ * sideLengthOfChunk + x));
                }

                for (var z = minInnerCellAreaPositionZ; z <= maxInnerCellAreaPositionZ; z++)
                {
                    var cellIndexZPart = z * sideLengthOfChunk;
                    cellIndexesOfApproximatePossibleCollisionArea.AddNoResize(
                        (ushort)(cellIndexZPart + minOuterSquareOfCollisionRadiusCellPositionX));
                    cellIndexesOfApproximatePossibleCollisionArea.AddNoResize(
                        (ushort)(cellIndexZPart + maxOuterSquareOfCollisionRadiusCellPositionX));
                }

                #endregion

                #region Read cellData to Check if collide to other entity

                var randomPosition = new float2(randomLocalPositionX, randomLocalPositionZ);

                if (IsCollideToCell(chunkSampleGrid, cellIndexesOfApproximatePossibleCollisionArea, randomPosition,
                        entityCollisionRadius))
                {
                    cellIndexesOfApproximatePossibleCollisionArea.Dispose();
                    continue;
                }

                #endregion

                #region Remove collided cell index in validCellIndexesOfGridArray

                var entityApproximateCollisionSquareSideLengthHalf =
                    gameEntitySampleGridGenerationProperties.ApproximateCollisionSquareSideLengthHalf;
                var approximateCollisionSquareMaxX =
                    randomLocalPositionX + entityApproximateCollisionSquareSideLengthHalf;
                var approximateCollisionSquareMinX =
                    randomLocalPositionX - entityApproximateCollisionSquareSideLengthHalf;
                var approximateCollisionSquareMaxZ =
                    randomLocalPositionZ + entityApproximateCollisionSquareSideLengthHalf;
                var approximateCollisionSquareMinZ =
                    randomLocalPositionZ - entityApproximateCollisionSquareSideLengthHalf;
#if UNITY_EDITOR
                var gameDebugDisplayData = SystemAPI.GetSingleton<GameDebugDisplayData>();
                if (gameDebugDisplayData.DrawGenerateStaticEntitySystem == 1)
                {
                    DebugExtension.DebugLocalCube(Matrix4x4.identity,
                        new Vector3(maxCollisionRadiusAABBPositionX - minCollisionRadiusAABBPositionX, 0,
                            maxCollisionRadiusAABBPositionZ - minCollisionRadiusAABBPositionZ), Color.green,
                        new Vector3(randomPosition.x, 0, randomPosition.y), float.PositiveInfinity, default);

                    DebugExtension.DebugLocalCube(Matrix4x4.identity,
                        new Vector3(approximateCollisionSquareMaxX - approximateCollisionSquareMinX, 0,
                            approximateCollisionSquareMaxZ - approximateCollisionSquareMinZ), Color.green,
                        new Vector3(randomPosition.x, 0, randomPosition.y), float.PositiveInfinity, default);
                }
#endif

                for (var x = minInnerCellAreaPositionX; x <= maxInnerCellAreaPositionX; x++)
                for (var z = minInnerCellAreaPositionZ; z <= maxInnerCellAreaPositionZ; z++)
                {
                    var cellIndex = (ushort)(z * sideLengthOfChunk + x);
                    if (approximateCollisionSquareMinX <= x && x + 1 <= approximateCollisionSquareMaxX &&
                        approximateCollisionSquareMinZ <= z &&
                        z + 1 <= approximateCollisionSquareMaxZ)
                    {
                        for (var index = 0; index < emptyOrPossibleCollideCellIndexesOfChunkSampleGrid.Length; index++)
                            if (emptyOrPossibleCollideCellIndexesOfChunkSampleGrid[index] == cellIndex)
                            {
                                emptyOrPossibleCollideCellIndexesOfChunkSampleGrid.RemoveAtSwapBack(index);
#if UNITY_EDITOR
                                if (gameDebugDisplayData.DrawGenerateStaticEntitySystem == 1)
                                    DebugExtension.DebugWireSphere(new Vector3(x + 0.5f, 0, z + 0.5f), Color.red,
                                        0.5f, float.PositiveInfinity, default);
#endif
                                break;
                            }
                    }
                    else
                    {
                        cellIndexesOfApproximatePossibleCollisionArea.AddNoResize(cellIndex);
                    }
                }

                #endregion

                #region Add entity position and CollisionDistanceSquared to collided and possible collision cell

                foreach (var cellIndex in cellIndexesOfApproximatePossibleCollisionArea)
                {
#if UNITY_EDITOR
                    var spherePositionX = cellIndex % sideLengthOfChunk + 0.5f;
                    // ReSharper disable once PossibleLossOfFraction
                    var spherePositionZ = cellIndex / sideLengthOfChunk + 0.5f;

#endif
                    var cellData = chunkSampleGrid[cellIndex];

                    var entityPositionAndCollisionRadius =
                        new float3(randomLocalPositionX, randomLocalPositionZ, entityCollisionRadius);
                    if (float.IsNaN(cellData.c1.x))
                    {
                        if (float.IsNaN(cellData.c0.x))
                        {
                            chunkSampleGrid[cellIndex] =
                                new float3x2(entityPositionAndCollisionRadius, cellData.c1);
#if UNITY_EDITOR
                            if (gameDebugDisplayData.DrawGenerateStaticEntitySystem == 1)
                                DebugExtension.DebugWireSphere(new Vector3(spherePositionX, 0, spherePositionZ),
                                    Color.cyan, 0.5f, float.PositiveInfinity, default);
#endif
                        }
                        else
                        {
                            chunkSampleGrid[cellIndex] =
                                new float3x2(cellData.c0, entityPositionAndCollisionRadius);
#if UNITY_EDITOR
                            if (gameDebugDisplayData.DrawGenerateStaticEntitySystem == 1)
                                DebugExtension.DebugWireSphere(new Vector3(spherePositionX, 0, spherePositionZ),
                                    Color.yellow, 0.5f, float.PositiveInfinity, default);
#endif
                        }
                    }
                    else
                    {
#if UNITY_EDITOR
                        if (gameDebugDisplayData.DrawGenerateStaticEntitySystem == 1)
                            DebugExtension.DebugWireSphere(new Vector3(spherePositionX, 0, spherePositionZ),
                                Color.magenta, 0.5f, float.PositiveInfinity, default);
#endif
                    }

                    for (var index = 0; index < emptyOrPossibleCollideCellIndexesOfChunkSampleGrid.Length; index++)
                        if (emptyOrPossibleCollideCellIndexesOfChunkSampleGrid[index] == cellIndex)
                        {
                            emptyOrPossibleCollideCellIndexesOfChunkSampleGrid.RemoveAtSwapBack(index);
                            break;
                        }
                }

                #endregion

                cellIndexesOfApproximatePossibleCollisionArea.Dispose();

                var gameEntityPosition =
                    new float3(chunkPosition.X + randomPosition.x, 0, chunkPosition.Z + randomPosition.y);

#if UNITY_EDITOR
                if (gameDebugDisplayData.DrawGenerateStaticEntitySystem == 1)
                    DebugExtension.DebugWireSphere(new Vector3(gameEntityPosition.x, 0, gameEntityPosition.z),
                        Color.green, entityCollisionRadius, float.PositiveInfinity, default);
#endif

                var newGameEntity = ecb.Instantiate(prefabEntity);

                ecb.SetComponent(newGameEntity, LocalTransform.FromPosition(gameEntityPosition));

                ecb.AppendToBuffer(chunkEntity, new GameChunkEntityBuffer
                {
                    Entity = newGameEntity
                });

                staticEntityCount++;
            } while (staticEntityCount < generateTargetNumber && generateTries < maxGenerateTries);

#if UNITY_EDITOR
            if (staticEntityCount < generationCountLimit.Min)
                Debug.Log(
                    $"Failed to generate enough {prefabEntity.ToFixedString()} in chunk {chunkPosition}, only {staticEntityCount}/{generateTargetNumber}  generated.");
#endif
        }

        [BurstCompile]
        private bool IsCollideToCell(NativeArray<float3x2> chunkSampleGrid,
            NativeList<ushort> cellIndexesOfApproximatePossibleCollisionArea,
            in float2 newPosition,
            in float entityCollisionRadius)
        {
            foreach (var cellIndex in cellIndexesOfApproximatePossibleCollisionArea)
            {
                var cellData = chunkSampleGrid[cellIndex];

                if (float.IsNaN(cellData.c0.x)) continue;
                var cellCollisionDistance = cellData.c0.z + entityCollisionRadius;
                if (math.distancesq(newPosition, new float2(cellData.c0.x, cellData.c0.y)) <
                    cellCollisionDistance * cellCollisionDistance)
                    return true;

                if (float.IsNaN(cellData.c1.x)) continue;
                cellCollisionDistance = cellData.c1.z + entityCollisionRadius;
                if (math.distancesq(newPosition, new float2(cellData.c1.x, cellData.c1.y)) <
                    cellCollisionDistance * cellCollisionDistance)
                    return true;
            }

            return false;
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
            _chunkSampleGrid.Dispose();
            _emptyOrPossibleCollideCellIndexesOfChunkSampleGrid.Dispose();
            _initializedChunkSampleGrid.Dispose();
            _initializedEmptyOrPossibleCollideCellIndexesOfGridArray.Dispose();
        }
    }
}