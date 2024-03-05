using Unity.Entities;

namespace Systems.Simulation.Game.GameWorld
{
    [UpdateInGroup(typeof(GameSystemGroup), OrderLast = true)]
    public partial class GameWorldSystemGroup : ComponentSystemGroup
    {
    }
}