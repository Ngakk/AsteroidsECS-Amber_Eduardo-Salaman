using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class TemporaryLifeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Lifetime;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new TemporaryLife { Lifetime = Lifetime, StartTime = -1 });
    }
}

public struct TemporaryLife : IComponentData
{
    public float Lifetime;
    public float StartTime;
}

public struct TemporaryLifeStateTag : ISystemStateComponentData { }

/// <summary>
/// Destroys entities that have a limited lifetime
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
public partial class TemporaryLifetimeSystem : SystemBase
{
    EntityQuery m_NewEntitiesQuery;
    EntityQuery m_CleanupQuery;
    EntityCommandBufferSystem m_ECBSource;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_NewEntitiesQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadWrite<TemporaryLife>() },
            None = new ComponentType[] { ComponentType.ReadOnly<TemporaryLifeStateTag>(), ComponentType.ReadOnly<Prefab>() }
        });

        m_CleanupQuery = GetEntityQuery(new EntityQueryDesc
        {
            None = new ComponentType[] { ComponentType.ReadOnly<TemporaryLife>(), ComponentType.ReadOnly<Prefab>() },
            All = new ComponentType[] { ComponentType.ReadOnly<TemporaryLifeStateTag>() }
        });

        m_ECBSource = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        float time = (float)Time.ElapsedTime;

        var newEntityJob = new NewEntityJob() { ECB = m_ECBSource.CreateCommandBuffer() , ElapsedTime = time };
        var newEntityJobHandle = newEntityJob.Schedule(m_NewEntitiesQuery);
        m_ECBSource.AddJobHandleForProducer(newEntityJobHandle);

        var cleanupJob = new CleanupJob() { ECB = m_ECBSource.CreateCommandBuffer() };
        var cleanupJobHandle = cleanupJob.Schedule(m_CleanupQuery);
        m_ECBSource.AddJobHandleForProducer(cleanupJobHandle);


        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.WithNone<Prefab>()
            .ForEach((in Entity e, in TemporaryLife life) =>
        {
            if (time > life.StartTime + life.Lifetime)
            {
                ecb.DestroyEntity(e);
            }
        })
        .Schedule();

        Dependency.Complete();

        ecb.Playback(EntityManager);

        ecb.Dispose();
    }

    partial struct NewEntityJob : IJobEntity
    {
        public EntityCommandBuffer ECB;
        public float ElapsedTime;

        public void Execute(in Entity e, ref TemporaryLife life)
        {
            life.StartTime = ElapsedTime;
            ECB.AddComponent(e, new TemporaryLifeStateTag());
        }
    }

    partial struct CleanupJob : IJobEntity
    {
        public EntityCommandBuffer ECB;

        public void Execute(in Entity e)
        {
            ECB.RemoveComponent<TemporaryLifeStateTag>(e);
        }
    }
}
