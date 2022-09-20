using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Rendering;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(EndFramePhysicsSystem))]
public abstract partial class CollisionsSystem<T1, T2> : SystemBase where T1 : struct, IComponentData where T2 : struct, IComponentData
{
    protected StepPhysicsWorld m_StepPhysicsWorld = default;
    protected EntityQuery m_Query = default;

    protected override void OnCreate()
    {
        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        m_Query = GetEntityQuery(new EntityQueryDesc
        {
            Any = new ComponentType[] { typeof(T1), typeof(T2) }
        });
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        this.RegisterPhysicsRuntimeSystemReadOnly();
    }

    protected NativeList<TriggerEvent> GetTriggerEvents()
    {
        NativeList<TriggerEvent>  triggerEvents = new NativeList<TriggerEvent>(Allocator.Persistent);

        Dependency = new CollectTriggerEvents
        {
            TriggerEvents = triggerEvents
        }
        .Schedule(m_StepPhysicsWorld.Simulation, Dependency);

        return triggerEvents;
    }
}

[BurstCompile]
public struct CollectTriggerEvents : ITriggerEventsJob
{
    public NativeList<TriggerEvent> TriggerEvents;

    public void Execute(TriggerEvent triggerEvent)
    {
        TriggerEvents.Add(triggerEvent);
    }
}
