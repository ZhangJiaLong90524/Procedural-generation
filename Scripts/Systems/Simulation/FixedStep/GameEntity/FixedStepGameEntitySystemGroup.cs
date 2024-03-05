using Unity.Entities;

namespace Systems.Simulation.FixedStep.GameEntity
{
    [UpdateInGroup(typeof(FixedStepSimulationGameSystemGroup))]
    public partial class FixedStepGameEntitySystemGroup : ComponentSystemGroup
    {
    }
}