using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public partial class AsteroidBulletCollision : CollisionsSystem<Asteroid, Bullet>
{
    private EndFixedStepSimulationEntityCommandBufferSystem m_CommandBufferSystem;
    

    protected override void OnCreate()
    {
        base.OnCreate();

        m_CommandBufferSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        if (m_Query.CalculateEntityCount() < 2) return;

        var commandBuffer = m_CommandBufferSystem.CreateCommandBuffer();
        var sys = this;

        NativeList<TriggerEvent> triggerEvents = GetTriggerEvents();

        Job.WithBurst()
            .WithCode(() =>
            {
                for (int i = 0; i < triggerEvents.Length; i++)
                {
                    bool isAAsteroid = HasComponent<Asteroid>(triggerEvents[i].EntityA);
                    bool isBAsteroid = HasComponent<Asteroid>(triggerEvents[i].EntityB);

                    bool isABullet = HasComponent<Bullet>(triggerEvents[i].EntityA);
                    bool isBBullet = HasComponent<Bullet>(triggerEvents[i].EntityB);

                    if (isAAsteroid && isBBullet || isBAsteroid && isABullet)
                    {
                        commandBuffer.DestroyEntity(triggerEvents[i].EntityA);
                        commandBuffer.DestroyEntity(triggerEvents[i].EntityB);
                    }
                }
            })
        .Schedule();

        m_CommandBufferSystem.AddJobHandleForProducer(Dependency);

        Dependency.Complete();
        triggerEvents.Dispose();
    }
}

public partial class AsteroidShipCollision : CollisionsSystem<Asteroid, ShipSettings>
{
    private EndFixedStepSimulationEntityCommandBufferSystem m_CommandBufferSystem;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_CommandBufferSystem = World.GetOrCreateSystem<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        if (m_Query.CalculateEntityCount() < 2) return;

        var commandBuffer = m_CommandBufferSystem.CreateCommandBuffer();

        NativeList<TriggerEvent> triggerEvents = GetTriggerEvents();

        Job.WithBurst()
            .WithCode(() =>
            {
                for (int i = 0; i < triggerEvents.Length; i++)
                {
                    bool isAAsteroid = HasComponent<Asteroid>(triggerEvents[i].EntityA);
                    bool isBAsteroid = HasComponent<Asteroid>(triggerEvents[i].EntityB);

                    bool isAShip = HasComponent<ShipSettings>(triggerEvents[i].EntityA);
                    bool isBShip = HasComponent<ShipSettings>(triggerEvents[i].EntityB);

                    if (isAAsteroid && isBShip || isBAsteroid && isAShip)
                    {
                        if (isAShip)
                            commandBuffer.AddComponent<Disabled>(triggerEvents[i].EntityA);
                        if (isBShip)
                            commandBuffer.AddComponent<Disabled>(triggerEvents[i].EntityB);
                    }
                }
            })
        .Schedule();

        m_CommandBufferSystem.AddJobHandleForProducer(Dependency);

        Dependency.Complete();
        triggerEvents.Dispose();
    }
}

