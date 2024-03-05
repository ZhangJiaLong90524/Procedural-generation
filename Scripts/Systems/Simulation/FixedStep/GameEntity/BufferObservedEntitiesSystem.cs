using System;
using System.Runtime.InteropServices;
using Authoring.EditorOnly;
using Components.GameWorld.GameChunk.GameEntity;
using Components.GameWorld.GameChunk.GameEntity.Generation;
using Components.Tags.GameEntity;
using Components.Tags.GameEntity.MobEntity;
using Unity.Burst;
using Unity.Collections;
using Unity.DebugDisplay;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Transforms;

namespace Systems.Simulation.FixedStep.GameEntity
{
    [UpdateInGroup(typeof(FixedStepGameEntitySystemGroup))]
    [RequireMatchingQueriesForUpdate]
    [StructLayout(LayoutKind.Auto)]
    public partial struct BufferObservedEntitiesSystem : ISystem, ISystemStartStop
    {
        private EntityQueryMask _fieldOfViewMask;
        private CollisionFilter _fieldOfViewCollisionFilter;
        private SimulationSingleton _simulationSingleton;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
#if UNITY_EDITOR
            state.RequireForUpdate<GameDebugDisplayData>();
#endif
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            _fieldOfViewMask = SystemAPI.QueryBuilder().WithAll<FieldOfView>().Build().GetEntityQueryMask();

            var fieldOfViewPrefabPackageEntity = SystemAPI.QueryBuilder().WithAll<PrefabPackageEntity, FieldOfView>()
                .Build().GetSingletonEntity();
            var fieldOfViewPrefabEntity =
                state.EntityManager.GetComponentData<EntityPrefab>(fieldOfViewPrefabPackageEntity).Entity;
            var fieldOfViewPrefabCollider =
                SystemAPI.GetComponent<PhysicsCollider>(fieldOfViewPrefabEntity).Value.Value;
            _fieldOfViewCollisionFilter = fieldOfViewPrefabCollider.GetCollisionFilter();
            _simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<ObservedGameEntitiesDataBuffer>().Build();
            if (query.IsEmpty)
            {
                return;
            }


            state.Dependency = new UpdateObservedGameEntitiesDataBufferJob
            {
                FixedDeltaTime = SystemAPI.Time.fixedDeltaTime
            }.ScheduleParallel(query, state.Dependency);


#if UNITY_EDITOR
            var drawBufferVisibleEntitiesSystem =
                SystemAPI.GetSingleton<GameDebugDisplayData>().DrawBufferVisibleEntitiesSystem;
#endif

            state.Dependency = new BufferObservedEntitiesEvent
            {
                FieldOfViewMask = _fieldOfViewMask,
                ParentLookup = SystemAPI.GetComponentLookup<Parent>(),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                PhysicsColliderLookup = SystemAPI.GetComponentLookup<PhysicsCollider>(),
                RaycastInputCollisionFilter = _fieldOfViewCollisionFilter,
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                ObservedGameEntitiesDataBufferLookup = SystemAPI.GetBufferLookup<ObservedGameEntitiesDataBuffer>()
#if UNITY_EDITOR
                , DrawBufferVisibleEntitiesSystem = drawBufferVisibleEntitiesSystem
#endif
            }.Schedule(_simulationSingleton, state.Dependency);


#if UNITY_EDITOR
            if (drawBufferVisibleEntitiesSystem == 1)
            {
                state.Dependency = new DrawObservedGameEntitiesCenterJob
                {
                    LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
                    PhysicsColliderLookup = SystemAPI.GetComponentLookup<PhysicsCollider>()
                }.ScheduleParallel(query, state.Dependency);
            }
#endif
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        [StructLayout(LayoutKind.Auto)]
        private partial struct UpdateObservedGameEntitiesDataBufferJob : IJobEntity
        {
            [ReadOnly] public float FixedDeltaTime;

            [BurstCompile]
            private void Execute(ref DynamicBuffer<ObservedGameEntitiesDataBuffer> observedGameEntitiesDataBuffer)
            {
                for (var index = observedGameEntitiesDataBuffer.Length - 1; index >= 0; index--)
                {
                    var secondsSinceLastObservation =
                        observedGameEntitiesDataBuffer[index].SecondsSinceLastObservation + FixedDeltaTime;

                    if (secondsSinceLastObservation > 10)
                    {
                        observedGameEntitiesDataBuffer.RemoveAtSwapBack(index);
                    }
                    else
                    {
                        observedGameEntitiesDataBuffer[index] =
                            new ObservedGameEntitiesDataBuffer(observedGameEntitiesDataBuffer[index],
                                secondsSinceLastObservation);
                    }
                }
            }
        }

        [BurstCompile]
        private struct BufferObservedEntitiesEvent : ITriggerEventsJob
        {
            [ReadOnly] public EntityQueryMask FieldOfViewMask;
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<PhysicsCollider> PhysicsColliderLookup;
            public CollisionFilter RaycastInputCollisionFilter;
            [ReadOnly] public PhysicsWorld PhysicsWorld;
            public BufferLookup<ObservedGameEntitiesDataBuffer> ObservedGameEntitiesDataBufferLookup;
#if UNITY_EDITOR
            [ReadOnly] public int DrawBufferVisibleEntitiesSystem;
#endif

