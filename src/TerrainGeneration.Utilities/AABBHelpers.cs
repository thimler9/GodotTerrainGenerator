using Godot;
using Godot.Collections;

namespace TerrainGeneration.Utilities;
public static class AABBHelpers
{

    /// <summary>
    /// Determines if an AABB is in the frustum planes for a camera. NOTE I used AI for this so could be wrong.
    /// </summary>
    /// <param name="aabb"></param>
    /// <param name="planes"></param>
    /// <returns></returns>
    public static bool IsWithinFrustumPlanes(this Aabb aabb, Array<Plane> planes)
    {
        Vector3 center = aabb.Position + aabb.Size * 0.5f;
        Vector3 extents = aabb.Size * 0.5f;

        foreach (Plane plane in planes)
        {
            Vector3 n = plane.Normal.Abs();

            float radius =
                extents.X * n.X +
                extents.Y * n.Y +
                extents.Z * n.Z;

            float distance = plane.DistanceTo(center);

            // IMPORTANT: Godot frustum planes face OUTWARD
            if (distance > radius)
            {
                return false;
            }
        }

        return true;
    }
}
