using Godot;
using System;
using System.Collections.Generic;

namespace GodotTerrainGenerator2.TerrainSpawns;

public readonly struct PoissonDiscPoint2D
{
    public readonly Vector2 Position;
    public readonly float Radius;
    public readonly uint Level;

    public PoissonDiscPoint2D(Vector2 position, float radius, uint level)
    {
        Position = position;
        Radius = radius;
        Level = level;
    }
}

public sealed class PoissonDiscQuadtreeNode : IDisposable
{
    private readonly RenderingDevice _rd;

    public Rect2 Bounds { get; }
    public int Level { get; }
    public IReadOnlyList<PoissonDiscPoint2D> Points => _points;
    public PoissonDiscQuadtreeNode[] Children { get; private set; } = [];
    public Rid GpuPointBuffer { get; private set; }

    private readonly List<PoissonDiscPoint2D> _points = [];

    internal PoissonDiscQuadtreeNode(RenderingDevice rd, Rect2 bounds, int level)
    {
        _rd = rd;
        Bounds = bounds;
        Level = level;
    }

    internal void SetPoints(List<PoissonDiscPoint2D> points)
    {
        _points.Clear();
        _points.AddRange(points);
        UploadPointBuffer();
    }

    internal void SetChildren(PoissonDiscQuadtreeNode[] children)
    {
        Children = children;
    }

    public PoissonDiscQuadtreeNode GetLeafForBounds(Rect2 queryBounds, int targetLevel)
    {
        if (Level >= targetLevel || Children.Length == 0)
        {
            return this;
        }

        foreach (PoissonDiscQuadtreeNode child in Children)
        {
            if (child.Bounds.Encloses(queryBounds))
            {
                return child.GetLeafForBounds(queryBounds, targetLevel);
            }
        }

        return this;
    }

    public void Dispose()
    {
        if (GpuPointBuffer.IsValid)
        {
            _rd.FreeRid(GpuPointBuffer);
            GpuPointBuffer = default;
        }

        foreach (PoissonDiscQuadtreeNode child in Children)
        {
            child.Dispose();
        }
    }

    private void UploadPointBuffer()
    {
        if (GpuPointBuffer.IsValid)
        {
            _rd.FreeRid(GpuPointBuffer);
            GpuPointBuffer = default;
        }

        int pointCount = Math.Max(1, _points.Count);
        float[] packed = new float[pointCount * 4];
        for (int i = 0; i < _points.Count; i++)
        {
            PoissonDiscPoint2D point = _points[i];
            int offset = i * 4;
            packed[offset] = point.Position.X;
            packed[offset + 1] = point.Position.Y;
            packed[offset + 2] = point.Radius;
            packed[offset + 3] = point.Level;
        }

        byte[] bytes = new byte[packed.Length * sizeof(float)];
        Buffer.BlockCopy(packed, 0, bytes, 0, bytes.Length);
        GpuPointBuffer = _rd.StorageBufferCreate((uint)bytes.Length, bytes);
    }
}

public sealed class PoissonDiscQuadtree2D : IDisposable
{
    private const int CandidatesPerPoint = 24;

    private readonly RenderingDevice _rd;
    private readonly uint _seed;
    private readonly int _maxLevel;
    private readonly int _maxPointsPerNode;
    private readonly float _baseRadius;
    private readonly float _radiusFalloff;

    public PoissonDiscQuadtreeNode Root { get; }

    public PoissonDiscQuadtree2D(
        RenderingDevice rd,
        float worldSize,
        uint seed,
        int maxLevel,
        float baseRadius,
        float radiusFalloff = 0.5f,
        int maxPointsPerNode = 4096)
    {
        _rd = rd;
        _seed = seed;
        _maxLevel = Math.Max(0, maxLevel);
        _baseRadius = Math.Max(0.001f, baseRadius);
        _radiusFalloff = Mathf.Clamp(radiusFalloff, 0.05f, 0.95f);
        _maxPointsPerNode = Math.Max(1, maxPointsPerNode);

        Root = BuildNode(new Rect2(Vector2.Zero, new Vector2(worldSize, worldSize)), 0, []);
    }

    public PoissonDiscQuadtreeNode GetNodeForChunk(Vector2 chunkOffset, float chunkSize, int lodLevel)
    {
        return Root.GetLeafForBounds(new Rect2(chunkOffset, new Vector2(chunkSize, chunkSize)), lodLevel);
    }

    public void Dispose()
    {
        Root.Dispose();
    }

    private PoissonDiscQuadtreeNode BuildNode(Rect2 bounds, int level, IReadOnlyList<PoissonDiscPoint2D> inheritedPoints)
    {
        PoissonDiscQuadtreeNode node = new PoissonDiscQuadtreeNode(_rd, bounds, level);
        float radius = _baseRadius * Mathf.Pow(_radiusFalloff, level);
        List<PoissonDiscPoint2D> points = GeneratePoints(bounds, level, radius, inheritedPoints);
        node.SetPoints(points);

        if (level < _maxLevel)
        {
            Vector2 halfSize = bounds.Size * 0.5f;
            PoissonDiscQuadtreeNode[] children = new PoissonDiscQuadtreeNode[4];
            for (int childIndex = 0; childIndex < 4; childIndex++)
            {
                Vector2 childOffset = bounds.Position + new Vector2(childIndex & 1, childIndex >> 1) * halfSize;
                Rect2 childBounds = new Rect2(childOffset, halfSize);
                List<PoissonDiscPoint2D> childInherited = points.FindAll(point => childBounds.HasPoint(point.Position));
                children[childIndex] = BuildNode(childBounds, level + 1, childInherited);
            }

            node.SetChildren(children);
        }

        return node;
    }

