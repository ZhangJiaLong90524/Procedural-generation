using Unity.Entities;

namespace Systems.Simulation
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class SimulationGameSystemGroup : ComponentSystemGroup
    {
    }
}