using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.TerrainGenerator;
public class TerrainMeshDrawDescriptor
{
    public Rid RenderPipeline;
    public Rid SreenBuffer;
    public Rid EmptyVertexArrayBuffer;
    public Rid Shader;
}
