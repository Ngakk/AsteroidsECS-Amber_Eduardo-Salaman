using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class BoundaryAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public Vector3 Min;
    public Vector3 Max;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Boundary
        {
            Min = new float2(Min.x, Min.y),
            Max = new float2(Max.x, Max.y),
            Width = Max.x - Min.x,
            Height = Max.y - Min.y,
        });
    }
}

public struct Boundary : IComponentData
{
    public float2 Min;
    public float2 Max;
    public float Width;
    public float Height;
}
