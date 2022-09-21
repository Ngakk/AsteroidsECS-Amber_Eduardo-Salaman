using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.VisualScripting;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(EndFramePhysicsSystem))]
/// <summary>
/// Makes all included <see cref="Translation"/> warp around if they reach the edges of the boundary
/// </summary>
public partial class BoundarySystem : SystemBase
{
    Boundary m_Boundary;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        m_Boundary = GetSingleton<Boundary>();
    }

    protected override void OnUpdate()
    {
        var boundary = m_Boundary;
        Entities.ForEach((ref Translation translation) =>
        {
            float3 pos = translation.Value;

            while (pos.x < boundary.Min.x)
                pos.x += boundary.Width;
            while (pos.x > boundary.Max.x)
                pos.x -= boundary.Width;
            while (pos.y < boundary.Min.y)
                pos.y += boundary.Height;
            while (pos.y > boundary.Max.y)
                pos.y -= boundary.Height;

            translation.Value = pos;
        })
        .Schedule();
    }
}
