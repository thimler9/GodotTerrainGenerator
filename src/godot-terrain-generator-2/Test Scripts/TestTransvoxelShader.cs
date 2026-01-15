using Godot;
using System;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

using TerrainGeneration.Application.TerrainGenerator;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel.NormalsShader;

public partial class TestTransvoxelShader : Node3D
{
    public uint ChunkSize = 8;
    public uint Lod = 1;

    // Map Params
    public Vector3 ChunkOffset = new Vector3(128, 128, 128);
    public uint Seed = 1;
    public float Scale = 1.0f;
    public float Strength = 1.0f;
    public uint NumOctaves = 1;
    public float Frequency = 1.0f;
    public float Amplitude = 1.0f;
    public float Lacunarity = 1.0f;
    public float Gain = 1.0f;

    // Transvoxel Shader Params
    public uint MaxNumOfVertices = 10000;
    public float TransitionWidth = 1.0f;

    // Transvoxel Params
    public uint MaxNumTerrainMeshesInQueue = 10;

    public override void _Ready()
    {
        RenderingDevice rd = RenderingServer.CreateLocalRenderingDevice();

        SDFShaderParameters sdfShaderParameters = new SDFShaderParameters(ChunkSize, Lod);
        SimplexNoiseShaderParameters simplexNoiseShaderParameters = new SimplexNoiseShaderParameters(
            ChunkOffset,
            Seed,
            Scale,
            Strength,
            NumOctaves,
            Frequency,
            Amplitude,
            Lacunarity,
            Gain
        );
        SimplexNoiseShaderDescriptor simplexNoiseShaderDescriptor = new SimplexNoiseShaderDescriptor()
        {
            ShaderPath = "res://Shaders/Compute/simplex_noise.glsl",
            Parameters = simplexNoiseShaderParameters,
        };

        SDFGeneratorSettings sdfGeneratorSettings = new SDFGeneratorSettings()
        {
            ChunkSize = ChunkSize,
            SDFShaderParameters = sdfShaderParameters,
            SimplexNoiseShaderDescriptor = simplexNoiseShaderDescriptor
        };
        SDFGenerator sdfGenerator = new SDFGenerator(rd, sdfGeneratorSettings);

        sdfGenerator.DispatchShaders(sdfShaderParameters);

        // ---- Get normals
        RDUniform sdfBufferUniform = sdfGenerator.OutputBufferUniform;

        NormalsShaderParameters normalsParaters = new NormalsShaderParameters()
        {
            ChunkSize = ChunkSize,
            Lod = Lod,
        };
        NormalsShaderDescriptor normalsDescriptor = new NormalsShaderDescriptor()
        {
            Parameters = normalsParaters,
            ShaderPath = "res://Shaders/Compute/normal_generator.glsl"
        };

        NormalsShader normalsShader = new NormalsShader(rd, normalsDescriptor);
        normalsShader.Dispatch(normalsParaters, sdfBufferUniform);
        //normalsShader.PrintOutBuffer();

        // --------------------------------------------------------------------------------------------------------------------
        RDUniform normalsBufferUniform = normalsShader.OutputNormalsUniform;

        TransvoxelShaderParameters transvoxelShaderParameters = new TransvoxelShaderParameters()
        {
            ChunkOffset = ChunkOffset,
            Lod = Lod,
            ChunkSize = ChunkSize,
            MaxNumVertices = MaxNumOfVertices,
            TransitionWidth = TransitionWidth,
        };

        TransvoxelShaderDescriptor transvoxelShaderDescriptor = new TransvoxelShaderDescriptor()
        {
            Parameters = transvoxelShaderParameters,
            ShaderPath = "res://Shaders/Compute/mesh_generator.glsl"
        };

        IndirectArgsShaderDescriptor indirectArgsShaderDescriptor = new IndirectArgsShaderDescriptor()
        {
            ShaderPath = "res://Shaders/Compute/indirect_args.glsl"
        };

        TransvoxelDescriptor transvoxelDescriptor = new TransvoxelDescriptor()
        {
            TransvoxelShaderDescriptor = transvoxelShaderDescriptor,
            IndirectArgsShaderDescriptor = indirectArgsShaderDescriptor,
            MaxNumTerrainMeshesInQueue = MaxNumTerrainMeshesInQueue,
        };

        Transvoxel transvoxel = new Transvoxel(rd, transvoxelDescriptor);

        TerrainMesh terrainMesh = transvoxel.GetTerrainMesh(transvoxelShaderParameters, sdfBufferUniform, normalsBufferUniform);
        terrainMesh.PrintVertices();
        terrainMesh.PrintIndirectArgs();
    }
}