using Godot;

namespace TerrainGeneration.Utilities;
public static class AABBHelpers
{

    public static bool IsWithinFrustumPlanes(this Aabb aabb, Plane[] planes)
    {
        // Cache AABB bounds to avoid repeated property access
        Vector3 min = aabb.Position;
        Vector3 max = aabb.End;

        // Test against all 6 frustum planes
        for (int i = 0; i < 6; i++)
        {
            Plane plane = planes[i];
            Vector3 normal = plane.Normal;

            // Get the positive vertex (furthest point in direction of plane normal)
            // Using ternary with cached values is faster than creating new Vector3
            float px = normal.X >= 0 ? max.X : min.X;
            float py = normal.Y >= 0 ? max.Y : min.Y;
            float pz = normal.Z >= 0 ? max.Z : min.Z;

            // Manual distance calculation is faster than plane.DistanceTo()
            // Distance = dot(normal, point) + d
            if (normal.X * px + normal.Y * py + normal.Z * pz + plane.D < 0)
            {
                return false; // AABB is completely outside the frustum
            }
        }

        return true;
    }
}
