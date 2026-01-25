using Godot;
using System;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

using TerrainGeneration.Application.TerrainGenerator;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel.NormalsShader;

public partial class TestNormalsShader : Node3D
{
    public uint ChunkSize = 8;
    public uint Lod = 1;

    public Vector3 ChunkOffset = new Vector3(128, 128, 128);
    public uint Seed = 1;
    public float Scale = 1.0f;
    public float Strength = 1.0f;
    public uint NumOctaves = 1;
    public float Frequency = 1.0f;
    public float Amplitude = 1.0f;
    public float Lacunarity = 1.0f;
    public float Gain = 1.0f;

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

        ISDFShader simplexNoiseShader = new SimplexNoiseShader(rd, simplexNoiseShaderDescriptor);

        SDFGeneratorSettings sdfGeneratorSettings = new SDFGeneratorSettings()
        {
            ChunkSize = ChunkSize,
            SDFShaderParameters = sdfShaderParameters,
            SDFShader = simplexNoiseShader
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
        normalsShader.PrintOutBuffer();
    }
}