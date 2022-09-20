using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;

public struct Asteroid : IComponentData { }

[DisallowMultipleComponent]
public class AsteroidAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Asteroid());
    }
}
