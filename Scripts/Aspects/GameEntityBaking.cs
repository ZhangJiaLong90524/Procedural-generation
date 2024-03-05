// using Authoring;
// using Components.GameEntity;
// using Unity.Entities;
//
// namespace Aspects
// {
//     // public readonly partial struct GameEntityBakingAspect : IAspect
//     // {
//     //     private readonly RefRO<EntityPrefab> _entityPrefab;
//     //     public Entity EntityPrefab => _entityPrefab.ValueRO.Entity;
//     //
//     //     private readonly RefRO<GameEntityPropertiesBaking> _gameEntityPropertiesBaking;
//     //     public bool CanBeDestroyed => _gameEntityPropertiesBaking.ValueRO.CanBeDestroyed;
//     // }
//
//     public readonly partial struct MobEntityBakingAspect : IAspect
//     {
//         private readonly RefRO<EntityPrefab> _entityPrefab;
//         public Entity EntityPrefab => _entityPrefab.ValueRO.Entity;
//
//         private readonly RefRO<GameEntityPropertiesBaking> _gameEntityPropertiesBaking;
//         public bool CanBeDestroyed => _gameEntityPropertiesBaking.ValueRO.CanBeDestroyed;
//
//         private readonly RefRO<MobEntityPropertiesBaking> _mobEntityPropertiesBaking;
//         public uint MaximumHealthPoint => _mobEntityPropertiesBaking.ValueRO.MaximumHealthPoint;
//     }
//
//     public readonly partial struct StaticEntityBakingAspect : IAspect
//     {
//         private readonly RefRO<EntityPrefab> _entityPrefab;
//         public Entity EntityPrefab => _entityPrefab.ValueRO.Entity;
//
//         private readonly RefRO<GameEntityPropertiesBaking> _gameEntityPropertiesBaking;
//         public bool CanBeDestroyed => _gameEntityPropertiesBaking.ValueRO.CanBeDestroyed;
//
//         private readonly RefRO<StaticEntityPropertiesBaking> _staticEntityPropertiesBaking;
//
//         public uint AverageDestructionInteractionCount =>
//             _staticEntityPropertiesBaking.ValueRO.AverageDestructionInteractionCount;
//     }
// }

