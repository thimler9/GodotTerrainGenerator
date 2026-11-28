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

        if (sdfPipeline.CPUSDFPipeline == null)
        {
            throw new Exception("CPU SDF Pipeline is null");
        }

        if (sdfPipeline.GPUSDFPipeline == null)
        {
            throw new Exception("GPU SDF Pipeline is null");
        }

        Vector3 chunkOffset = new Vector3(0, 0, 0);

        var totalTimer = Stopwatch.StartNew();
        float[] outputCPU = sdfPipeline.CPUSDFPipeline.GetSDF(chunkOffset, ChunkSize, Lod);
        float[] outputGPU = sdfPipeline.GPUSDFPipeline.GetSDF(new TerrainGeneration.Application.SDFGenerator.SDFParametersGPU(chunkOffset, ChunkSize, Lod)).GetData<float>();
        totalTimer.Stop();

        Transvoxel cpuMarchingCubes = new Transvoxel();
        TransvoxelParameters parameters = new TransvoxelParameters()
        {
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

        var colliderCreationTime = Stopwatch.StartNew();
        ConcavePolygonShape3D terrainCollider = new ConcavePolygonShape3D();
        terrainCollider.SetFaces(vertices);

        CollisionShape3D collisionShape3D = new CollisionShape3D();
        collisionShape3D.Shape = terrainCollider;

        StaticBody3D body3D = new StaticBody3D();
        body3D.AddChild(collisionShape3D);

        AddChild(body3D);
        colliderCreationTime.Stop();
        GD.Print($"Collider Creation Time: {colliderCreationTime.Elapsed.TotalMilliseconds}");
    }
}
