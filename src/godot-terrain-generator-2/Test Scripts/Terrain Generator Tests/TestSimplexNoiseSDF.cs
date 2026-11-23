using Godot;
using System;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;
using TerrainGeneration.Application.SDFGenerator.CPU.SimplexNoise;
using TerrainGeneration.Application.SDFGenerator.GPU.SimplexNoise;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;
using TerrainGeneration.Application.SDFGenerator.Pipeline.GPU;
using TerrainGeneration.Utilities.EngineAbstractions;
using TerrainGeneration.Utilities.Math.Extensions;
using TerrainGeneration.Utilities.Struct;

public partial class TestSimplexNoiseSDF : Node
{
    /// <summary>
    /// Maximum number of mismatches printed per test (all values are still compared).
    /// </summary>
    [Export]
    public uint MaxMismatchesToPrint = 20;

    /// <summary>
    /// Allowed difference between CPU and GPU. Scaled by magnitude for values larger than 1.
    /// Noise is a long chain of floating point math (and gets amplified per octave), so this is looser than for sphere/constant.
    /// </summary>
    [Export]
    public float Tolerance = 1e-3f;

    public override void _Ready()
    {
        (string Name, Func<bool> Test)[] tests =
        {
            (nameof(SimplexNoiseSDFBasicTest), SimplexNoiseSDFBasicTest),
            (nameof(SimplexNoiseLod1Test), SimplexNoiseLod1Test),
            (nameof(SimplexNoiseLod4Test), SimplexNoiseLod4Test),
            (nameof(SimplexNoiseChunkSizeSmallTest), SimplexNoiseChunkSizeSmallTest),
            (nameof(SimplexNoiseChunkSizeLargeTest), SimplexNoiseChunkSizeLargeTest),
            (nameof(SimplexNoiseSeedTest), SimplexNoiseSeedTest),
            (nameof(SimplexNoiseScaleTest), SimplexNoiseScaleTest),
            (nameof(SimplexNoiseStrengthTest), SimplexNoiseStrengthTest),
            (nameof(SimplexNoiseStrengthZeroTest), SimplexNoiseStrengthZeroTest),
            (nameof(SimplexNoiseSingleOctaveTest), SimplexNoiseSingleOctaveTest),
            (nameof(SimplexNoiseManyOctavesTest), SimplexNoiseManyOctavesTest),
            (nameof(SimplexNoiseFrequencyAmplitudeTest), SimplexNoiseFrequencyAmplitudeTest),
            (nameof(SimplexNoiseLacunarityGainTest), SimplexNoiseLacunarityGainTest),
            (nameof(SimplexNoiseChunkOffsetTest), SimplexNoiseChunkOffsetTest),
            (nameof(SimplexNoiseNegativeChunkOffsetTest), SimplexNoiseNegativeChunkOffsetTest),
            (nameof(SimplexNoiseBiomeProbabilityTest), SimplexNoiseBiomeProbabilityTest),
        };

        int passed = 0;
        foreach (var (name, test) in tests)
        {
            bool ok = test();
            if (ok) passed++;
            GD.Print($"[{(ok ? "PASS" : "FAIL")}] {name}");
        }

        GD.Print($"Simplex noise SDF parity: {passed}/{tests.Length} tests passed.");
    }

    // ---------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------

    /// <summary>
    /// Tests the sdf with chunkSize = 128, lod = 2 and default noise settings
    /// </summary>
    private bool SimplexNoiseSDFBasicTest()
    {
        return RunParityTest(nameof(SimplexNoiseSDFBasicTest));
    }

    /// <summary>
    /// Tests the sdf with LOD 1
    /// </summary>
    private bool SimplexNoiseLod1Test()
    {
        return RunParityTest(nameof(SimplexNoiseLod1Test), lod: 1);
    }

    /// <summary>
    /// Tests the sdf with LOD 4
    /// </summary>
    private bool SimplexNoiseLod4Test()
    {
        return RunParityTest(nameof(SimplexNoiseLod4Test), lod: 4);
    }

    /// <summary>
    /// Tests the sdf with chunkSize = 8, lod = 1
    /// </summary>
    private bool SimplexNoiseChunkSizeSmallTest()
    {
        return RunParityTest(nameof(SimplexNoiseChunkSizeSmallTest), chunkSize: 8, lod: 1);
    }

    /// <summary>
    /// Tests the sdf with chunkSize = 2048, lod = 64
    /// </summary>
    private bool SimplexNoiseChunkSizeLargeTest()
    {
        return RunParityTest(nameof(SimplexNoiseChunkSizeLargeTest), chunkSize: 2048, lod: 64);
    }

