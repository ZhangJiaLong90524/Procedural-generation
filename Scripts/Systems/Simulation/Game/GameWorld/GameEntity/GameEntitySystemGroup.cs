using Unity.Entities;

namespace Systems.Simulation.Game.GameWorld.GameEntity
{
    [UpdateInGroup(typeof(GameWorldSystemGroup), OrderLast = true)]
    public partial class GameEntitySystemGroup : ComponentSystemGroup
    {
    }
}