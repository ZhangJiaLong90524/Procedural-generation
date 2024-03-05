using Authoring.GameEntity.PrefabPackage;
using Components.GameWorld.GameChunk.GameEntity.Generation;
using Components.GameWorld.GameChunk.GameEntity.MobEntity;
using Components.Tags.GameEntity.MobEntity.UpdateStates;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Authoring.GameEntity.Prefab
{
    [RequireComponent(typeof(GameEntityAuthoring))]
    public class MovementModuleAuthoring : MonoBehaviour
    {
        public float moveSpeedMetersPerSec;
        public float turnSpeedDegreesPerSec;

        private void OnValidate()
        {
            if (gameObject.GetComponent<GameEntityAuthoring>().gameEntityType is GameEntityAuthoring.GameEntityType
                    .StaticEntity)
                Debug.LogError("MovementPropertiesAuthoring is only valid for MobEntity");
        }
    }

    [BakingType]
    public struct MovementPropertiesBaking : IComponentData
    {
        [ReadOnly] public float MoveSpeedMetersPerSec;
        [ReadOnly] public float TurnSpeedDegreesPerSec;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct MovementModuleBakingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var query in SystemAPI.Query<RefRO<EntityPrefab>, RefRO<MovementPropertiesBaking>>())
            {
                var entityPrefab = query.Item1.ValueRO.Entity;
                var movementPropertiesBaking = query.Item2.ValueRO;
                ecb.AddSharedComponent(entityPrefab, new MovementPropertiesShared
                {
                    MoveSpeedMetersPerSec = movementPropertiesBaking.MoveSpeedMetersPerSec,
                    TurnSpeedDegreesPerSec = movementPropertiesBaking.TurnSpeedDegreesPerSec
                });

                ecb.AddComponent<DestinationPosition>(entityPrefab);

                ecb.AddComponent<DeterminingDestinationPosition>(entityPrefab);

                ecb.RemoveComponent<MovementPropertiesBaking>(entityPrefab);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}