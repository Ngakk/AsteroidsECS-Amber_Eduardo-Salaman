using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Captures player input and saves it for usage later in the physics stepS
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
public partial class ShipController : SystemBase
{
    protected override void OnUpdate()
    {
        float time = (float)Time.ElapsedTime;
        float dt = Time.DeltaTime;
        bool advance = Input.GetKey(KeyCode.W);

        bool right = Input.GetKey(KeyCode.D);
        bool left = Input.GetKey(KeyCode.A);
        bool shoot = Input.GetKey(KeyCode.Space);

        Entities.ForEach((ref PlayerInput input) =>
        {
            input.Advance = advance;

            if (input.RotationDir != 1)
            {
                if (right) input.RotationDir = -1;
                else if (left) input.RotationDir = 1;
            }

            if (!right && !left)
            {
                input.RotationDir = 0;
            }

            input.Shoot = shoot;
        })
        .Schedule();
    }
}

/// <summary>
/// Updates the physics related values of the ship and takes care of spawning bullets if requested.
/// </summary>
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(EndFramePhysicsSystem))]
public partial class ShipSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float time = (float)Time.ElapsedTime;
        float dt = Time.DeltaTime;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.ForEach((ref PhysicsVelocity velocity, ref ShootTimer shooter, in Rotation rotation, in Translation translation, in PlayerInput input, in ShipSettings settings) =>
        {
            //Forward input
            if (input.Advance) 
            {
                //Accelerate
                velocity.Linear += math.mul(rotation.Value, new float3(settings.Acceleration, 0, 0)) * dt;
                if (math.distance(velocity.Linear, Velocity.Zero.Linear) > settings.MaxSpeed) //Cap speed
                {
                    velocity.Linear = math.normalize(velocity.Linear) * settings.MaxSpeed;
                }
            }
            else if (velocity.Linear.x != 0 || velocity.Linear.y != 0)
            {
                velocity.Linear -= velocity.Linear * settings.Drag * dt; //Slow down
            }

            //Rotation input
            if (input.RotationDir != 0)
            {
                //Accelerate in the given rotation direction
                velocity.Angular += new float3(0, 0, input.RotationDir * settings.AngularAcceleration) * dt;
                if (math.distance(velocity.Angular, Velocity.Zero.Angular) > settings.MaxAngularSpeed) //Cap speed
                {
                    velocity.Angular = math.normalize(velocity.Angular) * settings.MaxAngularSpeed;
                }
            }
            else
            {
                velocity.Angular -= velocity.Angular * settings.AngularDrag * dt; //Slow down
            }

            //Shoot input
            if (input.Shoot && time > shooter.LastShootTime + settings.ShootCooldown)
            {
                var newBullet = ecb.Instantiate(settings.BulletPrefab);

                InitilizeBullet(ecb, newBullet,
                    translation.Value,
                    rotation.Value,
                    math.mul(rotation.Value, new float3(settings.BulletSpeed, 0, 0)) + velocity.Linear,
                    time,
                    shooter.ShotLifetime);

                shooter.LastShootTime = time;
            }
        })
        .Schedule();

        Dependency.Complete();

        ecb.Playback(EntityManager);

        ecb.Dispose();
    }

    public static void InitilizeBullet(EntityCommandBuffer ecb, Entity e, float3 pos, quaternion rot, float3 vel, float timeStart, float lifetime)
    {
        //ecb.SetComponent(e, new TemporaryLife { StartTime = timeStart, Lifetime = lifetime });
        ecb.SetComponent(e, new Translation { Value = pos });
        ecb.SetComponent(e, new Rotation { Value = rot });
        ecb.SetComponent(e, new PhysicsVelocity { Linear = vel });
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
public partial class ShipReset : SystemBase
{
    EntityQuery m_OnDisabledQuery;
    EntityQuery m_DisabledQuery;
    EntityQuery m_ReEnabledQuery;
    EntityCommandBufferSystem m_ECBSource;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_ECBSource = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_OnDisabledQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<Disabled>(), ComponentType.ReadOnly<ShipSettings>() },
            None = new ComponentType[] { ComponentType.ReadWrite<RespawningStateTag>() }
        });

        m_DisabledQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadOnly<Disabled>(), ComponentType.ReadOnly<ShipSettings>(), ComponentType.ReadWrite<RespawningStateTag>() }
        });

        m_ReEnabledQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { ComponentType.ReadWrite<RespawningStateTag>(), ComponentType.ReadOnly<ShipSettings>() },
        });
    }

    protected override void OnUpdate()
    {
        //On Disable
        var disabledJob = new OnDisabledJob() { ECB = m_ECBSource.CreateCommandBuffer() };
        var disabledJobHandle = disabledJob.Schedule(m_OnDisabledQuery);
        m_ECBSource.AddJobHandleForProducer(disabledJobHandle);

        //While Disabled
        var whileDisabledJob = new WhileDisabledJob() { ECB = m_ECBSource.CreateCommandBuffer(), DeltaTime = Time.DeltaTime };
        var whileDisabledJobHandle = whileDisabledJob.Schedule(m_DisabledQuery);
        m_ECBSource.AddJobHandleForProducer(whileDisabledJobHandle);

        //On Enabled
        var reEnabledJob = new OnReEnabledJob() { ECB = m_ECBSource.CreateCommandBuffer() };
        var reEnabledJobHandle = reEnabledJob.Schedule(m_ReEnabledQuery);
        m_ECBSource.AddJobHandleForProducer(reEnabledJobHandle);
    }

    //Resets player ship on disable
    partial struct OnDisabledJob : IJobEntity
    {
        public EntityCommandBuffer ECB;

        public void Execute(in Entity e, ref PhysicsVelocity vel, ref Translation translation, ref Rotation rot, in ShipSettings settings)
        {
            vel.Linear = Velocity.Zero.Linear;
            vel.Angular = Velocity.Zero.Angular;
            translation.Value = new float3(0, 0, 0);
            rot.Value = quaternion.identity;
            ECB.AddComponent(e, new RespawningStateTag { TimeToRespawnRemaining = settings.RespawnTime });
        }
    }

    //Re enables player ship after a delay
    partial struct WhileDisabledJob : IJobEntity
    {
        public EntityCommandBuffer ECB;
        public float DeltaTime;
        public void Execute(in Entity e, ref RespawningStateTag state)
        {
            state.TimeToRespawnRemaining -= DeltaTime;

            if(state.TimeToRespawnRemaining <= 0)
            {
                ECB.RemoveComponent(e, typeof(Disabled)); //Respawn
            }
        }
    }

    //Setups the player ship system state tag for next disable
    partial struct OnReEnabledJob : IJobEntity
    {
        public EntityCommandBuffer ECB;

        public void Execute(in Entity e)
        {
            ECB.RemoveComponent(e, typeof(RespawningStateTag));
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ShipController))]
public partial class EnableOnAdvanceSystem : SystemBase
{
    EntityQuery m_Query;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_Query = GetEntityQuery(new EntityQueryDesc { All = new ComponentType[] { typeof(PlayerInput) } });
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
    }
    protected override void OnUpdate()
    {
        bool advancing;

        if (m_Query.CalculateEntityCount() == 1)
            advancing = m_Query.GetSingleton<PlayerInput>().Advance;
        else
            advancing = false;

        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities
            .WithAll<EnableOnShipAdvanceTag>()
            .ForEach((in Entity e) =>
        {
            if (!advancing)
            {
                ecb.AddComponent<Disabled>(e);
                Debug.Log("Disabled");
            }
        })
        .Schedule();

        Entities
            .WithEntityQueryOptions(EntityQueryOptions.IncludeDisabled)
            .WithAll<EnableOnShipAdvanceTag>()
            .ForEach((in Entity e) =>
        {
            if (advancing)
            {
                ecb.RemoveComponent<Disabled>(e);
                Debug.Log("Enabled");
            }

        })
        .Schedule();

        Dependency.Complete();

        ecb.Playback(EntityManager);

        ecb.Dispose();
    }
}

