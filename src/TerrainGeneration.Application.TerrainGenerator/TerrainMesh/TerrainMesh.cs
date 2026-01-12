using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator;

namespace TerrainGeneration.Application.TerrainGenerator;
internal class TerrainMesh
{
    public Rid VertexBuffer;
    public Rid IndirectArgsBuffer;

    public TerrainMeshParameters? TerrainMeshParameters;
    public Rid TerrainMeshParamsUniformBuffer;
    public Rid TerrainMeshParamsUniformSet;

    public TerrainMesh(TerrainMeshParameters parameters)
    {
        // Get Map Buffer
        SDFGenerator.SDFGenerator? sdfGenerator = parameters.SDFGenerator;
        if (sdfGenerator == null)
        {
            throw new ArgumentNullException($"{nameof(parameters.SDFGenerator)} cannot be null.");
        }

        sdfGenerator.DispatchShaders(parameters.SDFShaderParameters);

        // Get Normals Buffer
        RDUniform sdfBufferUniform = sdfGenerator.GetSDFBufferUniform();

        NormalsShader.NormalsShader? normalsShader = parameters.NormalsShader;
        if (normalsShader == null)
        {
            throw new ArgumentNullException($"{nameof(normalsShader)} cannot be null.");
        }

        normalsShader.Dispatch(parameters.NormalsShaderParameters, sdfBufferUniform);
        // Get Vertex Buffer
        
        // Get Indirect Args Buffer
    }
}