    private List<PoissonDiscPoint2D> GeneratePoints(Rect2 bounds, int level, float radius, IReadOnlyList<PoissonDiscPoint2D> inheritedPoints)
    {
        List<PoissonDiscPoint2D> points = new List<PoissonDiscPoint2D>(_maxPointsPerNode);
        points.AddRange(inheritedPoints);

        float cellSize = radius / Mathf.Sqrt(2.0f);
        int gridWidth = Math.Max(1, Mathf.CeilToInt(bounds.Size.X / cellSize));
        int gridHeight = Math.Max(1, Mathf.CeilToInt(bounds.Size.Y / cellSize));
        int[] grid = new int[gridWidth * gridHeight];
        Array.Fill(grid, -1);

        for (int i = 0; i < points.Count; i++)
        {
            InsertGridPoint(points[i].Position, i, bounds, cellSize, gridWidth, gridHeight, grid);
        }

        List<int> active = [];
        if (points.Count == 0)
        {
            Vector2 first = new Vector2(
                Mathf.Lerp(bounds.Position.X, bounds.End.X, Hash01(_seed, level, 1)),
                Mathf.Lerp(bounds.Position.Y, bounds.End.Y, Hash01(_seed, level, 2)));
            points.Add(new PoissonDiscPoint2D(first, radius, (uint)level));
            InsertGridPoint(first, 0, bounds, cellSize, gridWidth, gridHeight, grid);
            active.Add(0);
        }
        else
        {
            for (int i = 0; i < points.Count; i++)
            {
                active.Add(i);
            }
        }

        int cursor = 0;
        while (active.Count > 0 && points.Count < _maxPointsPerNode)
        {
            int activeIndex = (cursor + (int)(Hash01(_seed, level, active.Count + cursor) * active.Count)) % active.Count;
            PoissonDiscPoint2D source = points[active[activeIndex]];
            bool accepted = false;

            for (int candidate = 0; candidate < CandidatesPerPoint; candidate++)
            {
                uint hashSalt = (uint)(level * 73856093 ^ activeIndex * 19349663 ^ candidate * 83492791);
                float angle = Hash01(_seed, (int)hashSalt, 0) * Mathf.Tau;
                float distance = radius * (1.0f + Hash01(_seed, (int)hashSalt, 1));
                Vector2 candidatePoint = source.Position + Vector2.Right.Rotated(angle) * distance;

                if (!bounds.HasPoint(candidatePoint) || HasNearbyPoint(candidatePoint, radius, bounds, cellSize, gridWidth, gridHeight, grid, points))
                {
                    continue;
                }

                int pointIndex = points.Count;
                points.Add(new PoissonDiscPoint2D(candidatePoint, radius, (uint)level));
                active.Add(pointIndex);
                InsertGridPoint(candidatePoint, pointIndex, bounds, cellSize, gridWidth, gridHeight, grid);
                accepted = true;
                break;
            }

            if (!accepted)
            {
                active.RemoveAt(activeIndex);
            }

            cursor++;
        }

        return points;
    }

    private static bool HasNearbyPoint(Vector2 position, float radius, Rect2 bounds, float cellSize, int gridWidth, int gridHeight, int[] grid, List<PoissonDiscPoint2D> points)
    {
        Vector2I cell = ToGridCell(position, bounds, cellSize, gridWidth, gridHeight);
        int searchRadius = 2;
        float radiusSquared = radius * radius;

        for (int y = Math.Max(0, cell.Y - searchRadius); y <= Math.Min(gridHeight - 1, cell.Y + searchRadius); y++)
        {
            for (int x = Math.Max(0, cell.X - searchRadius); x <= Math.Min(gridWidth - 1, cell.X + searchRadius); x++)
            {
                int pointIndex = grid[x + y * gridWidth];
                if (pointIndex < 0)
                {
                    continue;
                }

                if (position.DistanceSquaredTo(points[pointIndex].Position) < radiusSquared)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void InsertGridPoint(Vector2 position, int pointIndex, Rect2 bounds, float cellSize, int gridWidth, int gridHeight, int[] grid)
    {
        Vector2I cell = ToGridCell(position, bounds, cellSize, gridWidth, gridHeight);
        grid[cell.X + cell.Y * gridWidth] = pointIndex;
    }

    private static Vector2I ToGridCell(Vector2 position, Rect2 bounds, float cellSize, int gridWidth, int gridHeight)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((position.X - bounds.Position.X) / cellSize), 0, gridWidth - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((position.Y - bounds.Position.Y) / cellSize), 0, gridHeight - 1);
        return new Vector2I(x, y);
    }

    private static float Hash01(uint seed, int a, int b)
    {
        uint value = seed ^ 0x9E3779B9u;
        value ^= (uint)a * 0x85EBCA6Bu;
        value = (value << 13) | (value >> 19);
        value ^= (uint)b * 0xC2B2AE35u;
        value ^= value >> 16;
        value *= 0x7FEB352Du;
        value ^= value >> 15;
        value *= 0x846CA68Bu;
        value ^= value >> 16;
        return (value & 0x00FFFFFFu) / 16777216.0f;
    }
}
