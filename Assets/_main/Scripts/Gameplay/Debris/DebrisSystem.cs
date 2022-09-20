using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

using Random = Unity.Mathematics.Random;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
public partial class DebrisSystem : SystemBase
{
    EntityQuery m_NewEntitiesQuery;
    EntityQuery m_ProcessEntityQuery;
    EntityQuery m_OnDestroyedQuery;
    EntityCommandBufferSystem m_ECBSource;
    Random m_Random;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_NewEntitiesQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<DebrisSpawner>() },
            None = new ComponentType[] { ComponentType.ReadOnly<DebrisSpawnerState>() }
        });

        m_ProcessEntityQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<DebrisSpawner>(),
                ComponentType.ReadOnly<DebrisSpawnerState>()}
        });

        m_OnDestroyedQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<DebrisSpawnerState>() },
            None = new ComponentType[] { ComponentType.ReadOnly<DebrisSpawner>() }
        },
        new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<DebrisSpawnerState>(), 
                ComponentType.ReadOnly<DebrisSpawner>(),
                ComponentType.ReadOnly<Disabled>()
            }
        });

        m_ECBSource = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_Random = Random.CreateFromIndex((uint)DateTime.Now.Ticks);
    }

    protected override void OnUpdate()
    {
        var newEntityJob = new NewEntityJob() { ECB = m_ECBSource.CreateCommandBuffer() };
        var newEntityJobHandle = newEntityJob.Schedule(m_NewEntitiesQuery);
        m_ECBSource.AddJobHandleForProducer(newEntityJobHandle);

        var processJob = new ProcessEntityJob();
        var processJobHandle = processJob.Schedule(m_ProcessEntityQuery);
        m_ECBSource.AddJobHandleForProducer(processJobHandle);

        var destroyedJob = new OnDestroyedJob() { ECB = m_ECBSource.CreateCommandBuffer(), Random = m_Random };
        var destroyedJobHandle = destroyedJob.Schedule(m_OnDestroyedQuery);
        m_ECBSource.AddJobHandleForProducer(destroyedJobHandle);
    }

    partial struct NewEntityJob : IJobEntity
    {
        public EntityCommandBuffer ECB;

        public void Execute(in Entity e, in DebrisSpawner spawner)
        {
            ECB.AddComponent(e, new DebrisSpawnerState
            {
                Prefab = spawner.Prefab,
                Amount = spawner.Amount
            });
        }
    }

    partial struct ProcessEntityJob : IJobEntity
    {
        public void Execute(ref DebrisSpawnerState state, in Translation pos)
        {
            state.Position = pos.Value;
        }
    }

    partial struct OnDestroyedJob : IJobEntity 
    {
        public EntityCommandBuffer ECB;
        public Random Random;

        public void Execute(in Entity e, in DebrisSpawnerState spawner)
        {
            NativeArray<Entity> debris = new NativeArray<Entity>(spawner.Amount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            ECB.Instantiate(spawner.Prefab, debris);

            for(int i = 0; i < spawner.Amount; i++)
            {
                float3 dir = math.normalize(Random.NextFloat3(new float3(-1, -1, 0), new float3(1, 1, 0)));
                float magnitude = Random.NextFloat(2, 4);
                PhysicsVelocity velocity = new PhysicsVelocity 
                { 
                    Linear = dir * magnitude ,
                    Angular = new float3(0, 0, Random.NextFloat(-5, 5))
                };


                ECB.SetComponent(debris[i], velocity);
                ECB.SetComponent(debris[i], new Translation { Value = spawner.Position });
            }


            ECB.RemoveComponent<DebrisSpawnerState>(e);
        }
    }

}
