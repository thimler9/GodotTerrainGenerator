using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace TerrainGeneration.Utilities.Math.Noise;

/// <summary>
/// SIMD port of <see cref="Simplex"/>. Each lane is a different sample point, stored
/// structure-of-arrays style (one Vector&lt;float&gt; per component). The math is a
/// line-for-line translation of the scalar version so results match it closely.
/// Requires .NET 5+ (Vector.Floor).
/// </summary>
public static class SimplexSimd
{
    private const uint SeedModulo = 2347;

    // Gradient constants (same values the scalar version derives from n_, D)
    private const float N = 0.142857142857f;
    private const float NsX = N * 2.0f - 0.0f;
    private const float NsY = N * 0.5f - 1.0f;
    private const float NsZ = N * 1.0f - 0.0f;

    // --------------------------------------------------
    // Helpers
    // --------------------------------------------------

    // GLSL step(edge, x): x < edge ? 0 : 1
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<float> Step(Vector<float> edge, Vector<float> x) =>
        Vector.ConditionalSelect(Vector.LessThan(x, edge), Vector<float>.Zero, Vector<float>.One);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<float> Mod289(Vector<float> x) =>
        x - Vector.Floor(x * (1.0f / 289.0f)) * 289.0f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<float> Permute(Vector<float> v) =>
        Mod289((v * 34.0f + Vector<float>.One) * v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<float> Hash(
        Vector<float> ix, Vector<float> iy, Vector<float> iz,
        Vector<float> ox, Vector<float> oy, Vector<float> oz) =>
        Permute(Permute(Permute(iz + oz) + iy + oy) + ix + ox);

    /// <summary>
    /// Gradient + falloff contribution of one simplex corner.
    /// perm = permuted hash for the corner, (dx,dy,dz) = offset from that corner.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<float> Corner(Vector<float> perm, Vector<float> dx, Vector<float> dy, Vector<float> dz)
    {
        Vector<float> one = Vector<float>.One;

        Vector<float> j = perm - 49.0f * Vector.Floor(perm * (NsZ * NsZ));
        Vector<float> xf = Vector.Floor(j * NsZ);
        Vector<float> yf = Vector.Floor(j - 7.0f * xf);

        Vector<float> x = xf * NsX + new Vector<float>(NsY);
        Vector<float> y = yf * NsX + new Vector<float>(NsY);

        Vector<float> h = one - Vector.Abs(x) - Vector.Abs(y);
        Vector<float> sh = -Step(h, Vector<float>.Zero);

        Vector<float> px = x + (Vector.Floor(x) * 2.0f + one) * sh;
        Vector<float> py = y + (Vector.Floor(y) * 2.0f + one) * sh;
        Vector<float> pz = h;

        // Normalize gradient (Taylor inverse sqrt)
        Vector<float> r = px * px + py * py + pz * pz;
        Vector<float> norm = new Vector<float>(1.79284291400159f) - r * 0.85373472095314f;
        px *= norm;
        py *= norm;
        pz *= norm;

        // Falloff: max(0.6 - |d|^2, 0)^4
        Vector<float> m = Vector.Max(new Vector<float>(0.6f) - (dx * dx + dy * dy + dz * dz), Vector<float>.Zero);
        m *= m;
        m *= m;

        return m * (px * dx + py * dy + pz * dz);
    }

    // --------------------------------------------------
    // 3D Simplex noise (one sample per lane)
    // --------------------------------------------------

    public static Vector<float> Snoise(Vector<float> vx, Vector<float> vy, Vector<float> vz)
    {
        const float cXs = 1.0f / 6.0f;
        const float cYs = 1.0f / 3.0f;
        Vector<float> cX = new(cXs);
        Vector<float> cY = new(cYs);
        Vector<float> half = new(0.5f);
        Vector<float> one = Vector<float>.One;

        // First corner (skew / unskew)
        Vector<float> skew = (vx * cY + vy * cY) + vz * cY;
        Vector<float> ix = Vector.Floor(vx + skew);
        Vector<float> iy = Vector.Floor(vy + skew);
        Vector<float> iz = Vector.Floor(vz + skew);

        Vector<float> unskew = (ix * cX + iy * cX) + iz * cX;
        Vector<float> x0x = vx - ix + unskew;
        Vector<float> x0y = vy - iy + unskew;
        Vector<float> x0z = vz - iz + unskew;

        // Other corners: g = step(x0.yzx, x0)
        Vector<float> gx = Step(x0y, x0x);
        Vector<float> gy = Step(x0z, x0y);
        Vector<float> gz = Step(x0x, x0z);

        Vector<float> lx = one - gx;
        Vector<float> ly = one - gy;
        Vector<float> lz = one - gz;

        // l.zxy
        Vector<float> i1x = Vector.Min(gx, lz);
        Vector<float> i1y = Vector.Min(gy, lx);
        Vector<float> i1z = Vector.Min(gz, ly);

        Vector<float> i2x = Vector.Max(gx, lz);
        Vector<float> i2y = Vector.Max(gy, lx);
        Vector<float> i2z = Vector.Max(gz, ly);

        Vector<float> x1x = x0x - i1x + cX;
        Vector<float> x1y = x0y - i1y + cX;
        Vector<float> x1z = x0z - i1z + cX;

        Vector<float> x2x = x0x - i2x + cY;
        Vector<float> x2y = x0y - i2y + cY;
        Vector<float> x2z = x0z - i2z + cY;

        Vector<float> x3x = x0x - half;
        Vector<float> x3y = x0y - half;
        Vector<float> x3z = x0z - half;

        // Permutations for the four corners (offsets 0, i1, i2, 1)
        ix = Mod289(ix);
        iy = Mod289(iy);
        iz = Mod289(iz);

        Vector<float> zero = Vector<float>.Zero;
        Vector<float> p0 = Hash(ix, iy, iz, zero, zero, zero);
        Vector<float> p1 = Hash(ix, iy, iz, i1x, i1y, i1z);
        Vector<float> p2 = Hash(ix, iy, iz, i2x, i2y, i2z);
        Vector<float> p3 = Hash(ix, iy, iz, one, one, one);

        // Gradients + falloff per corner, then sum
        Vector<float> n0 = Corner(p0, x0x, x0y, x0z);
        Vector<float> n1 = Corner(p1, x1x, x1y, x1z);
        Vector<float> n2 = Corner(p2, x2x, x2y, x2z);
        Vector<float> n3 = Corner(p3, x3x, x3y, x3z);

        return 42.0f * (n0 + n1 + n2 + n3);
    }

    // --------------------------------------------------
    // Multi-octave Simplex noise
    // --------------------------------------------------

    /// <summary>
    /// idX/idY/idZ are the integer voxel coordinates for each lane.
    /// chunkOffset is passed as three floats to avoid clashing with Godot's Vector3.
    /// </summary>
    public static Vector<float> SimplexNoise(
        Simplex.SimplexNoiseParams parameters,
        Vector<uint> idX, Vector<uint> idY, Vector<uint> idZ,
        float chunkOffsetX, float chunkOffsetY, float chunkOffsetZ,
        uint lod)
    {
        // id * lod does not change between octaves, so do it once
        Vector<uint> lodV = new(lod);
        Vector<float> px = Vector.ConvertToSingle(idX * lodV);
        Vector<float> py = Vector.ConvertToSingle(idY * lodV);
        Vector<float> pz = Vector.ConvertToSingle(idZ * lodV);

        Vector<float> scale = new(parameters.Scale);

        Vector<float> noiseHeight = Vector<float>.Zero;

        float frequency = parameters.Frequency;
        float amplitude = parameters.Amplitude;
        uint seed = parameters.Seed;

        for (uint o = 0; o < parameters.NumOctaves; o++)
        {
            // Uniform across lanes, so computed once per octave in scalar code
            float seedValue = (float)(Simplex.Wang(seed) % SeedModulo);
            float ox = (chunkOffsetX + 0.317f) + seedValue;
            float oy = (chunkOffsetY + 0.571f) + seedValue;
            float oz = (chunkOffsetZ + 0.829f) + seedValue;

            Vector<float> freq = new(frequency);

            Vector<float> sx = (new Vector<float>(ox) + px) / scale * freq;
            Vector<float> sy = (new Vector<float>(oy) + py) / scale * freq;
            Vector<float> sz = (new Vector<float>(oz) + pz) / scale * freq;

            noiseHeight += Snoise(sx, sy, sz) * amplitude;

            amplitude *= parameters.Gain;
            frequency *= parameters.Lacunarity;
            seed++;
        }

        return noiseHeight * parameters.Strength;
    }
}
