using Authoring.GameEntity.PrefabPackage;
using Components.GameWorld.GameChunk.GameEntity;
using Components.GameWorld.GameChunk.GameEntity.Generation;
using Components.Tags.GameEntity.InitializeStates.PerceptiveModule;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Authoring.GameEntity.Prefab
{
    [RequireComponent(typeof(GameEntityAuthoring))]
    public class PerceptiveModuleAuthoring : MonoBehaviour
    {
    }

    [BakingType]
    public struct PerceptiveBaking : IComponentData
    {
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct PerceptiveModuleBakingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var query in SystemAPI.Query<RefRO<EntityPrefab>>().WithAll<PerceptiveBaking>())
            {
                var entityPrefab = query.ValueRO.Entity;

                ecb.AddComponent<GeneratingFieldOfViews>(entityPrefab);


                ecb.AddBuffer<ObservedGameEntitiesDataBuffer>(entityPrefab);


                ecb.RemoveComponent<PerceptiveBaking>(entityPrefab);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}