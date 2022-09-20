using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct SystemStateTag : ISystemStateComponentData { }

[Serializable]
public struct RespawningStateTag : ISystemStateComponentData
{
    public float TimeToRespawnRemaining;
}
