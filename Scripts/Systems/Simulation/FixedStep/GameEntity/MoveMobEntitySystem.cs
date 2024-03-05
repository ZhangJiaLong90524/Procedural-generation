// using Components;
// using Components.Tags.GameEntity.MobEntity;
// using Unity.Burst;
// using Unity.Entities;
// using Unity.Physics;
//
// namespace Systems.Simulation.FixedStep.GameEntity
// {
//     [UpdateInGroup(typeof(FixedStepGameEntitySystemGroup))]
//     [UpdateAfter(typeof(BufferVisibleEntitiesSystem))]
//     [RequireMatchingQueriesForUpdate]
//     public partial struct MoveMobEntitySystem : ISystem
//     {
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<MathematicsRandom>();
//             state.RequireForUpdate<PhysicsWorldSingleton>();
//         }
//
//         // [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             state.Dependency = new MoveGameEntityJob
//             {
//                 MovingEntityMask = SystemAPI.QueryBuilder().WithAll<Villager>().Build().GetEntityQueryMask(),
//                 PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
//                 FieldOfViewCollisionFilter = SystemAPI.QueryBuilder().WithAll<FieldOfView, Prefab, PhysicsCollider>()
//                     .WithOptions(EntityQueryOptions.IncludePrefab).Build()
//                     .GetSingleton<PhysicsCollider>().Value.Value.GetCollisionFilter()
//             }.ScheduleParallel(state.Dependency);
//             
//             if (raycastHit.Fraction > 0.382f)
//             {
//                 var forward = observerLocalTransform.Forward() * 3.82f;
//                 physicsVelocity.Linear = forward;
//             }
//             else
//             {
//                 physicsVelocity.Angular = new float3(0, 1.618f, 0);
//             }
//         }
//
//         public void OnDestroy(ref SystemState state)
//         {
//         }
//     }
// }
//

//todo

