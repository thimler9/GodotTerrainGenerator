using TerrainGeneration.Application.SDFGenerator;

namespace TerrainGeneration.Application.TerrainGenerator;
internal class TerrainMeshParameters
{
    public SDFGenerator.SDFGenerator? SDFGenerator;
    public SDFGenerator.SDFShaderParameters SDFShaderParameters;

    public NormalsShader.NormalsShader? NormalsShader;
    public NormalsShader.NormalsShaderParameters NormalsShaderParameters;
}
