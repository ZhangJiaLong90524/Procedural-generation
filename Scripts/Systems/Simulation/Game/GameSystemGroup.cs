using Unity.Entities;

namespace Systems.Simulation.Game
{
    [UpdateInGroup(typeof(SimulationGameSystemGroup))]
    public partial class GameSystemGroup : ComponentSystemGroup
    {
    }
}