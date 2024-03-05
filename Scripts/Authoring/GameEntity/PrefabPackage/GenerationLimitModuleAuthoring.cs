using UnityEngine;

namespace Authoring.GameEntity.PrefabPackage
{
    [RequireComponent(typeof(GameEntityAuthoring))]
    public class GenerationLimitModuleAuthoring : MonoBehaviour
    {
        public ushort min;
        public ushort max;
    }
}