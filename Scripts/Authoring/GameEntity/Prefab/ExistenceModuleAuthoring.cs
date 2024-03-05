using Authoring.GameEntity.PrefabPackage;
using Components.GameWorld.GameChunk.GameEntity;
using Components.GameWorld.GameChunk.GameEntity.Generation;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Authoring.GameEntity.Prefab
{
    [RequireComponent(typeof(GameEntityAuthoring))]
    public class ExistenceModuleAuthoring : MonoBehaviour
    {
        public uint averageErasureInteractionCount;
    }

    [BakingType]
    public struct ExistencePropertiesBaking : IComponentData
    {
        public uint AverageErasureInteractionCount;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct ExistenceModuleBakingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var query in SystemAPI.Query<RefRO<EntityPrefab>, RefRO<ExistencePropertiesBaking>>())
            {
                var entityPrefab = query.Item1.ValueRO.Entity;

                ecb.AddSharedComponent(entityPrefab, new AverageErasureInteractionCountShared
                {
                    Value = query.Item2.ValueRO.AverageErasureInteractionCount
                });

                ecb.AddComponent(entityPrefab, new CurrentErasureInteractionCount
                {
                    Value = 0
                });

                ecb.RemoveComponent<ExistencePropertiesBaking>(entityPrefab);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}