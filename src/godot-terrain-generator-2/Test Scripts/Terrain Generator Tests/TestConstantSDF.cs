using Godot;
using System;
using System.Diagnostics;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;
using TerrainGeneration.Application.SDFGenerator.CPU.Constant;
using TerrainGeneration.Application.SDFGenerator.GPU.Constant;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;
using TerrainGeneration.Application.SDFGenerator.Pipeline.GPU;
using TerrainGeneration.Utilities.EngineAbstractions;
using TerrainGeneration.Utilities.Math.Extensions;
using TerrainGeneration.Utilities.Struct;

using CpuOperationType = TerrainGeneration.Application.SDFGenerator.CPU.OperationType;
using GpuOperationType = TerrainGeneration.Application.SDFGenerator.GPU.OperationType;

public partial class TestConstantSDF : Node
{
    /// <summary>
    /// Maximum number of mismatches printed per test (all values are still compared).
    /// </summary>
    [Export]
    public uint MaxMismatchesToPrint = 20;

    /// <summary>
    /// Allowed difference between CPU and GPU. Scaled by magnitude for values larger than 1.
    /// </summary>
    [Export]
    public float Tolerance = 1e-4f;

    public override void _Ready()
    {
        (string Name, Func<bool> Test)[] tests =
        {
            (nameof(ConstantSDFBasicTest), ConstantSDFBasicTest),
            (nameof(ConstantLod1Test), ConstantLod1Test),
            (nameof(ConstantLod4Test), ConstantLod4Test),
            (nameof(ConstantChunkSizeSmallTest), ConstantChunkSizeSmallTest),
            (nameof(ConstantChunkSizeLargeTest), ConstantChunkSizeLargeTest),
            (nameof(ConstantValueZeroTest), ConstantValueZeroTest),
            (nameof(ConstantNegativeValueTest), ConstantNegativeValueTest),
            (nameof(ConstantLargeValueTest), ConstantLargeValueTest),
            (nameof(ConstantAddOperationTest), ConstantAddOperationTest),
            (nameof(ConstantSubtractOperationTest), ConstantSubtractOperationTest),
            (nameof(ConstantMultiplyOperationTest), ConstantMultiplyOperationTest),
            (nameof(ConstantDivisionOperationTest), ConstantDivisionOperationTest),
            (nameof(ConstantDivisionByZeroTest), ConstantDivisionByZeroTest),
            (nameof(ConstantChunkOffsetTest), ConstantChunkOffsetTest),
            (nameof(ConstantBiomeProbabilityTest), ConstantBiomeProbabilityTest),
        };

        int passed = 0;
        var totalTimer = Stopwatch.StartNew();
        foreach (var (name, test) in tests)
        {
            var timer = Stopwatch.StartNew();
            bool ok = test();
            timer.Stop();

            if (ok) passed++;
            GD.Print($"[{(ok ? "PASS" : "FAIL")}] {name} ({timer.Elapsed.TotalMilliseconds:F2} ms)");
        }
        totalTimer.Stop();

        GD.Print($"Constant SDF parity: {passed}/{tests.Length} tests passed in {totalTimer.Elapsed.TotalMilliseconds:F2} ms.");
    }

    // ---------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------

    /// <summary>
    /// Tests the sdf with chunkSize = 128, lod = 2, value = 5, operation = set
    /// </summary>
    private bool ConstantSDFBasicTest()
    {
        return RunParityTest(nameof(ConstantSDFBasicTest));
    }

    /// <summary>
    /// Tests the sdf with LOD 1
    /// </summary>
    private bool ConstantLod1Test()
    {
        return RunParityTest(nameof(ConstantLod1Test), lod: 1);
    }

    /// <summary>
    /// Tests the sdf with LOD 4
    /// </summary>
    private bool ConstantLod4Test()
    {
        return RunParityTest(nameof(ConstantLod4Test), lod: 4);
    }

    /// <summary>
    /// Tests the sdf with chunkSize = 8, lod = 1
    /// </summary>
    private bool ConstantChunkSizeSmallTest()
    {
        return RunParityTest(nameof(ConstantChunkSizeSmallTest), chunkSize: 8, lod: 1);
    }

