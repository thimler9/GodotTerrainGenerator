using Godot;
using System;
using System.Runtime.InteropServices;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

public partial class TestMapGenerator : Node3D
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

        SDFShaderParameters sdfShaderParameters = new SDFShaderParameters(ChunkOffset, ChunkSize, Lod);
        SimplexNoiseShaderParameters simplexNoiseShaderParameters = new SimplexNoiseShaderParameters(
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
            SDFShaderParameters = sdfShaderParameters,
            SDFShader = simplexNoiseShader
        };
        SDFGenerator sdfGenerator = new SDFGenerator(rd, sdfGeneratorSettings);

        sdfGenerator.DispatchShaders(sdfShaderParameters);
        sdfGenerator.PrintOutBuffer();
    }
}