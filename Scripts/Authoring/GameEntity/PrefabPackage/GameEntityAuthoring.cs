using System;
using Authoring.GameEntity.Prefab;
using Components.GameWorld.GameChunk;
using Components.GameWorld.GameChunk.GameEntity.Generation;
using Components.Tags.GameEntity;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Authoring.GameEntity.PrefabPackage
{
    public class GameEntityAuthoring : MonoBehaviour
    {
        public enum GameEntityType
        {
            MobEntity,
            StaticEntity
        }

        public GameObject prefab;
        public GameEntityType gameEntityType;
    }

    public class GameEntityPrefabAndPrefabPackageBaker : Baker<GameEntityAuthoring>
    {
        public override void Bake(GameEntityAuthoring authoring)
        {
            var prefab = authoring.prefab;

            DependsOn(prefab);

            var packageEntity = GetEntity(TransformUsageFlags.None);

            AddComponent<PrefabPackageEntity>(packageEntity);

            if (authoring.gameEntityType == GameEntityAuthoring.GameEntityType.MobEntity)
                BakeMobEntity(packageEntity, prefab);
            else
                BakeStaticEntity(packageEntity, prefab);

            var authoringGameObject = authoring.gameObject;
            if (authoringGameObject.TryGetComponent<SampleGridGenerationModuleAuthoring>(
                    out var sampleGridGenerationAuthoring))
            {
                var collisionMeshSourceGameObject = sampleGridGenerationAuthoring.collisionMeshSourceGameObject;
                var collisionMesh = prefab;

                if (collisionMeshSourceGameObject != null)
                {
                    DependsOn(collisionMeshSourceGameObject);
                    collisionMesh = collisionMeshSourceGameObject;
                }

                var prefabCollisionRadius = collisionMesh.GetComponent<MeshRenderer>().bounds.extents.magnitude;
                AddComponent(packageEntity, new CollisionProperties
                {
                    CollisionRadius = prefabCollisionRadius,
                    ApproximateCollisionSquareSideLengthHalf =
                        Mathf.Sqrt(prefabCollisionRadius * prefabCollisionRadius / 2)
                });
            }

            if (authoringGameObject.TryGetComponent<GenerationLimitModuleAuthoring>(
                    out var generationCountLimitAuthoring))
                AddComponent(packageEntity, new GenerationCountLimit
                {
                    Max = generationCountLimitAuthoring.max,
                    Min = generationCountLimitAuthoring.min
                });

            #region EntityPrefabBaking

            if (authoringGameObject.TryGetComponent<ExistenceModuleAuthoring>(out var existenceAuthoring))
                AddComponent(packageEntity, new ExistencePropertiesBaking
                {
                    AverageErasureInteractionCount = existenceAuthoring.averageErasureInteractionCount
                });

            if (authoringGameObject.TryGetComponent<PerceptiveModuleAuthoring>(out var fieldOfViewAuthoring))
                AddComponent<PerceptiveBaking>(packageEntity);

            if (authoringGameObject.TryGetComponent<MovementModuleAuthoring>(out var movementPropertiesAuthoring))
                AddComponent(packageEntity, new MovementPropertiesBaking
                {
                    MoveSpeedMetersPerSec = movementPropertiesAuthoring.moveSpeedMetersPerSec,
                    TurnSpeedDegreesPerSec = movementPropertiesAuthoring.turnSpeedDegreesPerSec
                });

            #endregion
        }

        private void BakeMobEntity(Entity packageEntity, GameObject prefab)
        {
            var gameEntityTagType = "Components.Tags.GameEntity.MobEntity." + prefab.name;
            AddComponent(packageEntity, Type.GetType(gameEntityTagType)); //not supported by burst
            AddComponent(packageEntity, new GameEntityTagBaking(gameEntityTagType));

            AddComponent(packageEntity, new EntityPrefab
            {
                Entity = GetEntity(prefab, TransformUsageFlags.Dynamic)
            });
        }

        private void BakeStaticEntity(Entity packageEntity, GameObject entityPrefab)
        {
            var gameEntityTagType = "Components.Tags.GameEntity.StaticEntity." + entityPrefab.name;
            AddComponent(packageEntity, Type.GetType(gameEntityTagType)); //not supported by burst
            AddComponent(packageEntity, new GameEntityTagBaking(gameEntityTagType));

            AddComponent(packageEntity, new EntityPrefab
            {
                Entity = GetEntity(entityPrefab, TransformUsageFlags.Renderable)
            });
        }
    }

    [BakingType]
    public struct GameEntityTagBaking : IComponentData
    {
        public FixedString64Bytes TypeString;

        public GameEntityTagBaking(FixedString64Bytes typeString)
        {
            TypeString = typeString;
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct EntityPrefabAdditionalComponentsBakingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var query in SystemAPI.Query<RefRO<EntityPrefab>, RefRO<GameEntityTagBaking>>())
            {
                var entityPrefab = query.Item1.ValueRO.Entity;

                ecb.AddComponent(entityPrefab,
                    Type.GetType(query.Item2.ValueRO.TypeString.ToString())); //not supported by burst

                ecb.AddComponent<ChunkPosition>(entityPrefab);

                ecb.RemoveComponent<GameEntityTagBaking>(entityPrefab);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}