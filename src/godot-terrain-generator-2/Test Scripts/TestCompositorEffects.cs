using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;

namespace GodotTerrainGenerator2.Test_Scripts;

[Tool]
[GlobalClass]
public partial class TestCompositorEffects : CompositorEffect
{
    // Overall rendering objects
    public RenderingDevice Rd;
    public Rid IndirectDrawShader;
    public Rid IndirectDrawPipeline;

    // Indirect Draw Data
    public Rid VertexBuffer;
    public RDUniform VertexUniform;
    public Rid VertexBufferUniformSet;
    public Rid IndirectArgsBuffer;

    // Per Frame Rendering Data
    public Rid EmptyVertexArray;
    public Color[] ClearColors;
    public Rid ColorTexture;
    public Rid DepthTexture;
    public Rid ScreenBuffer;


    public TestCompositorEffects() : base()
    {
        EffectCallbackType = EffectCallbackTypeEnum.PreOpaque;
    }

    // System notifications, we want to react on the notification that
    // alerts us we are about to be destroyed.
    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            if (Rd == null)
            {
                return;
            }

            if (IndirectDrawShader.IsValid)
            {
                // Freeing our shader will also free any dependents such as the pipeline!
                Rd.FreeRid(IndirectDrawShader);
            }
        }
    }

    public override void _RenderCallback(int effectCallbackType, RenderData renderData)
    {
        // We only render on PreOpaque
        if (effectCallbackType != (int)EffectCallbackTypeEnum.PreOpaque)
        {
            return;
        }

        using RenderSceneBuffersRD renderSceneBuffers = renderData.GetRenderSceneBuffers() as RenderSceneBuffersRD;
        
        if (renderSceneBuffers != null)
        {
            var size = renderSceneBuffers.GetInternalSize();
            if (size.X == 0 && size.Y == 0)
            {
                return;
            }

            if (Rd == null)
            {
                SetRenderPipeline("res://Shaders/Graphic/terrain_shader.glsl", renderSceneBuffers);
            }
            else
            {
                var newColorTexture = renderSceneBuffers.GetColorTexture();
                var newDepthTexture = renderSceneBuffers.GetDepthTexture();

                if (ColorTexture != newColorTexture && DepthTexture != newDepthTexture)
                {
                    ColorTexture = newColorTexture;
                    DepthTexture = newDepthTexture;

                    if (Rd.FramebufferIsValid(ScreenBuffer))
                    {
                        Rd.FreeRid(ScreenBuffer);
                    }

                    ScreenBuffer = Rd.FramebufferCreate([ColorTexture, DepthTexture]);
                }
            }

            //if (VertexBufferUniformSet.IsValid)
            //{
                //long drawList = Rd.DrawListBegin(ScreenBuffer, RenderingDevice.DrawFlags.ClearColor0, ClearColors);

                //Rd.DrawCommandBeginLabel("Test Indirect Draw", new Color(0.0f, 0.0f, 0.0f, 1.0f));
                //Rd.DrawListBindVertexArray(drawList, EmptyVertexArray);
                //Rd.DrawListBindRenderPipeline(drawList, IndirectDrawPipeline);
                //Rd.DrawListBindUniformSet(drawList, VertexBufferUniformSet, 0);
                //Rd.DrawListDrawIndirect(drawList, false, IndirectArgsBuffer);
                //Rd.DrawListEnd();
                //Rd.DrawCommandEndLabel();


                Rd.DrawCommandBeginLabel("Test Indirect Draw", new Color(1.0f, 1.0f, 1.0f, 1.0f));
                long drawList = Rd.DrawListBegin(ScreenBuffer, RenderingDevice.DrawFlags.ClearColor0, ClearColors);
                Rd.DrawListBindRenderPipeline(drawList, IndirectDrawPipeline);
                Rd.DrawListBindVertexArray(drawList, EmptyVertexArray);
                Rd.DrawListDraw(drawList, false, 1, 0);
                Rd.DrawListEnd();
                Rd.DrawCommandEndLabel();
            //}
        }
    }



    private void SetRenderPipeline(string? shaderPath, RenderSceneBuffersRD renderSceneBuffers)
    {
        Rd = RenderingServer.GetRenderingDevice();

        RDShaderFile shaderFile = GD.Load<RDShaderFile>(shaderPath);
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        IndirectDrawShader = Rd.ShaderCreateFromSpirV(shaderBytecode);

        // Position descriptor for vertices
        RDVertexAttribute vertexAttributePosition = new RDVertexAttribute()
        {
            Format = RenderingDevice.DataFormat.R32G32Sfloat,
            Frequency = RenderingDevice.VertexFrequency.Vertex,
            Location = 0,
            Offset = 0,
            Stride = sizeof(float) * 2
        };

        RDVertexAttribute vertexAttributeColor = new RDVertexAttribute()
        {
            Format = RenderingDevice.DataFormat.R32G32B32Sfloat,
            Frequency = RenderingDevice.VertexFrequency.Vertex,
            Location = 1,
            Offset = 0,
            Stride = sizeof(float) * 3
        };

        long vertexFormat = Rd.VertexFormatCreate([vertexAttributePosition, vertexAttributeColor]);

        
        float[] vertexPositionsFake = new float[] {
            0.0f, -0.5f,
            0.5f, 0.5f,
            -0.5f, 0.5f,
        };
        byte[] verticesPositionsBytes = new byte[vertexPositionsFake.Length * sizeof(float)];
        Buffer.BlockCopy(vertexPositionsFake, 0, verticesPositionsBytes, 0, verticesPositionsBytes.Length);
        Rid dummyVertexPositionBuffer = Rd.VertexBufferCreate((uint)verticesPositionsBytes.Length, verticesPositionsBytes, false);

        float[] vertexColorsFake = new float[] {
            1, 0, 0,
            0, 1, 0,
            0, 0, 1
        };
        byte[] vertexColorBytes = new byte[vertexColorsFake.Length * sizeof(float)];
        Buffer.BlockCopy(vertexColorsFake, 0, vertexColorBytes, 0, vertexColorBytes.Length);
        Rid dummyVertexColorBuffer = Rd.VertexBufferCreate((uint)vertexColorBytes.Length, vertexColorBytes);

        // Vertex array can be empty since we're doing indirect drawing
        EmptyVertexArray = Rd.VertexArrayCreate(3, vertexFormat, [dummyVertexPositionBuffer, dummyVertexColorBuffer]);

        RDPipelineRasterizationState rasterizationState = new RDPipelineRasterizationState()
        {
            Wireframe = false,
            CullMode = RenderingDevice.PolygonCullMode.Disabled,
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
            EnableDepthTest = false,
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

        IndirectDrawPipeline = Rd.RenderPipelineCreate(
            IndirectDrawShader,
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

        ClearColors = new Color[] { new Color(0.0f, 0.0f, 0.0f, 1.0f) };

        //// Set Vertex Data
        //float[] drawVertices = new float[]
        //{
        //    -0.5f, -0.5f, 0.5f, 0.0f,
        //    0.0f, -0.5f, 0.5f, 0.0f,
        //    0.0f, 0.5f, 0.5f, 0.0f
        //};

        //// 3 vertices, 1 instance, 0 first vertex index, 0 first instance index
        //uint[] indirectArgs = new uint[] { 3, 1, 0, 0 };

        //byte[] drawVerticesBytes = new byte[drawVertices.Length * sizeof(float)];
        //byte[] indirectArgsBytes = new byte[sizeof(uint) * 4];

        //Buffer.BlockCopy(drawVertices, 0, drawVerticesBytes, 0, drawVerticesBytes.Length);
        //Buffer.BlockCopy(indirectArgs, 0, indirectArgsBytes, 0, indirectArgsBytes.Length);

        //VertexBuffer = Rd.StorageBufferCreate((uint)drawVerticesBytes.Length, drawVerticesBytes);
        //IndirectArgsBuffer = Rd.StorageBufferCreate((uint)indirectArgsBytes.Length, indirectArgsBytes, usage: RenderingDevice.StorageBufferUsage.Indirect);

        //VertexUniform = new RDUniform()
        //{
        //    UniformType = RenderingDevice.UniformType.StorageBuffer,
        //    Binding = 0
        //};
        //VertexUniform.AddId(VertexBuffer);

        //RDUniform indirectDrawUniform = new RDUniform()
        //{
        //    UniformType = RenderingDevice.UniformType.StorageBuffer,
        //    Binding = 0
        //};
        //indirectDrawUniform.AddId(IndirectArgsBuffer);

        //VertexBufferUniformSet = Rd.UniformSetCreate([VertexUniform], IndirectDrawShader, 0);
    }
}
