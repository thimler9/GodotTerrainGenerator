using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.Abstractions;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel;
public class TransvoxelTerrainGeneratorDescriptor
{
    // SDF Generator
    public required ISDFShader SDFShader;

    // Normals Shader
    public required string? NormalsShaderPath;

    //Transvoxel Params
    public required uint ChunkSize;
    public required uint Lod;
    public required float TransitionWidth;
    public required Vector3 ChunkOffset;
    public required uint MaxNumVertices;
    public required string? TransvoxelShaderPath;

    // Indirect Args Params
    public required string? IndirectArgsShaderPath;

    // Other Params
    public required uint MaxNumTerrainMeshesInQueue;
}
