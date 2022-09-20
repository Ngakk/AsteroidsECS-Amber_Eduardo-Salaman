using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;

using Random = Unity.Mathematics.Random;
using Unity.VisualScripting;
using System;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
[DisallowMultipleComponent]
public class AsteroidSpawnerAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public GameObject AsteroidPrefab;
    public float CoolDownSeconds;
    public int GenerateMaxCount = 100;
    public float SafeAreaWidth = 2f;
    public float SafeAreaHeight = 2f;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new AsteroidSpawner
        {
            AsteroidPrefab = conversionSystem.GetPrimaryEntity(AsteroidPrefab),
            CoolDownSeconds = CoolDownSeconds,
            TimeUntilNextSpawn = CoolDownSeconds,
            Random = new Random(0xDBC19 * (uint)entity.Index),
            SafeAreaMin = new float3(-SafeAreaWidth/2, -SafeAreaHeight/2, 0),
            SafeAreaW = SafeAreaWidth,
            SafeAreaH = SafeAreaHeight
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(AsteroidPrefab);
    }
}

public struct AsteroidSpawner : IComponentData
{
    public Entity AsteroidPrefab;
    public float CoolDownSeconds;
    public float TimeUntilNextSpawn;
    public Random Random;
    public float3 SafeAreaMin;
    public float SafeAreaW;
    public float SafeAreaH;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
public partial class AsteroidSpawnerSystem : SystemBase
{
    EntityQuery m_Query;
    Boundary m_Boundary;
    EntityQuery m_PlayerQuery;

    protected override void OnCreate()
    {
        m_Query = GetEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[] { typeof(Asteroid) }
        });
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        m_Boundary = GetSingleton<Boundary>();

        m_PlayerQuery = GetEntityQuery(new EntityQueryDesc 
            { All = new ComponentType[] { ComponentType.ReadOnly<ShipSettings>(), ComponentType.ReadWrite<Translation>() } });
    }

    protected override void OnUpdate()
    {
        if (m_Query.CalculateEntityCount() != 0) return; //Only respawn asteroids when none are left
        if (m_PlayerQuery.CalculateEntityCount() != 1) return;

        float dt = Time.DeltaTime;
        Boundary boundary = m_Boundary;
        float3 playerPos = m_PlayerQuery.GetSingleton<Translation>().Value;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.ForEach((ref AsteroidSpawner spawner) =>
        {
            spawner.TimeUntilNextSpawn -= dt;
            if (spawner.TimeUntilNextSpawn <= 0)
            {
                spawner.SafeAreaMin = playerPos - new float3(spawner.SafeAreaW / 2, spawner.SafeAreaH / 2, 0);

                int count = 4;
                NativeArray<Entity> entities = new NativeArray<Entity>(count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                ecb.Instantiate(spawner.AsteroidPrefab, entities);

                for (int i = 0; i < count; i++)
                {
                    //Vel
                    float3 dir = math.normalize(spawner.Random.NextFloat3(new float3(-1, -1, 0), new float3(1, 1, 0)));
                    float magnitude = spawner.Random.NextFloat(1, 3);
                    PhysicsVelocity velocity = new PhysicsVelocity { Linear = dir * magnitude };
                    ecb.SetComponent(entities[i], velocity);

                    //Get the total available area
                    float2 totalArea = new float2(  boundary.Max.x - boundary.Min.x - spawner.SafeAreaW,
                                                    boundary.Max.y - boundary.Min.y - spawner.SafeAreaH);

                    //Get a random point in that area
                    float posX = spawner.Random.NextFloat(boundary.Min.x, boundary.Min.x + totalArea.x);
                    float posY = spawner.Random.NextFloat(boundary.Min.y, boundary.Min.y + totalArea.y);

                    //Make the pos skip over the safe area
                    if (posX > spawner.SafeAreaMin.x) posX += spawner.SafeAreaW;
                    if (posY > spawner.SafeAreaMin.y) posY += spawner.SafeAreaH;

                    ecb.SetComponent(entities[i], new Translation { Value = new float3(posX, posY, 0) });
                }

                spawner.TimeUntilNextSpawn = spawner.CoolDownSeconds;
            }
        })
        .Schedule();

        Dependency.Complete();

        ecb.Playback(EntityManager);

        ecb.Dispose();
    }
}
