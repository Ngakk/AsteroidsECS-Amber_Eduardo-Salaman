using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class EnableOnShipAdvanceAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{ 
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new EnableOnShipAdvanceTag());
    }
}

public struct EnableOnShipAdvanceTag : IComponentData { }