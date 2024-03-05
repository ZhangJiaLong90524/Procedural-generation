using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class SortComponents : MonoBehaviour
{
    public static void SortComponentsByNames(Component sortComponentsMonoBehaviour)
    {
        var gameObject = sortComponentsMonoBehaviour.gameObject;
        UnPackGameObject(gameObject);

        while (ComponentUtility.MoveComponentUp(sortComponentsMonoBehaviour))
        {
        }

        var sortedComponents = gameObject.GetComponents<Component>()
            .Where(c => c is not Transform && c is not SortComponents).ToArray().OrderBy(c => c.GetType().Name)
            .ToArray();

        for (var targetIndex = 0; targetIndex < sortedComponents.Length; targetIndex++)
        {
            var sortingComponent = sortedComponents[targetIndex];

            var currentComponents = gameObject.GetComponents<Component>()
                .Where(c => c is not Transform && c is not SortComponents).ToArray();
            var currentIndex = Array.IndexOf(currentComponents, sortingComponent);

            var offset = currentIndex - targetIndex;
            while (offset != 0)
                if (offset < 0)
                {
                    ComponentUtility.MoveComponentDown(sortingComponent);
                    offset++;
                }
                else
                {
                    ComponentUtility.MoveComponentUp(sortingComponent);
                    offset--;
                }
        }

        Debug.Log($"Sorted components on {gameObject.name}");
    }

    private static void UnPackGameObject(GameObject gameObject)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
        {
            PrefabUtility.UnpackPrefabInstance(gameObject, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            Debug.Log($"Unpacked {gameObject.name}");
        }
    }
}