    /// <summary>
    /// Tests the sdf with chunkSize = 2048, lod = 64
    /// </summary>
    private bool ConstantChunkSizeLargeTest()
    {
        return RunParityTest(nameof(ConstantChunkSizeLargeTest), chunkSize: 2048, lod: 64);
    }

    /// <summary>
    /// Tests the sdf with value = 0 (catches a value that is silently left at its default)
    /// </summary>
    private bool ConstantValueZeroTest()
    {
        return RunParityTest(nameof(ConstantValueZeroTest), value: 0f);
    }

    /// <summary>
    /// Tests the sdf with a negative value
    /// </summary>
    private bool ConstantNegativeValueTest()
    {
        return RunParityTest(nameof(ConstantNegativeValueTest), value: -7.5f);
    }

    /// <summary>
    /// Tests the sdf with a large value
    /// </summary>
    private bool ConstantLargeValueTest()
    {
        return RunParityTest(nameof(ConstantLargeValueTest), value: 100000f);
    }

    /// <summary>
    /// Tests the sdf with operation = add
    /// </summary>
    private bool ConstantAddOperationTest()
    {
        return RunParityTest(nameof(ConstantAddOperationTest), value: 2.5f, operation: CpuOperationType.Add);
    }

    /// <summary>
    /// Tests the sdf with operation = subtract
    /// </summary>
    private bool ConstantSubtractOperationTest()
    {
        return RunParityTest(nameof(ConstantSubtractOperationTest), value: 2.5f, operation: CpuOperationType.Subtract);
    }

    /// <summary>
    /// Tests the sdf with operation = multiply
    /// </summary>
    private bool ConstantMultiplyOperationTest()
    {
        return RunParityTest(nameof(ConstantMultiplyOperationTest), value: 2.5f, operation: CpuOperationType.Multiply);
    }

    /// <summary>
    /// Tests the sdf with operation = divide
    /// </summary>
    private bool ConstantDivisionOperationTest()
    {
        return RunParityTest(nameof(ConstantDivisionOperationTest), value: 2.5f, operation: CpuOperationType.Divide);
    }

    /// <summary>
    /// Tests the sdf with operation = divide and value = 0 (every output should be +Infinity on both sides)
    /// </summary>
    private bool ConstantDivisionByZeroTest()
    {
        return RunParityTest(nameof(ConstantDivisionByZeroTest), value: 0f, operation: CpuOperationType.Divide);
    }

    /// <summary>
    /// Tests the sdf with a non-zero chunk offset (the offset's Y is used for the biome depth)
    /// </summary>
    private bool ConstantChunkOffsetTest()
    {
        return RunParityTest(
            nameof(ConstantChunkOffsetTest),
            chunkOffset: new Vector3(32, 48, 64),
            ignoreBiome: false,
            biomeDepth: 64f,
            biomeDepthSpread: 128f);
    }

    /// <summary>
    /// Tests the sdf with biomes enabled so the probability is between 0 and 1 for part of the chunk,
    /// which exercises the lerp/mix between the old and new value.
    /// </summary>
    private bool ConstantBiomeProbabilityTest()
    {
        return RunParityTest(
            nameof(ConstantBiomeProbabilityTest),
            value: 10f,
            operation: CpuOperationType.Add,
            ignoreBiome: false,
            biomeDepth: 64f,
            biomeDepthSpread: 32f);
    }

    // ---------------------------------------------------------------------
    // Shared test runner
    // ---------------------------------------------------------------------

