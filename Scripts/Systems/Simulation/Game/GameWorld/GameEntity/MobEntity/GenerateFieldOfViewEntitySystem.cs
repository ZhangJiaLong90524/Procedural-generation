using System.Runtime.InteropServices;
using Components;
using Components.GameWorld;
using Components.GameWorld.GameChunk.GameEntity.Generation;
using Components.Tags.GameEntity;
using Components.Tags.GameEntity.InitializeStates.PerceptiveModule;
using Components.Tags.GameEntity.MobEntity;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace Systems.Simulation.Game.GameWorld.GameEntity.MobEntity
{
    [UpdateInGroup(typeof(MobEntitySystemGroup))]
    [StructLayout(LayoutKind.Auto)]
    public partial struct GenerateFieldOfViewEntitySystem : ISystem, ISystemStartStop
    {
        private Entity _fieldOfViewPrefabEntity;
        private Collider _fieldOfViewPrefabCollider;
        private CollisionFilter _fieldOfViewCollisionFilter;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Monster>();
            state.RequireForUpdate<Villager>();
            state.RequireForUpdate<FieldOfView>();
            state.RequireForUpdate<GameWorldGenerationProperties>();
            state.RequireForUpdate<MathematicsRandom>();
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            var fieldOfViewPrefabPackageEntity = SystemAPI.QueryBuilder().WithAll<PrefabPackageEntity, FieldOfView>()
                .Build().GetSingletonEntity();
            _fieldOfViewPrefabEntity =
                state.EntityManager.GetComponentData<EntityPrefab>(fieldOfViewPrefabPackageEntity).Entity;

            _fieldOfViewPrefabCollider =
                SystemAPI.GetComponent<PhysicsCollider>(_fieldOfViewPrefabEntity).Value.Value;
            _fieldOfViewCollisionFilter = _fieldOfViewPrefabCollider.GetCollisionFilter();
        }


        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<GeneratingFieldOfViews>().Build();
            if (query.IsEmpty) return;


            var perceptiveEntities = query.ToEntityArray(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var physicsColliderLookup = SystemAPI.GetComponentLookup<PhysicsCollider>();

            foreach (var perceptiveEntity in perceptiveEntities)
            {
                ecb.RemoveComponent<GeneratingFieldOfViews>(perceptiveEntity);


                var groupIndex = -perceptiveEntity.Index;

                var colliderBlobAssetReference = physicsColliderLookup[perceptiveEntity].Value.Value.Clone();
                var collisionFilter = colliderBlobAssetReference.Value.GetCollisionFilter();

                colliderBlobAssetReference.Value.SetCollisionFilter(new CollisionFilter
                {
                    BelongsTo = collisionFilter.BelongsTo,
                    CollidesWith = collisionFilter.CollidesWith,
                    GroupIndex = groupIndex
                });

                ecb.SetComponent(perceptiveEntity, new PhysicsCollider
                {
                    Value = colliderBlobAssetReference
                });


                var fieldOfViewEntity = ecb.Instantiate(_fieldOfViewPrefabEntity);

                ecb.AddComponent(fieldOfViewEntity, new Parent
                {
                    Value = perceptiveEntity
                });
            }

            perceptiveEntities.Dispose();


            ecb.Playback(state.EntityManager);
            ecb.Dispose();


            state.Dependency = new SetFieldOfViewGroupIndexJob
            {
                FieldOfViewCollisionFilter = _fieldOfViewCollisionFilter
            }.ScheduleParallel(state.Dependency);
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        [StructLayout(LayoutKind.Auto)]
        [WithAll(typeof(FieldOfView))]
        public partial struct SetFieldOfViewGroupIndexJob : IJobEntity
        {
            [ReadOnly] public CollisionFilter FieldOfViewCollisionFilter;

            private void Execute(ref PhysicsCollider physicsCollider, in Parent parent)
            {
                if (physicsCollider.Value.Value.GetCollisionFilter().GroupIndex != 0) return;


                var colliderBlobAssetReference = physicsCollider.Value.Value.Clone();

                FieldOfViewCollisionFilter.GroupIndex = -parent.Value.Index;
                colliderBlobAssetReference.Value.SetCollisionFilter(FieldOfViewCollisionFilter);
                physicsCollider.Value = colliderBlobAssetReference;
            }
        }
    }
}