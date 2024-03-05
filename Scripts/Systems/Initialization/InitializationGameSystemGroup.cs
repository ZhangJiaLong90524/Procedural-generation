using Unity.Entities;

namespace Systems.Initialization
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class InitializationGameSystemGroup : ComponentSystemGroup
    {
    }
}