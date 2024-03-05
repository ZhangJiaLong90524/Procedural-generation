// using System;
// using System.Runtime.InteropServices;
// using Authoring.EditorOnly;
// using Components;
// using Components.GameWorld.GameChunk.GameEntity;
// using Components.GameWorld.GameChunk.GameEntity.MobEntity;
// using Components.Tags.GameEntity.MobEntity;
// using Components.Tags.GameEntity.StaticEntity;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.DebugDisplay;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Physics;
// using Unity.Physics.Authoring;
// using Unity.Transforms;
// using UnityEngine;
// using Random = Unity.Mathematics.Random;
//
// namespace Systems.Simulation.Game.GameWorld.GameEntity.MobEntity
// {
//     [UpdateInGroup(typeof(MobEntitySystemGroup))]
//     public partial struct DetermineDestinationPositionSystem : ISystem
//     {
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
// #if UNITY_EDITOR
//             state.RequireForUpdate<GameDebugDisplayData>();
// #endif
//             state.RequireForUpdate<MathematicsRandom>();
//             state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var query = SystemAPI.QueryBuilder().WithAll<ReadyForDetermineDestinationPosition>().Build();
//             if (query.IsEmpty)
//             {
// #if UNITY_EDITOR
//                 if (SystemAPI.GetSingleton<GameDebugDisplayData>().DrawDestinationPosition == 1)
//                     state.Dependency = new DrawDestinationPosition().ScheduleParallel(state.Dependency);
// #endif
//                 return;
//             }
//
//             var ecb = new EntityCommandBuffer(Allocator.TempJob);
//             new DetermineDestinationPositionJob
//             {
//                 VillagerMask = SystemAPI.QueryBuilder().WithAll<Villager>().Build().GetEntityQueryMask(),
//                 MonsterMask = SystemAPI.QueryBuilder().WithAll<Monster>().Build().GetEntityQueryMask(),
//                 ECB = ecb,
//                 FoodMask = SystemAPI.QueryBuilder().WithAll<Food>().Build().GetEntityQueryMask(),
//                 PhysicsColliders = SystemAPI.GetComponentLookup<PhysicsCollider>(),
//                 LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(),
//                 Random = new Random(SystemAPI.GetSingletonRW<MathematicsRandom>().ValueRW.Random.NextUInt())
//             }.Schedule(state.Dependency).Complete();
//             ecb.Playback(state.EntityManager);
//             ecb.Dispose();
//
// #if UNITY_EDITOR
//             if (SystemAPI.GetSingleton<GameDebugDisplayData>().DrawDestinationPosition == 1)
//                 state.Dependency = new DrawDestinationPosition().ScheduleParallel(state.Dependency);
// #endif
//         }
//
//         [BurstCompile]
//         public void OnDestroy(ref SystemState state)
//         {
//         }
//     }
//
//     [BurstCompile]
//     [StructLayout(LayoutKind.Auto)]
//     [WithAll(typeof(ReadyForDetermineDestinationPosition))]
//     public partial struct DetermineDestinationPositionJob : IJobEntity
//     {
//         [ReadOnly] public EntityQueryMask VillagerMask;
//         [ReadOnly] public EntityQueryMask MonsterMask;
//         public EntityCommandBuffer ECB;
//         [ReadOnly] public EntityQueryMask FoodMask;
//         [ReadOnly] public ComponentLookup<PhysicsCollider> PhysicsColliders;
//         [ReadOnly] public ComponentLookup<LocalTransform> LocalTransforms;
//         public Random Random;
//
//         private void Execute(Entity entity,
//             ref DestinationPosition destinationPosition,
//             ref DynamicBuffer<ObservedGameEntitiesBuffer> observedGameEntitiesBuffers)
//         {
//             EntityQueryMask observedEntityQueryMask;
//             if (VillagerMask.MatchesIgnoreFilter(entity))
//                 observedEntityQueryMask = FoodMask;
//             else if (MonsterMask.MatchesIgnoreFilter(entity))
//                 observedEntityQueryMask = VillagerMask;
//             else
//                 throw new ArgumentException($"The entity(index: {entity.Index}) is not a villager or a monster.");
//
//             ECB.RemoveComponent<ReadyForDetermineDestinationPosition>(entity); //todo: fix the logic
//
//             var localTransform = LocalTransforms[entity];
//
//             foreach (var observedGameEntitiesBuffer in observedGameEntitiesBuffers)
//             {
//                 var observedEntity = observedGameEntitiesBuffer.ObservedEntity;
//                 if (!observedEntityQueryMask.MatchesIgnoreFilter(observedEntity)) continue;
//
//                 var observedEntityLocalTransform = LocalTransforms[observedEntity];
//                 var observedEntityAABBCenter = PhysicsColliders[observedEntity].Value.Value
//                     .CalculateAabb(new RigidTransform(observedEntityLocalTransform.Rotation,
//                         observedEntityLocalTransform.Position)).Center;
//                 destinationPosition.Value = localTransform.Position + observedEntityAABBCenter;
//                 return;
//             }
//
//             var forward = localTransform.Forward();
//             var randomAngle = Quaternion.Euler(0, Random.NextFloat(-90, 90), 0);
//             var randomDistance = Random.NextFloat(0, 10);
//             var randomVector = (float3)(randomAngle * forward * randomDistance);
//             destinationPosition.Value = localTransform.Position + randomVector;
//         }
//     }
//
// #if UNITY_EDITOR
//     [BurstCompile]
//     public partial struct DrawDestinationPosition : IJobEntity
//     {
//         [BurstCompile]
//         private static void Execute(in LocalTransform localTransform,
//             in DestinationPosition destinationPosition)
//         {
//             PhysicsDebugDisplaySystem.Line(localTransform.Position, destinationPosition.Value, ColorIndex.Orange);
//             PhysicsDebugDisplaySystem.Point(destinationPosition.Value, 0.5f, ColorIndex.OrangeRed);
//         }
//     }
// #endif
// }

