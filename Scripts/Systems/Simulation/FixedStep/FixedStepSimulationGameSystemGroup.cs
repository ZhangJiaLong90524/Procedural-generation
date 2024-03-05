using Unity.Entities;
using Unity.Physics.Systems;

namespace Systems.Simulation.FixedStep
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial class FixedStepSimulationGameSystemGroup : ComponentSystemGroup
    {
    }
}