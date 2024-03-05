using UnityEngine;

namespace Authoring.GameEntity.PrefabPackage
{
    [RequireComponent(typeof(GameEntityAuthoring))]
    public class SampleGridGenerationModuleAuthoring : MonoBehaviour
    {
        public GameObject collisionMeshSourceGameObject;
    }
}