using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Utilities.Math.Noise;

// THIS WAS AI GENERATED - PROBABLY DOGSHIT - NEEDS TO BE REWRITTEN
public static class Simplex
{
    public struct UInt3
    {
        public uint X, Y, Z;

        public UInt3(uint x, uint y, uint z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public struct SimplexNoiseParams
    {
        public uint Seed;
        public float Scale;
        public float Strength;
        public uint NumOctaves;
        public float Frequency;
        public float Amplitude;
        public float Lacunarity;
        public float Gain;
    }

    private const uint SeedModulo = 2347;

    // --------------------------------------------------
    // Vector helpers
    // --------------------------------------------------

    private static Vector3 Floor(Vector3 v) =>
        new(MathF.Floor(v.X), MathF.Floor(v.Y), MathF.Floor(v.Z));

    private static Vector4 Floor(Vector4 v) =>
        new(MathF.Floor(v.X), MathF.Floor(v.Y),
            MathF.Floor(v.Z), MathF.Floor(v.W));

    private static Vector3 Abs(Vector3 v) =>
        new(MathF.Abs(v.X), MathF.Abs(v.Y), MathF.Abs(v.Z));

    private static Vector4 Abs(Vector4 v) =>
        new(MathF.Abs(v.X), MathF.Abs(v.Y),
            MathF.Abs(v.Z), MathF.Abs(v.W));

    private static Vector3 Max(Vector3 a, float b) =>
        Vector3.Max(a, new Vector3(b));

    private static Vector4 Max(Vector4 a, float b) =>
        Vector4.Max(a, new Vector4(b));

    private static Vector3 Min(Vector3 a, Vector3 b) =>
        Vector3.Min(a, b);

    private static Vector3 Max(Vector3 a, Vector3 b) =>
        Vector3.Max(a, b);

    // GLSL step(edge, x)
    private static float Step(float edge, float x) =>
        x < edge ? 0.0f : 1.0f;

    private static Vector3 Step(Vector3 edge, Vector3 x) =>
        new(
            Step(edge.X, x.X),
            Step(edge.Y, x.Y),
            Step(edge.Z, x.Z)
        );

    private static Vector4 Step(Vector4 edge, Vector4 x) =>
        new(
            Step(edge.X, x.X),
            Step(edge.Y, x.Y),
            Step(edge.Z, x.Z),
            Step(edge.W, x.W)
        );

    // --------------------------------------------------
    // Simplex noise helpers
    // --------------------------------------------------

    private static Vector3 Mod289(Vector3 x) =>
        x - Floor(x * (1.0f / 289.0f)) * 289.0f;

    private static Vector4 Mod289(Vector4 x) =>
        x - Floor(x * (1.0f / 289.0f)) * 289.0f;

    private static Vector3 Permute(Vector3 v) =>
        Mod289(((v * 34.0f) + new Vector3(1.0f)) * v);

    private static Vector4 Permute(Vector4 v) =>
        Mod289(((v * 34.0f) + new Vector4(1.0f)) * v);

    private static Vector3 TaylorInvSqrt(Vector3 r) =>
        new Vector3(1.79284291400159f) - r * 0.85373472095314f;

    private static Vector4 TaylorInvSqrt(Vector4 r) =>
        new Vector4(1.79284291400159f) - r * 0.85373472095314f;

    // --------------------------------------------------
    // 3D Simplex noise
    // --------------------------------------------------

    public static float Sno​​ise(Vector3 v)
    {
        Vector2 C = new(1.0f / 6.0f, 1.0f / 3.0f);
        Vector4 D = new(0.0f, 0.5f, 1.0f, 2.0f);

        // First corner
        Vector3 i = Floor(v + new Vector3(Vector3.Dot(v, new Vector3(C.Y))));
        Vector3 x0 = v - i + new Vector3(Vector3.Dot(i, new Vector3(C.X)));

        // Other corners
        Vector3 g = Step(
            new Vector3(x0.Y, x0.Z, x0.X),
            x0
        );

        Vector3 l = new Vector3(1.0f) - g;

        Vector3 i1 = Min(g, new Vector3(l.Z, l.X, l.Y));
        Vector3 i2 = Max(g, new Vector3(l.Z, l.X, l.Y));

        Vector3 x1 = x0 - i1 + new Vector3(C.X);
        Vector3 x2 = x0 - i2 + new Vector3(C.Y);
        Vector3 x3 = x0 - new Vector3(D.Y);

        // Permutations
        i = Mod289(i);

        Vector4 p = Permute(
            Permute(
                Permute(
                    new Vector4(i.Z) + new Vector4(0.0f, i1.Z, i2.Z, 1.0f)
                )
                + new Vector4(i.Y) + new Vector4(0.0f, i1.Y, i2.Y, 1.0f)
            )
            + new Vector4(i.X) + new Vector4(0.0f, i1.X, i2.X, 1.0f)
        );

        // Gradients
        float n_ = 0.142857142857f;

        Vector3 ns = new(
            n_ * D.W - D.X,
            n_ * D.Y - D.Z,
            n_ * D.Z - D.X
        );

        Vector4 j = p - 49.0f * Floor(p * (ns.Z * ns.Z));

        Vector4 x_ = Floor(j * ns.Z);
        Vector4 y_ = Floor(j - 7.0f * x_);

        Vector4 x = x_ * ns.X + new Vector4(ns.Y);
        Vector4 y = y_ * ns.X + new Vector4(ns.Y);

        Vector4 h = new Vector4(1.0f) - Abs(x) - Abs(y);

        Vector4 b0 = new(x.X, x.Y, y.X, y.Y);
        Vector4 b1 = new(x.Z, x.W, y.Z, y.W);

        Vector4 s0 = Floor(b0) * 2.0f + new Vector4(1.0f);
        Vector4 s1 = Floor(b1) * 2.0f + new Vector4(1.0f);

        Vector4 sh = -Step(h, Vector4.Zero);

        Vector4 a0 = new(
            b0.X + s0.X * sh.X,
            b0.Z + s0.Z * sh.X,
            b0.Y + s0.Y * sh.Y,
            b0.W + s0.W * sh.Y
        );

        Vector4 a1 = new(
            b1.X + s1.X * sh.Z,
            b1.Z + s1.Z * sh.Z,
            b1.Y + s1.Y * sh.W,
            b1.W + s1.W * sh.W
        );

        Vector3 p0 = new(a0.X, a0.Y, h.X);
        Vector3 p1 = new(a0.Z, a0.W, h.Y);
        Vector3 p2 = new(a1.X, a1.Y, h.Z);
        Vector3 p3 = new(a1.Z, a1.W, h.W);

        // Normalize gradients
        Vector4 norm = TaylorInvSqrt(
            new Vector4(
                Vector3.Dot(p0, p0),
                Vector3.Dot(p1, p1),
                Vector3.Dot(p2, p2),
                Vector3.Dot(p3, p3)
            )
        );

        p0 *= norm.X;
        p1 *= norm.Y;
        p2 *= norm.Z;
        p3 *= norm.W;

        // Mix final noise value
        Vector4 m = Max(
            new Vector4(
                0.6f - Vector3.Dot(x0, x0),
                0.6f - Vector3.Dot(x1, x1),
                0.6f - Vector3.Dot(x2, x2),
                0.6f - Vector3.Dot(x3, x3)
            ),
            0.0f
        );

        m *= m;

        return 42.0f * Vector4.Dot(
            m * m,
            new Vector4(
                Vector3.Dot(p0, x0),
                Vector3.Dot(p1, x1),
                Vector3.Dot(p2, x2),
                Vector3.Dot(p3, x3)
            )
        );
    }

    // --------------------------------------------------
    // Wang hash
    // --------------------------------------------------

    public static uint Wang(uint a)
    {
        unchecked
        {
            a = (a ^ 61U) ^ (a >> 16);
            a = a * 9U;
            a = a ^ (a >> 4);
            a = a * 0x27d4eb2dU;
            a = a ^ (a >> 15);

            return a;
        }
    }

    // --------------------------------------------------
    // Multi-octave Simplex noise
    // --------------------------------------------------

    public static float SimplexNoise(
        SimplexNoiseParams parameters,
        UInt3 id,
        Vector3 chunkOffset,
        uint lod)
    {
        float noiseHeight = 0.0f;

        float scale = parameters.Scale;
        float frequency = parameters.Frequency;
        float amplitude = parameters.Amplitude;

        uint numOctaves = parameters.NumOctaves;

        float lacunarity = parameters.Lacunarity;
        float gain = parameters.Gain;

        uint seed = parameters.Seed;

        for (uint i = 0; i < numOctaves; i++)
        {
            float seedValue = (float)(Wang(seed) % SeedModulo);

            Vector3 samplePoint = (
                chunkOffset
                + new Vector3(seedValue)
                + new Vector3(
                    (float)((ulong)id.X * lod),
                    (float)((ulong)id.Y * lod),
                    (float)((ulong)id.Z * lod)
                )
            ) / scale * frequency;

            float simplexNoiseValue = Sno​​ise(samplePoint);

            noiseHeight += simplexNoiseValue * amplitude;

            amplitude *= gain;
            frequency *= lacunarity;

            seed++;
        }

        return noiseHeight * parameters.Strength;
    }
}