            [BurstCompile]
            public void Execute(TriggerEvent triggerEvent)
            {
                //TriggerEvent只知道兩個Entity觸發事件，需使用EntityQueryMask確認哪個是視野物件
                var triggerEntityA = triggerEvent.EntityA;
                var triggerEntityB = triggerEvent.EntityB;

                Entity fieldOfViewEntity;
                Entity observedEntity;
                if (FieldOfViewMask.MatchesIgnoreFilter(triggerEntityA))
                {
                    fieldOfViewEntity = triggerEntityA;
                    observedEntity = triggerEntityB;
                }
                else if (FieldOfViewMask.MatchesIgnoreFilter(triggerEntityB))
                {
                    fieldOfViewEntity = triggerEntityB;
                    observedEntity = triggerEntityA;
                }
                else
                {
                    return;
                }


                var observerEntity = ParentLookup[fieldOfViewEntity].Value;

                var observerLocalTransform = LocalTransformLookup[observerEntity];
                var raycastStart = PhysicsColliderLookup[observerEntity].Value.Value
                    .CalculateAabb(new RigidTransform(observerLocalTransform.Rotation,
                        observerLocalTransform.Position)).Center;

                var observedEntityLocalTransform = LocalTransformLookup[observedEntity];
                var raycastEnd = PhysicsColliderLookup[observedEntity].Value.Value
                    .CalculateAabb(new RigidTransform(observedEntityLocalTransform.Rotation,
                        observedEntityLocalTransform.Position))
                    .Center; //Fraction length unit will not be same for all entities

                RaycastInputCollisionFilter.GroupIndex = -observerEntity.Index; //忽略observerEntity
                var raycastInput = new RaycastInput
                {
                    Start = raycastStart,
                    End = raycastEnd,
                    Filter = RaycastInputCollisionFilter
                };
                PhysicsWorld.CastRay(raycastInput, out var raycastHit);


                if (raycastHit.Entity == observedEntity)
                {
                    var observedGameEntitiesDataBuffer = ObservedGameEntitiesDataBufferLookup[observerEntity];
                    for (var index = 0; index < observedGameEntitiesDataBuffer.Length; index++)
                    {
                        if (observedGameEntitiesDataBuffer[index].Entity == observedEntity)
                        {
                            observedGameEntitiesDataBuffer.RemoveAtSwapBack(index);
                            break;
                        }
                    }


                    observedGameEntitiesDataBuffer.Add(new ObservedGameEntitiesDataBuffer(observedEntity));
                }


#if UNITY_EDITOR
                if (DrawBufferVisibleEntitiesSystem == 1)
                {
                    var raycastHitPosition = raycastStart + (raycastEnd - raycastStart) * raycastHit.Fraction;
                    PhysicsDebugDisplaySystem.Line(raycastHitPosition, raycastEnd, ColorIndex.BrightRed);
                    PhysicsDebugDisplaySystem.Line(raycastStart, raycastHitPosition, ColorIndex.BrightBlue);
                }
#endif
            }
        }

#if UNITY_EDITOR
        [BurstCompile]
        [StructLayout(LayoutKind.Auto)]
        private partial struct DrawObservedGameEntitiesCenterJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<PhysicsCollider> PhysicsColliderLookup;

            [BurstCompile]
            private void Execute(ref DynamicBuffer<ObservedGameEntitiesDataBuffer> observedGameEntitiesDataBuffer)
            {
                foreach (var observedGameEntitiesData in observedGameEntitiesDataBuffer)
                {
                    var observedEntityLocalTransform = LocalTransformLookup[observedGameEntitiesData.Entity];
                    var observedEntityAabbCenter = PhysicsColliderLookup[observedGameEntitiesData.Entity].Value.Value
                        .CalculateAabb(new RigidTransform(observedEntityLocalTransform.Rotation,
                            observedEntityLocalTransform.Position)).Center;
                    switch (observedGameEntitiesData.SecondsSinceLastObservation)
                    {
                        case > 8:
                            PhysicsDebugDisplaySystem.Point(observedEntityAabbCenter, 0.5f, ColorIndex.Red);
                            break;
                        case > 6:
                            PhysicsDebugDisplaySystem.Point(observedEntityAabbCenter, 0.5f, ColorIndex.Yellow);
                            break;
                        case > 4:
                            PhysicsDebugDisplaySystem.Point(observedEntityAabbCenter, 0.5f, ColorIndex.Green);
                            break;
                        case > 2:
                            PhysicsDebugDisplaySystem.Point(observedEntityAabbCenter, 0.5f, ColorIndex.Cyan);
                            break;
                        case >= 0:
                            PhysicsDebugDisplaySystem.Point(observedEntityAabbCenter, 0.5f, ColorIndex.Blue);
                            break;
                        default:
                            throw new ArgumentException("SecondsSinceLastObservation is invalid");
                    }
                }
            }
        }
#endif
    }
}