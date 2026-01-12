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
        Rid sdfBuffer = sdfGenerator.GetSDFBuffer();



        // Get Vertex Buffer
        
        // Get Indirect Args Buffer
    }
}
