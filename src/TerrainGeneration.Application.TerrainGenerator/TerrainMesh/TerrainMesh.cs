using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.TerrainGenerator;
internal class TerrainMesh
{
    public Rid VertexBuffer;
    public Rid IndirectArgsBuffer;

    public TerrainMeshParameters? TerrainMeshParameters;
    public Rid TerrainMeshParamsUniformBuffer;
    public Rid TerrainMeshParamsUniformSet;
}
