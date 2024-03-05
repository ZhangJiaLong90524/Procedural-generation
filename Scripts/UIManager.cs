using Systems.Simulation.Game;
using Unity.Entities;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject mainMenu;

    private void FixedUpdate()
    {
        // if (Input.GetKeyDown(KeyCode.R))
        // {
        //     var query = new EntityQueryBuilder(Allocator.Temp).WithAny<StaticEntityPropertiesShared>()
        //         .WithAny<MobEntityCurrentState>()
        //         .Build(_entityManager);
        //     foreach (var entity in query.ToEntityArray(Allocator.Temp)) _entityManager.DestroyEntity(entity);
        //     GameStart();
        // }
    }

    public void GameStart()
    {
        mainMenu.SetActive(false);

        var world = World.DefaultGameObjectInjectionWorld.Unmanaged;
        world.GetExistingSystemState<CreateNewWorldSystem>().Enabled = true;
    }
}