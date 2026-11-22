using Godot;
using System;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.CPU;
using TerrainGeneration.Application.SDFGenerator.CPU.Sphere;
using TerrainGeneration.Application.SDFGenerator.GPU.Sphere;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;
using TerrainGeneration.Application.SDFGenerator.Pipeline.GPU;
using TerrainGeneration.Utilities.EngineAbstractions;
using TerrainGeneration.Utilities.Math.Extensions;
using TerrainGeneration.Utilities.Struct;

using CpuOperationType = TerrainGeneration.Application.SDFGenerator.CPU.OperationType;
using GpuOperationType = TerrainGeneration.Application.SDFGenerator.GPU.OperationType;

public partial class TestSphereSDF : Node
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
            (nameof(SphereSDFBasicTest), SphereSDFBasicTest),
            (nameof(SphereLod1Test), SphereLod1Test),
            (nameof(SphereLod4Test), SphereLod4Test),
            (nameof(SphereChunkSizeSmallTest), SphereChunkSizeSmallTest),
            (nameof(SphereChunkSizeLargeTest), SphereChunkSizeLargeTest),
            (nameof(SphereRadius0Test), SphereRadius0Test),
            (nameof(SphereRadius100Test), SphereRadius100Test),
            (nameof(SphereNewCenterTest), SphereNewCenterTest),
            (nameof(SphereSubtractOperationTest), SphereSubtractOperationTest),
            (nameof(SphereAddOperationTest), SphereAddOperationTest),
            (nameof(SphereMultiplyOperationTest), SphereMultiplyOperationTest),
            (nameof(SphereDivisionOperationTest), SphereDivisionOperationTest),
        };

        int passed = 0;
        foreach (var (name, test) in tests)
        {
            bool ok = test();
            if (ok) passed++;
            GD.Print($"[{(ok ? "PASS" : "FAIL")}] {name}");
        }

        GD.Print($"Sphere SDF parity: {passed}/{tests.Length} tests passed.");
    }

    // ---------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------

    private bool SphereSDFBasicTest()
    {
        return RunParityTest(nameof(SphereSDFBasicTest));
    }

    /// <summary>
    /// Tests the sdf with LOD 1
    /// </summary>
    private bool SphereLod1Test()
    {
        return RunParityTest(nameof(SphereLod1Test), lod: 1);
    }

    /// <summary>
    /// Tests the sdf with LOD 4
    /// </summary>
    private bool SphereLod4Test()
    {
        return RunParityTest(nameof(SphereLod4Test), lod: 4);
    }

    /// <summary>
    /// Tests the sdf with chunkSize = 8, lod = 1
    /// </summary>
    private bool SphereChunkSizeSmallTest()
    {
        return RunParityTest(nameof(SphereChunkSizeSmallTest), chunkSize: 8, lod: 1);
    }

    /// <summary>
    /// Tests the sdf with chunkSize = 2048, lod = 64
    /// </summary>
    private bool SphereChunkSizeLargeTest()
    {
        return RunParityTest(nameof(SphereChunkSizeLargeTest), chunkSize: 2048, lod: 64);
    }

    /// <summary>
    /// Tests the sdf with radius = 0
    /// </summary>
    private bool SphereRadius0Test()
    {
        return RunParityTest(nameof(SphereRadius0Test), radius: 0f);
    }

    /// <summary>
    /// Tests the sdf with radius = 100
    /// </summary>
    private bool SphereRadius100Test()
    {
        return RunParityTest(nameof(SphereRadius100Test), radius: 100f);
    }

    /// <summary>
    /// Tests the sdf with center = (10, 50, 75) chunkSize = 512 lod = 8
    /// </summary>
    private bool SphereNewCenterTest()
    {
        return RunParityTest(
            nameof(SphereNewCenterTest),
            chunkSize: 512,
            lod: 8,
            center: new Vector3(10, 50, 75));
    }

    /// <summary>
    /// Tests the sdf with operation = subtraction
    /// </summary>
    private bool SphereSubtractOperationTest()
    {
        return RunParityTest(nameof(SphereSubtractOperationTest), operation: CpuOperationType.Subtract);
    }

    /// <summary>
    /// Tests the sdf with operation = add
    /// </summary>
    private bool SphereAddOperationTest()
    {
        return RunParityTest(nameof(SphereAddOperationTest), operation: CpuOperationType.Add);
    }

    /// <summary>
    /// Tests the sdf with operation multiply
    /// </summary>
    private bool SphereMultiplyOperationTest()
    {
        return RunParityTest(nameof(SphereMultiplyOperationTest), operation: CpuOperationType.Multiply);
    }

    /// <summary>
    /// Tests the sdf with operation division
    /// </summary>
    private bool SphereDivisionOperationTest()
    {
        return RunParityTest(nameof(SphereDivisionOperationTest), operation: CpuOperationType.Divide);
    }

    // ---------------------------------------------------------------------
    // Shared test runner
    // ---------------------------------------------------------------------

    /// <summary>
    /// Runs the sphere SDF on the CPU and GPU with the given parameters and compares every output value.
    /// Both outputs start from the same non-zero data so Add/Subtract/Multiply/Divide are meaningful.
    /// </summary>
    private bool RunParityTest(
        string testName,
        uint chunkSize = 128,
        uint lod = 2,
        float radius = 5.0f,
        Vector3? center = null,
        Vector3? chunkOffset = null,
        CpuOperationType operation = CpuOperationType.Set)
    {
        Vector3 sphereCenter = center ?? Vector3.Zero;
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
        var sdf = new TerrainGeneration.Application.SDFGenerator.CPU.Sphere.SDF();
        var sphereParameters = new SphereParameters(radius, sphereCenter, operation);
        var sdfParametersCPU = new SDFParametersCPU(offset, chunkSize, lod);
        var biomeParametersCPU = new BiomeParametersCPU(0, 0, 0, 0, true);
        float[] outputCPU = (float[])initial.Clone();
        ComputeCPUSDF(sdf, sdfParametersCPU, sphereParameters, biomeParametersCPU, outputCPU);

        // ======== GPU =========
        RenderingDevice rd = RenderingServer.GetRenderingDevice();
        var shader = new SphereShader(rd, "res://Shaders/Compute/sphere.glsl");

        byte[] initialBytes = new byte[initial.Length * sizeof(float)];
        Buffer.BlockCopy(initial, 0, initialBytes, 0, initialBytes.Length);
        ComputeBuffer outputBuffer = new ComputeBuffer(
            rd, sizeof(float) * bufferSize, RenderingDevice.UniformType.StorageBuffer, 0, initialBytes);

        var parametersGPU = new SphereShaderParameters(new TerrainGeneration.Application.SDFGenerator.GPU.Sphere.SphereStage()
        {
            Center = sphereCenter,
            OperationType = gpuOperation,
            Radius = radius
        });
        var sdfParametersGPU = new SDFParametersGPU(offset, chunkSize, lod);
        var biomeParametersGPU = new BiomeParametersGPU(0, 0, 0, 0, true);
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
        shader.Dispose();
        return passed;
    }

    private void ComputeCPUSDF(ISDFFunctionCPU sdf, SDFParametersCPU sdfParametersCPU, SphereParameters sphereParameters, BiomeParametersCPU biomeParameters, float[] output)
    {
        float[] temperatureValues = new float[1];
        sdf.Dispatch(sphereParameters, sdfParametersCPU, biomeParameters, temperatureValues, output);
    }

    private void ComputeGPUSDF(RenderingDevice rd, uint chunkSize, uint lod, ISDFShader shader, SDFParametersGPU sDFParameters, ISDFShaderParameters sphereShaderParameters, BiomeParametersGPU biomeParameters, ComputeBuffer outputBuffer)
    {
        byte[] sdfParametersGPUBytes = StructHelpers.ToByteArray(sDFParameters);
        byte[] biomeParametersGPUBytes = StructHelpers.ToByteArray(biomeParameters);

        ComputeBuffer sdfParametersBuffer = new ComputeBuffer(rd, sizeof(float) * 8, RenderingDevice.UniformType.UniformBuffer, 0, sdfParametersGPUBytes);
        ComputeBuffer biomeParametersBuffer = new ComputeBuffer(rd, sizeof(float) * 8, RenderingDevice.UniformType.UniformBuffer, 0, biomeParametersGPUBytes);
        ComputeBuffer temperatureBuffer = new ComputeBuffer(rd, sizeof(float), RenderingDevice.UniformType.StorageBuffer, 0);

        shader.Dispatch(chunkSize, lod, sphereShaderParameters, sdfParametersBuffer, biomeParametersBuffer, temperatureBuffer, outputBuffer);

        sdfParametersBuffer.Dispose();
        biomeParametersBuffer.Dispose();
        temperatureBuffer.Dispose();
    }
}