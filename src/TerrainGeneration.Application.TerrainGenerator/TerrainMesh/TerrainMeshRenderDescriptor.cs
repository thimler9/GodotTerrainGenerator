using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.TerrainGenerator;
public class TerrainMeshRenderDescriptor
{
    public required Rid RenderPipeline;
    public required Rid EmptyVertexArray;
    public required Color[] ClearColors;
    public required Rid ScreenBuffer;
    public required Rid Shader;
    public required Rid RenderSceneDataUniformSet;
    public required Rid TerrainConstantsUniformSet;
}
