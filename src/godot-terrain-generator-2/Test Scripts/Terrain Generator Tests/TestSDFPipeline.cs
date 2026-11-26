using Godot;
using System;
using System.Diagnostics;
using TerrainGeneration.Application.SDFGenerator.Pipeline;
using TerrainGeneration.Utilities.EngineAbstractions;
using TerrainGeneration.Utilities.Math.Extensions;
using static System.Net.Mime.MediaTypeNames;

public partial class TestSDFPipeline : Node
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

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		RenderingDevice rd = RenderingServer.GetRenderingDevice();
		SDFPipelineParser sdfPipelineParser = new SDFPipelineParser();
		SDFPipeline sdfPipeline = sdfPipelineParser.ParseFromFile("res://Test Scripts/Terrain Generator Tests/TerrainPipelineTest.json", rd);

		if (sdfPipeline.cPUSDFPipeline == null)
		{
			throw new Exception("CPU SDF Pipeline is null");
        }

		if (sdfPipeline.gPUSDFPipeline == null)
		{
			throw new Exception("GPU SDF Pipeline is null");
        }

        Vector3 chunkOffset = new Vector3(0, 0, 0);
        uint chunkSize = 32;
		uint lod = 2;
		
        var totalTimer = Stopwatch.StartNew();
        float[] outputCPU = sdfPipeline.cPUSDFPipeline.GetSDF(chunkOffset, chunkSize, lod);
		float[] outputGPU = sdfPipeline.gPUSDFPipeline.GetSDF(new TerrainGeneration.Application.SDFGenerator.SDFParametersGPU(chunkOffset, chunkSize, lod)).GetData<float>();
        totalTimer.Stop();

        // ======== COMPARE =========
        bool passed = true;
        if (outputGPU.Length < outputCPU.Length)
        {
            GD.PrintErr($"[BasicTest] GPU output is smaller than CPU output ({outputGPU.Length} < {outputCPU.Length}).");
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
                    GD.PrintErr($"[BasicTest] Mismatch at index {i}: CPU = {outputCPU[i]}, GPU = {outputGPU[i]}");
                }
            }

            if (mismatches > 0)
            {
                GD.PrintErr($"[BasicTest] {mismatches} of {outputCPU.Length} values mismatched.");
            }
        }

        GD.Print($"Constant SDF parity: {passed}/{1} tests passed in {totalTimer.Elapsed.TotalMilliseconds:F2} ms.");

    }
}
