using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEditor.Tilemaps;
using UnityEngine;

[DisallowMultipleComponent]
public class ShipAuthoring : MonoBehaviour
{
    [Header("Shoot Settings")]
    public GameObject BulletPrefab;
    public float ShootCooldown; //Time between shots
    public float BulletSpeed;
    [Header("Movement Settings")]
    public float MaxSpeed = 2f;
    public float Acceleration = 8f;
    public float Drag = 4f;
    public float MaxAngularSpeed = 2f;
    public float AngularAcceleration = 8f;
    public float AngularDrag = 4f;
    [Header("Respawn")]
    public float RespawnTime = 5f;
}

/// <summary>
/// Saves detected player input on Simulation System to be later processed at Fixed Step Simulation System
/// </summary>
public struct PlayerInput : IComponentData
{
    public bool Advance;
    public int RotationDir;
    public bool Shoot;
}

/// <summary>
/// Ship stats for read only purposes
/// </summary>
public struct ShipSettings : IComponentData 
{
    //Movement settings
    public float MaxSpeed;
    public float Acceleration;
    public float Drag;
    public float MaxAngularSpeed;
    public float AngularAcceleration;
    public float AngularDrag;
    //Shoot settings
    public Entity BulletPrefab;
    public float BulletSpeed;
    public float ShootCooldown;
    //Respawn
    public float RespawnTime;
}

/// <summary>
/// Helps track rate of fire
/// </summary>
public struct ShootTimer : IComponentData
{
    public float LastShootTime;
    public float ShotLifetime;
}

[UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
class ShipConverterDeclare : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ShipAuthoring shipAuthoring) =>
        {
            DeclareReferencedPrefab(shipAuthoring.BulletPrefab);
        });
    }
}

class ShipConverter : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ShipAuthoring shipAuthoring) =>
        {
            var entity = GetPrimaryEntity(shipAuthoring);
            var prefab = GetPrimaryEntity(shipAuthoring.BulletPrefab);

            var settings = new ShipSettings 
            { 
                BulletPrefab = prefab,
                MaxSpeed = shipAuthoring.MaxSpeed,
                Acceleration = shipAuthoring.Acceleration,
                Drag = shipAuthoring.Drag,
                MaxAngularSpeed = shipAuthoring.MaxAngularSpeed,
                AngularAcceleration = shipAuthoring.AngularAcceleration,
                AngularDrag = shipAuthoring.AngularDrag,
                BulletSpeed = shipAuthoring.BulletSpeed,
                ShootCooldown = shipAuthoring.ShootCooldown,
                RespawnTime = shipAuthoring.RespawnTime,
            };
            DstEntityManager.AddComponentData(entity, settings);

            var data = new PlayerInput
            {
                Advance = false,
                RotationDir = 0,
                Shoot = false,
            };
            DstEntityManager.AddComponentData(entity, data);

            var shooter = new ShootTimer
            {
                LastShootTime = -shipAuthoring.ShootCooldown,
            };
            DstEntityManager.AddComponentData(entity, shooter);
        });
    }
}