    /// <summary>
    /// Tests the sdf with a different seed
    /// </summary>
    private bool SimplexNoiseSeedTest()
    {
        return RunParityTest(nameof(SimplexNoiseSeedTest), seed: 987654u);
    }

    /// <summary>
    /// Tests the sdf with a small scale (high detail)
    /// </summary>
    private bool SimplexNoiseScaleTest()
    {
        return RunParityTest(nameof(SimplexNoiseScaleTest), scale: 7.3f);
    }

    /// <summary>
    /// Tests the sdf with a different strength
    /// </summary>
    private bool SimplexNoiseStrengthTest()
    {
        return RunParityTest(nameof(SimplexNoiseStrengthTest), strength: 12.5f);
    }

    /// <summary>
    /// Tests the sdf with strength = 0 (output should be unchanged on both sides)
    /// </summary>
    private bool SimplexNoiseStrengthZeroTest()
    {
        return RunParityTest(nameof(SimplexNoiseStrengthZeroTest), strength: 0f, expectChange: false);
    }

    /// <summary>
    /// Tests the sdf with a single octave
    /// </summary>
    private bool SimplexNoiseSingleOctaveTest()
    {
        return RunParityTest(nameof(SimplexNoiseSingleOctaveTest), numOctaves: 1u);
    }

    /// <summary>
    /// Tests the sdf with many octaves
    /// </summary>
    private bool SimplexNoiseManyOctavesTest()
    {
        return RunParityTest(nameof(SimplexNoiseManyOctavesTest), numOctaves: 8u);
    }

    /// <summary>
    /// Tests the sdf with a non-default frequency and amplitude
    /// </summary>
    private bool SimplexNoiseFrequencyAmplitudeTest()
    {
        return RunParityTest(nameof(SimplexNoiseFrequencyAmplitudeTest), frequency: 2.5f, amplitude: 3f);
    }

    /// <summary>
    /// Tests the sdf with a non-default lacunarity and gain
    /// </summary>
    private bool SimplexNoiseLacunarityGainTest()
    {
        return RunParityTest(nameof(SimplexNoiseLacunarityGainTest), lacunarity: 3.1f, gain: 0.35f);
    }

    /// <summary>
    /// Tests the sdf with a positive, non-zero chunk offset
    /// </summary>
    private bool SimplexNoiseChunkOffsetTest()
    {
        return RunParityTest(nameof(SimplexNoiseChunkOffsetTest), chunkOffset: new Vector3(128, 256, 384));
    }

    /// <summary>
    /// Tests the sdf with a negative chunk offset (negative world coordinates stress floor/hash handling)
    /// </summary>
    private bool SimplexNoiseNegativeChunkOffsetTest()
    {
        return RunParityTest(nameof(SimplexNoiseNegativeChunkOffsetTest), chunkOffset: new Vector3(-128, -64, -256));
    }

    /// <summary>
    /// Tests the sdf with biomes enabled so the probability is between 0 and 1 for part of the chunk
    /// </summary>
    private bool SimplexNoiseBiomeProbabilityTest()
    {
        return RunParityTest(
            nameof(SimplexNoiseBiomeProbabilityTest),
            ignoreBiome: false,
            biomeDepth: 64f,
            biomeDepthSpread: 32f);
    }

    // ---------------------------------------------------------------------
    // Shared test runner
    // ---------------------------------------------------------------------

