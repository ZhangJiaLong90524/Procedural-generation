using Unity.Entities;
using Unity.Mathematics;

namespace Components
{
    public struct MathematicsRandom : IComponentData
    {
        public Random Random;

        public MathematicsRandom(Random random)
        {
            Random = random;
        }
    }
}