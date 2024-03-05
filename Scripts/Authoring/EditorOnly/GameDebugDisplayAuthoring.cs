#if UNITY_EDITOR
using Unity.Entities;
using UnityEngine;

namespace Authoring.EditorOnly
{
    public class GameDebugDisplayAuthoring : MonoBehaviour
    {
        public bool drawGenerateStaticEntitySystem;
        public bool drawBufferVisibleEntitiesSystem;
        public bool drawDestinationPosition;
    }

    public struct GameDebugDisplayData : IComponentData
    {
        public int DrawGenerateStaticEntitySystem;
        public int DrawBufferVisibleEntitiesSystem;
        public int DrawDestinationPosition;
    }

    public class GameDebugDisplayAuthoringBaker : Baker<GameDebugDisplayAuthoring>
    {
        public override void Bake(GameDebugDisplayAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity,
                new GameDebugDisplayData
                {
                    DrawGenerateStaticEntitySystem = authoring.drawGenerateStaticEntitySystem ? 1 : 0,
                    DrawBufferVisibleEntitiesSystem = authoring.drawBufferVisibleEntitiesSystem ? 1 : 0,
                    DrawDestinationPosition = authoring.drawDestinationPosition ? 1 : 0
                });
        }
    }
}
#endif