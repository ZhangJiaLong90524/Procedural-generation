using Unity.Entities;

namespace Systems.Simulation.Game.GameWorld.GameEntity.MobEntity
{
    [UpdateInGroup(typeof(GameEntitySystemGroup), OrderLast = true)]
    public partial class MobEntitySystemGroup : ComponentSystemGroup
    {
    }
}