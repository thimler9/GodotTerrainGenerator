using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.TerrainGenerator;
using TerrainGeneration.Application.VoxelOctree;
using TerrainGeneration.Application.VoxelOctree.Abstractions.RenderOctree;
using static System.Net.Mime.MediaTypeNames;

namespace GodotTerrainGenerator2.Test_Scripts;

[Tool]
[GlobalClass]
public partial class TestVoxelOctreeRenderer : CompositorEffect
{
    public VoxelOctree VoxelOctree;
    public Camera3D Camera;

    RenderingDevice Rd;
    public Rid TerrainShader;
    public Rid RenderPipeline;

    // Per frame rendering data
    public Rid EmptyVertexArray;
    public Color[] ClearColors = [new Color(0.0f, 0.0f, 0.0f, 0.0f)];
    public Rid ColorTexture;
    public Rid DepthTexture;
    public Rid ScreenBuffer;
    public RDUniform RenderSceneDataUniform;
    public Rid RenderSceneDataUniformSet;

    public Rid TerrainMeshConstantsBuffer;
    public RDUniform TerrainConstantsUniform;
    public Rid TerrainConstantsUniformSet;

    public void Init(RenderSceneBuffersRD renderSceneBuffers, Rid renderSceneDataBuffer)
    {
        SetRenderPipeline(renderSceneBuffers, renderSceneDataBuffer);
    }


    public override void _RenderCallback(int effectCallbackType, RenderData renderData)
    {
        if (effectCallbackType != (int)EffectCallbackTypeEnum.PostTransparent)
        {
            return;
        }

        using RenderSceneBuffersRD renderSceneBuffers = renderData.GetRenderSceneBuffers() as RenderSceneBuffersRD;

        Rid renderSceneDataBuffer = renderData.GetRenderSceneData().GetUniformBuffer();

        if (renderSceneBuffers != null && renderSceneDataBuffer.IsValid)
        {
            var size = renderSceneBuffers.GetInternalSize();
            // Can't render if screen is 0 size
            if (size.X == 0 && size.Y == 0)
            {
                return;
            }
            // Create Render Pipeline and Setup buffers if there aren't any
            else if (Rd == null)
            {
                Init(renderSceneBuffers, renderSceneDataBuffer);
            }
            // Update camera buffers
            else
            {
                var newColorTexture = renderSceneBuffers.GetColorTexture();
                var newDepthTexture = renderSceneBuffers.GetDepthTexture();

                if (newColorTexture != ColorTexture || newDepthTexture != DepthTexture)
                {
                    ColorTexture = newColorTexture;
                    DepthTexture = newDepthTexture;

                    if (Rd.FramebufferIsValid(ScreenBuffer))
                    {
                        Rd.FreeRid(ScreenBuffer);
                    }

                    ScreenBuffer = Rd.FramebufferCreate([newColorTexture, newDepthTexture]);
                }
            }

            if (VoxelOctree != null)
            {
                VoxelOctree.Render(Camera.GetFrustum(), new TerrainMeshRenderDescriptor()
                {
                    RenderPipeline = RenderPipeline,
                    RenderSceneDataUniformSet = RenderSceneDataUniformSet,
                    ClearColors = ClearColors,
                    EmptyVertexArray = EmptyVertexArray,
                    ScreenBuffer = ScreenBuffer,
                    Shader = TerrainShader,
                    TerrainConstantsUniformSet = TerrainConstantsUniformSet,
                });
            }

        }
    }

