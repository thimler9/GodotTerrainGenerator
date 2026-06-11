using Godot;
using System.Collections.Generic;

namespace GodotTerrainGenerator2.TerrainSpawns;

public partial class TerrainSpawnRenderer : Node3D
{
    private readonly List<MultiMeshInstance3D> _instances = [];

    public void RenderSelections(IReadOnlyList<TerrainSpawnDefinition> definitions, IReadOnlyList<TerrainSpawnSelection> selections)
    {
        Clear();

        List<Transform3D>[] transformsByType = new List<Transform3D>[definitions.Count];
        for (int i = 0; i < transformsByType.Length; i++)
        {
            transformsByType[i] = [];
        }

        foreach (TerrainSpawnSelection selection in selections)
        {
            int typeIndex = Mathf.RoundToInt(selection.NormalAndType.W);
            if (typeIndex < 0 || typeIndex >= definitions.Count)
            {
                continue;
            }

            Vector3 position = new Vector3(selection.PositionAndScale.X, selection.PositionAndScale.Y, selection.PositionAndScale.Z);
            Vector3 normal = new Vector3(selection.NormalAndType.X, selection.NormalAndType.Y, selection.NormalAndType.Z).Normalized();
            float scale = selection.PositionAndScale.W;
            Basis basis = Basis.FromScale(Vector3.One * scale);
            Quaternion align = GetAlignment(Vector3.Up, normal == Vector3.Zero ? Vector3.Up : normal);
            basis = new Basis(align) * basis;
            transformsByType[typeIndex].Add(new Transform3D(basis, position));
        }

        for (int typeIndex = 0; typeIndex < definitions.Count; typeIndex++)
        {
            TerrainSpawnDefinition definition = definitions[typeIndex];
            List<Transform3D> transforms = transformsByType[typeIndex];
            if (definition.Mesh == null || transforms.Count == 0)
            {
                continue;
            }

            MultiMesh multiMesh = new MultiMesh
            {
                Mesh = definition.Mesh,
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                InstanceCount = transforms.Count,
            };

            for (int i = 0; i < transforms.Count; i++)
            {
                multiMesh.SetInstanceTransform(i, transforms[i]);
            }

            MultiMeshInstance3D instance = new MultiMeshInstance3D
            {
                Name = $"{definition.SpawnId}_instances",
                Multimesh = multiMesh,
                MaterialOverride = definition.MaterialOverride,
            };
            AddChild(instance);
            _instances.Add(instance);
        }
    }

    public void Clear()
    {
        foreach (MultiMeshInstance3D instance in _instances)
        {
            if (IsInstanceValid(instance))
            {
                instance.QueueFree();
            }
        }

        _instances.Clear();
    }

    private static Quaternion GetAlignment(Vector3 from, Vector3 to)
    {
        Vector3 fromNormal = from.Normalized();
        Vector3 toNormal = to.Normalized();
        float dot = Mathf.Clamp(fromNormal.Dot(toNormal), -1.0f, 1.0f);

        if (dot > 0.9999f)
        {
            return Quaternion.Identity;
        }

        if (dot < -0.9999f)
        {
            return new Quaternion(Vector3.Right, Mathf.Pi);
        }

        Vector3 axis = fromNormal.Cross(toNormal).Normalized();
        return new Quaternion(axis, Mathf.Acos(dot));
    }
}