    /// <summary>
    /// Runs the simplex noise SDF on the CPU and GPU with the given parameters and compares every output value.
    /// Both outputs start from the same non-zero data because the noise is accumulated onto the existing values.
    /// </summary>
    private bool RunParityTest(
        string testName,
        uint chunkSize = 128,
        uint lod = 2,
        uint seed = 1337u,
        float scale = 32f,
        float strength = 1f,
        uint numOctaves = 4u,
        float frequency = 1f,
        float amplitude = 1f,
        float lacunarity = 2f,
        float gain = 0.5f,
        Vector3? chunkOffset = null,
        bool ignoreBiome = true,
        float biomeDepth = 0f,
        float biomeDepthSpread = 0f,
        bool expectChange = true)
    {
        Vector3 offset = chunkOffset ?? Vector3.Zero;

        uint adjustedSize = chunkSize / lod + 2;
        uint bufferSize = adjustedSize * adjustedSize * adjustedSize;

        // Shared, non-zero starting data (values between 1.0 and 4.0)
        float[] initial = new float[bufferSize];
        for (uint i = 0; i < bufferSize; i++)
        {
            initial[i] = 1.0f + (i % 7) * 0.5f;
        }

        // ======== CPU =========
        var sdf = new TerrainGeneration.Application.SDFGenerator.CPU.SimplexNoise.SDF();
        var cpuParameters = new SimplexNoiseParameters(seed, scale, strength, numOctaves, frequency, amplitude, lacunarity, gain);
        var sdfParametersCPU = new SDFParametersCPU(offset, chunkSize, lod);
        var biomeParametersCPU = new BiomeParametersCPU(0, 0, biomeDepth, biomeDepthSpread, ignoreBiome);
        float[] outputCPU = (float[])initial.Clone();
        ComputeCPUSDF(sdf, sdfParametersCPU, cpuParameters, biomeParametersCPU, outputCPU);

        // ======== GPU =========
        RenderingDevice rd = RenderingServer.GetRenderingDevice();
        var shader = new SimplexNoiseShader(rd, "res://Shaders/Compute/terrain_simplex_noise.glsl");

        byte[] initialBytes = new byte[initial.Length * sizeof(float)];
        Buffer.BlockCopy(initial, 0, initialBytes, 0, initialBytes.Length);
        ComputeBuffer outputBuffer = new ComputeBuffer(
            rd, sizeof(float) * bufferSize, RenderingDevice.UniformType.StorageBuffer, 0, initialBytes);

        var parametersGPU = new SimplexNoiseShaderParameters(seed, scale, strength, numOctaves, frequency, amplitude, lacunarity, gain);
        var sdfParametersGPU = new SDFParametersGPU(offset, chunkSize, lod);
        var biomeParametersGPU = new BiomeParametersGPU(0, 0, biomeDepth, biomeDepthSpread, ignoreBiome);
        ComputeGPUSDF(rd, chunkSize, lod, shader, sdfParametersGPU, parametersGPU, biomeParametersGPU, outputBuffer);

        float[] outputGPU = outputBuffer.GetData<float>();

        // ======== COMPARE =========
        bool passed = true;

        // Guard against a vacuous pass (e.g. the CPU function never writing the noise)
        if (expectChange && outputCPU.AsSpan().SequenceEqual(initial))
        {
            GD.PrintErr($"[{testName}] CPU output is identical to the initial data, so the noise was never applied.");
            passed = false;
        }

        if (outputGPU.Length < outputCPU.Length)
        {
            GD.PrintErr($"[{testName}] GPU output is smaller than CPU output ({outputGPU.Length} < {outputCPU.Length}).");
            passed = false;
        }
        else
        {
            uint mismatches = 0;
            for (int i = 0; i < outputCPU.Length; i++)
            {
                if (outputCPU[i].FuzzyEquals(outputGPU[i], Tolerance))
                {
                    continue;
                }

                passed = false;
                mismatches++;
                if (mismatches <= MaxMismatchesToPrint)
                {
                    GD.PrintErr($"[{testName}] Mismatch at index {i}: CPU = {outputCPU[i]}, GPU = {outputGPU[i]}");
                }
            }

            if (mismatches > 0)
            {
                GD.PrintErr($"[{testName}] {mismatches} of {outputCPU.Length} values mismatched.");
            }
        }

        outputBuffer.Dispose();
        return passed;
    }

    private void ComputeCPUSDF(ISDFFunctionCPU sdf, SDFParametersCPU sdfParametersCPU, SimplexNoiseParameters parameters, BiomeParametersCPU biomeParameters, float[] output)
    {
        float[] temperatureValues = new float[1];
        sdf.Dispatch(parameters, sdfParametersCPU, biomeParameters, temperatureValues, output);
    }

    private void ComputeGPUSDF(RenderingDevice rd, uint chunkSize, uint lod, ISDFShader shader, SDFParametersGPU sDFParameters, ISDFShaderParameters shaderParameters, BiomeParametersGPU biomeParameters, ComputeBuffer outputBuffer)
    {
        byte[] sdfParametersGPUBytes = StructHelpers.ToByteArray(sDFParameters);
        byte[] biomeParametersGPUBytes = StructHelpers.ToByteArray(biomeParameters);

        ComputeBuffer sdfParametersBuffer = new ComputeBuffer(rd, sizeof(float) * 8, RenderingDevice.UniformType.UniformBuffer, 0, sdfParametersGPUBytes);
        ComputeBuffer biomeParametersBuffer = new ComputeBuffer(rd, sizeof(float) * 8, RenderingDevice.UniformType.UniformBuffer, 0, biomeParametersGPUBytes);
        ComputeBuffer temperatureBuffer = new ComputeBuffer(rd, sizeof(float), RenderingDevice.UniformType.StorageBuffer, 0);

        shader.Dispatch(chunkSize, lod, shaderParameters, sdfParametersBuffer, biomeParametersBuffer, temperatureBuffer, outputBuffer);

        sdfParametersBuffer.Dispose();
        biomeParametersBuffer.Dispose();
        temperatureBuffer.Dispose();
    }
}
