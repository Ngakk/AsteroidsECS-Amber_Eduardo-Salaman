using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class DebrisSpawnerAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GameObject Prefab;
    public int Amount;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new DebrisSpawner
        {
            Prefab = conversionSystem.GetPrimaryEntity(Prefab),
            Amount = Amount
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(Prefab);
    }
}

public struct DebrisSpawner : IComponentData 
{
    public Entity Prefab;
    public int Amount;
}

public struct DebrisSpawnerState : ISystemStateComponentData 
{
    public Entity Prefab;
    public int Amount;
    public float3 Position;
}