using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Utilities.EngineAbstractions;

namespace TerrainGeneration.Application.TerrainGenerator;
public class TerrainMeshRenderDescriptor
{
    public required Rid RenderPipeline;
    public required Rid EmptyVertexArray;
    public required Color[] ClearColors;
    public required Rid ScreenBuffer;
    public required GraphicShader Shader;
    public required Rid RenderSceneDataUniformSet;
    public required Rid TerrainConstantsUniformSet;
}
