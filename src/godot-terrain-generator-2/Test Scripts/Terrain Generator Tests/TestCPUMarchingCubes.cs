using Godot;
using System;
using System.Diagnostics;
using TerrainGeneration.Application.SDFGenerator.Pipeline;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel.CPU;
using TerrainGeneration.Utilities.Math.Extensions;

public partial class TestCPUMarchingCubes : Node
{
    [Export]
    public uint ChunkSize;

    [Export]
    public uint Lod;

    [Export]
    public uint MaxTrianglesToPrint;

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

        var totalTimer = Stopwatch.StartNew();
        float[] outputCPU = sdfPipeline.cPUSDFPipeline.GetSDF(chunkOffset, ChunkSize, Lod);
        float[] outputGPU = sdfPipeline.gPUSDFPipeline.GetSDF(new TerrainGeneration.Application.SDFGenerator.SDFParametersGPU(chunkOffset, ChunkSize, Lod)).GetData<float>();
        totalTimer.Stop();

        Transvoxel cpuMarchingCubes = new Transvoxel();
        TransvoxelParameters parameters = new TransvoxelParameters()
        {
            BorderWidth = 0,
            ChunkSize = ChunkSize,
            Lod = Lod,
        };

        var marchingCubesTimer = Stopwatch.StartNew();
        Vector3[] vertices = cpuMarchingCubes.GenerateMarchingCubes(parameters, outputCPU);
        marchingCubesTimer.Stop();
        for (int i = 0; i < Math.Min(vertices.Length, MaxTrianglesToPrint); i++)
        {
            Vector3 currVertex = vertices[i];
            GD.Print($"{i} = ({currVertex.X}, {currVertex.Y}, {currVertex.Z})");
        }

        GD.Print($"SDF Time: {totalTimer.Elapsed.TotalMilliseconds}");
        GD.Print($"Marching Cubes Time: {marchingCubesTimer.Elapsed.TotalMilliseconds}");
    }
}