    /// <summary>
    /// Runs the constant SDF on the CPU and GPU with the given parameters and compares every output value.
    /// Both outputs start from the same non-zero data so Add/Subtract/Multiply/Divide are meaningful.
    /// </summary>
    private bool RunParityTest(
        string testName,
        uint chunkSize = 128,
        uint lod = 2,
        float value = 5.0f,
        Vector3? chunkOffset = null,
        CpuOperationType operation = CpuOperationType.Set,
        bool ignoreBiome = true,
        float biomeDepth = 0f,
        float biomeDepthSpread = 0f)
    {
        Vector3 offset = chunkOffset ?? Vector3.Zero;
        GpuOperationType gpuOperation = Enum.Parse<GpuOperationType>(operation.ToString());

        uint adjustedSize = chunkSize / lod + 2;
        uint bufferSize = adjustedSize * adjustedSize * adjustedSize;

        // Shared, non-zero starting data (values between 1.0 and 4.0)
        float[] initial = new float[bufferSize];
        for (uint i = 0; i < bufferSize; i++)
        {
            initial[i] = 1.0f + (i % 7) * 0.5f;
        }

        // ======== CPU =========
        var sdf = new TerrainGeneration.Application.SDFGenerator.CPU.Constant.SDF();
        var constantParameters = new ConstantParameters(value, operation);
        var sdfParametersCPU = new SDFParametersCPU(offset, chunkSize, lod);
        var biomeParametersCPU = new BiomeParametersCPU(0, 0, biomeDepth, biomeDepthSpread, ignoreBiome);
        float[] outputCPU = (float[])initial.Clone();

        var cpuTimer = Stopwatch.StartNew();
        ComputeCPUSDF(sdf, sdfParametersCPU, constantParameters, biomeParametersCPU, outputCPU);
        cpuTimer.Stop();
        GD.Print($"[{testName}] CPU only: {cpuTimer.Elapsed.TotalMilliseconds:F2} ms");

        // ======== GPU =========
        RenderingDevice rd = RenderingServer.GetRenderingDevice();
        var shader = new ConstantShader(rd, "res://Shaders/Compute/constant.glsl");

        byte[] initialBytes = new byte[initial.Length * sizeof(float)];
        Buffer.BlockCopy(initial, 0, initialBytes, 0, initialBytes.Length);
        ComputeBuffer outputBuffer = new ComputeBuffer(
            rd, sizeof(float) * bufferSize, RenderingDevice.UniformType.StorageBuffer, 0, initialBytes);

        var parametersGPU = new ConstantShaderParameters(new TerrainGeneration.Application.SDFGenerator.GPU.Constant.ConstantStage()
        {
            Value = value,
            OperationType = gpuOperation
        });
        var sdfParametersGPU = new SDFParametersGPU(offset, chunkSize, lod);
        var biomeParametersGPU = new BiomeParametersGPU(0, 0, biomeDepth, biomeDepthSpread, ignoreBiome);
        ComputeGPUSDF(rd, chunkSize, lod, shader, sdfParametersGPU, parametersGPU, biomeParametersGPU, outputBuffer);

        float[] outputGPU = outputBuffer.GetData<float>();

        // ======== COMPARE =========
        bool passed = true;
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

    private void ComputeCPUSDF(ISDFFunctionCPU sdf, SDFParametersCPU sdfParametersCPU, ConstantParameters constantParameters, BiomeParametersCPU biomeParameters, float[] output)
    {
        float[] temperatureValues = new float[1];
        sdf.Dispatch(constantParameters, sdfParametersCPU, biomeParameters, temperatureValues, output);
    }

    private void ComputeGPUSDF(RenderingDevice rd, uint chunkSize, uint lod, ISDFShader shader, SDFParametersGPU sDFParameters, ISDFShaderParameters constantShaderParameters, BiomeParametersGPU biomeParameters, ComputeBuffer outputBuffer)
    {
        byte[] sdfParametersGPUBytes = StructHelpers.ToByteArray(sDFParameters);
        byte[] biomeParametersGPUBytes = StructHelpers.ToByteArray(biomeParameters);

        ComputeBuffer sdfParametersBuffer = new ComputeBuffer(rd, sizeof(float) * 8, RenderingDevice.UniformType.UniformBuffer, 0, sdfParametersGPUBytes);
        ComputeBuffer biomeParametersBuffer = new ComputeBuffer(rd, sizeof(float) * 8, RenderingDevice.UniformType.UniformBuffer, 0, biomeParametersGPUBytes);
        ComputeBuffer temperatureBuffer = new ComputeBuffer(rd, sizeof(float), RenderingDevice.UniformType.StorageBuffer, 0);

        shader.Dispatch(chunkSize, lod, constantShaderParameters, sdfParametersBuffer, biomeParametersBuffer, temperatureBuffer, outputBuffer);

        sdfParametersBuffer.Dispose();
        biomeParametersBuffer.Dispose();
        temperatureBuffer.Dispose();
    }
}