    private void SetRenderPipeline(RenderSceneBuffersRD renderSceneBuffers, Rid renderSceneDataBuffer)
    {
        Rd = RenderingServer.GetRenderingDevice();

        RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://Shaders/Graphic/terrain_shader.glsl");
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        TerrainShader = Rd.ShaderCreateFromSpirV(shaderBytecode);

        // Position descriptor for vertices
        RDVertexAttribute vertexAttributePosition = new RDVertexAttribute()
        {
            Format = RenderingDevice.DataFormat.R32G32Sfloat,
            Frequency = RenderingDevice.VertexFrequency.Vertex,
            Location = 0,
            Offset = 0,
            Stride = sizeof(float) * 2
        };
        long vertexFormat = Rd.VertexFormatCreate([vertexAttributePosition]);


        float[] vertexPositionsFake = new float[] {
            0.0f, -0.5f,
            0.5f, 0.5f,
            -0.5f, 0.5f,
        };
        byte[] verticesPositionsBytes = new byte[vertexPositionsFake.Length * sizeof(float)];
        Buffer.BlockCopy(vertexPositionsFake, 0, verticesPositionsBytes, 0, verticesPositionsBytes.Length);
        Rid dummyVertexPositionBuffer = Rd.VertexBufferCreate((uint)verticesPositionsBytes.Length, verticesPositionsBytes, false);

        // Vertex array can be empty since we're doing indirect drawing
        EmptyVertexArray = Rd.VertexArrayCreate(3, vertexFormat, [dummyVertexPositionBuffer]);

        RDPipelineRasterizationState rasterizationState = new RDPipelineRasterizationState()
        {
            Wireframe = false,
            CullMode = RenderingDevice.PolygonCullMode.Back,
            EnableDepthClamp = false,
            LineWidth = 1.0f,
            FrontFace = RenderingDevice.PolygonFrontFace.Clockwise,
            DepthBiasEnabled = false
        };

        RDPipelineMultisampleState multisampleState = new RDPipelineMultisampleState()
        {
            EnableSampleShading = false,
            SampleCount = RenderingDevice.TextureSamples.Samples1,
            MinSampleShading = 1.0f
        };

        RDPipelineDepthStencilState depthStencilState = new RDPipelineDepthStencilState()
        {
            EnableDepthTest = true,
            EnableDepthWrite = true,
            DepthCompareOperator = RenderingDevice.CompareOperator.Greater,
            EnableStencil = false
        };

        RDPipelineColorBlendState blendState = new RDPipelineColorBlendState()
        {
            EnableLogicOp = false,
            LogicOp = RenderingDevice.LogicOperation.Copy,
        };

        RDPipelineColorBlendStateAttachment blendStateAttachment = new RDPipelineColorBlendStateAttachment()
        {
            EnableBlend = true,
            WriteA = true,
            WriteB = true,
            WriteG = true,
            WriteR = true,
            AlphaBlendOp = RenderingDevice.BlendOperation.Add,
            ColorBlendOp = RenderingDevice.BlendOperation.Add,
            SrcColorBlendFactor = RenderingDevice.BlendFactor.One,
            DstColorBlendFactor = RenderingDevice.BlendFactor.Zero,
            SrcAlphaBlendFactor = RenderingDevice.BlendFactor.One,
            DstAlphaBlendFactor = RenderingDevice.BlendFactor.Zero,
        };

        blendState.Attachments.Add(blendStateAttachment);

        ColorTexture = renderSceneBuffers.GetColorTexture();
        DepthTexture = renderSceneBuffers.GetDepthTexture();
        ScreenBuffer = Rd.FramebufferCreate([ColorTexture, DepthTexture]);

        long frameBufferFormat = Rd.FramebufferGetFormat(ScreenBuffer);

        RenderPipeline = Rd.RenderPipelineCreate(
            TerrainShader,
            frameBufferFormat,
            vertexFormat,
            RenderingDevice.RenderPrimitive.Triangles,
            rasterizationState,
            multisampleState,
            depthStencilState,
            blendState,
            0,
            0,
            []
        );

        ClearColors = new Color[] { new Color(0.0f, 0.0f, 0.0f, 0.0f) };

        // Set camera projection
        RenderSceneDataUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0,
        };
        RenderSceneDataUniform.AddId(renderSceneDataBuffer);
        RenderSceneDataUniformSet = Rd.UniformSetCreate([RenderSceneDataUniform], TerrainShader, 0);

        // Set camera projection
        TerrainConstantsUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0,
        };
        TerrainConstantsUniform.AddId(TerrainMeshConstantsBuffer);
        TerrainConstantsUniformSet = Rd.UniformSetCreate([TerrainConstantsUniform], TerrainShader, 3);
    }

}
