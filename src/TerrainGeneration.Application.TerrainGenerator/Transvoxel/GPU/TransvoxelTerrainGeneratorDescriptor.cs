using Godot;
using TerrainGeneration.Application.SDFGenerator.Pipeline.GPU;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel;
public class TransvoxelTerrainGeneratorDescriptor
{
    // SDF Pipeline
    public required SDFPipelineGPU SDFPipeline;

    // Normals Shader
    public required string? NormalsShaderPath;

    //Transvoxel Params
    public required uint ChunkSize;
    public required uint Lod;
    public required float BorderWidth;
    public required Vector3 ChunkOffset;
    public required uint MaxNumVertices;
    public required string? TransvoxelShaderPath;

    // Indirect Args Params
    public required string? IndirectArgsShaderPath;

    // Other Params
    public required uint MaxNumTerrainMeshesInQueue;
}
