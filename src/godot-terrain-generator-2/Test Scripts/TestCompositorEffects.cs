using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Numerics;
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

    // Test render scene data
    public RDUniform RenderSceneDataUniform;
    public Rid RenderSceneDataUniformSet;


    public TestCompositorEffects() : base()
    {
        EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
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
        if (effectCallbackType != (int)EffectCallbackTypeEnum.PostTransparent)
        {
            return;
        }

        using RenderSceneBuffersRD renderSceneBuffers = renderData.GetRenderSceneBuffers() as RenderSceneBuffersRD;
        //GD.Print(renderData.GetRenderSceneData().GetCamProjection());

        Rid renderSceneDataBuffer = renderData.GetRenderSceneData().GetUniformBuffer();
        
        if (renderSceneBuffers != null)
        {
            var size = renderSceneBuffers.GetInternalSize();
            if (size.X == 0 && size.Y == 0)
            {
                return;
            }

            if (Rd == null)
            {
                SetRenderPipeline(renderSceneBuffers, renderSceneDataBuffer, renderData);

                GD.Print(renderData.GetRenderSceneData().GetCamProjection());
                TestPrintOutCameraUniform(renderSceneDataBuffer);
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

            if (VertexBufferUniformSet.IsValid)
            {
                long drawList = Rd.DrawListBegin(ScreenBuffer, RenderingDevice.DrawFlags.IgnoreAll, ClearColors);
                Rd.DrawCommandBeginLabel("Test Indirect Draw", new Color(0.0f, 0.0f, 0.0f, 0.0f));
                Rd.DrawListBindVertexArray(drawList, EmptyVertexArray);
                Rd.DrawListBindRenderPipeline(drawList, IndirectDrawPipeline);
                Rd.DrawListBindUniformSet(drawList, RenderSceneDataUniformSet, 0);
                Rd.DrawListBindUniformSet(drawList, VertexBufferUniformSet, 1);
                Rd.DrawListDrawIndirect(drawList, false, IndirectArgsBuffer);
                Rd.DrawListEnd();
                Rd.DrawCommandEndLabel();
            }
        }
    }



    private void SetRenderPipeline(RenderSceneBuffersRD renderSceneBuffers, Rid sceneDataUniformBuffer, RenderData renderData)
    {
        Rd = RenderingServer.GetRenderingDevice();

        RDShaderFile shaderFile = GD.Load<RDShaderFile>("res://Shaders/Graphic/test_compositor_effect.glsl");
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
            CullMode = RenderingDevice.PolygonCullMode.Front,
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
            DepthCompareOperator = RenderingDevice.CompareOperator.LessOrEqual,
            FrontOpCompare = RenderingDevice.CompareOperator.LessOrEqual,
            FrontOpDepthFail = RenderingDevice.StencilOperation.Replace,
            FrontOpPass = RenderingDevice.StencilOperation.Keep,
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

        ClearColors = new Color[] { new Color(0.0f, 0.0f, 0.0f, 0.0f) };

        // Set Vertex Data
        float[] drawVertices = new float[]
        {
            0.0f, -10.5f, 10.5f, 0.0f,
            0.0f, 0.0f, 0.0f, 0.0f,
            10.5f, 10.5f, 10.5f, 0.0f,
            0.7f, 0.4f, 0.8f, 0.0f,
            -10.5f, 10.5f, 10.5f, 0.0f,
            0.00f, 0.0f, 0.2f, 0.0f,
        };

        // 3 vertices, 1 instance, 0 first vertex index, 0 first instance index
        uint[] indirectArgs = new uint[] { 3, 1, 0, 0 };

        byte[] drawVerticesBytes = new byte[drawVertices.Length * sizeof(float)];
        byte[] indirectArgsBytes = new byte[sizeof(uint) * 4];

        Buffer.BlockCopy(drawVertices, 0, drawVerticesBytes, 0, drawVerticesBytes.Length);
        Buffer.BlockCopy(indirectArgs, 0, indirectArgsBytes, 0, indirectArgsBytes.Length);

        VertexBuffer = Rd.StorageBufferCreate((uint)drawVerticesBytes.Length, drawVerticesBytes);
        IndirectArgsBuffer = Rd.StorageBufferCreate((uint)indirectArgsBytes.Length, indirectArgsBytes, usage: RenderingDevice.StorageBufferUsage.Indirect);

        VertexUniform = new RDUniform()
        {
           UniformType = RenderingDevice.UniformType.StorageBuffer,
           Binding = 0
        };
        VertexUniform.AddId(VertexBuffer);

        RDUniform indirectDrawUniform = new RDUniform()
        {
           UniformType = RenderingDevice.UniformType.StorageBuffer,
           Binding = 0
        };
        indirectDrawUniform.AddId(IndirectArgsBuffer);

        VertexBufferUniformSet = Rd.UniformSetCreate([VertexUniform], IndirectDrawShader, 1);



        // Set camera projection
        //Transform3D cameraTransform = renderData.GetRenderSceneData().GetCamTransform();
        ////Projection viewProjection = renderData.GetRenderSceneData().GetCamProjection();

        //float[] cameraMatrix = new float[]
        //{
        //    cameraTransform.Basis.X.X,
        //    cameraTransform.Basis.X.Y,
        //    cameraTransform.Basis.X.Z,
        //    0.0f,
        //    cameraTransform.Basis.Y.X,
        //    cameraTransform.Basis.Y.Y,
        //    cameraTransform.Basis.Y.Z,
        //    0.0f,
        //    cameraTransform.Basis.Z.X,
        //    cameraTransform.Basis.Z.Y,
        //    cameraTransform.Basis.Z.Z,
        //    0.0f,
        //    cameraTransform.Origin.X,
        //    cameraTransform.Origin.Y,
        //    cameraTransform.Origin.Z,
        //    1.0f,
        //};

        //Matrix4x4 cameraMatrix1 = new Matrix4x4()
        //{
        //    M11 = cameraTransform.Basis.X.X,
        //    M12 = cameraTransform.Basis.X.Y,
        //    M13 = cameraTransform.Basis.X.Z,
        //    M14 = 0.0f,
        //    M21 = cameraTransform.Basis.Y.X,
        //    M22 = cameraTransform.Basis.Y.Y,
        //    M23 = cameraTransform.Basis.Y.Z,
        //    M24 = 0.0f,
        //    M31 = cameraTransform.Basis.Z.X,
        //    M32 = cameraTransform.Basis.Z.Y,
        //    M33 = cameraTransform.Basis.Z.Z,
        //    M34 = 0.0f,
        //    M41 = cameraTransform.Origin.X,
        //    M42 = cameraTransform.Origin.Y,
        //    M43 = cameraTransform.Origin.Z,
        //    M44 = 1.0f,
        //};

        //Matrix4x4 cameraMatrixInverse;
        //Matrix4x4.Invert(cameraMatrix1, out cameraMatrixInverse);

        //GD.Print(cameraMatrixInverse);

        //float[] viewProjectionMatrix = new float[]
        //{
        //    viewProjection.X.X,
        //    viewProjection.X.Y,
        //    viewProjection.X.Z,
        //    viewProjection.X.W,
        //    viewProjection.Y.X,
        //    viewProjection.Y.Y,
        //    viewProjection.Y.Z,
        //    viewProjection.Y.W,
        //    viewProjection.Z.X,
        //    viewProjection.Z.Y,
        //    viewProjection.Z.Z,
        //    viewProjection.Z.W,
        //    viewProjection.W.X,
        //    viewProjection.W.Y,
        //    viewProjection.W.Z,
        //    viewProjection.W.W,
        //};

        //GD.Print("Output: ", string.Join(", ", cameraMatrix.Select(vec => $"{vec}\n").ToArray()));






        //byte[] sceneDataUniformBytes = new byte[(16 + 16) * sizeof(float)];
        //Buffer.BlockCopy(cameraMatrix, 0, sceneDataUniformBytes, 0, cameraMatrix.Length * sizeof(float));
        //Buffer.BlockCopy(viewProjectionMatrix, 0, sceneDataUniformBytes, cameraMatrix.Length * sizeof(float), viewProjectionMatrix.Length * sizeof(float));

        //Rid sceneDataUniformBuffer = Rd.UniformBufferCreate((uint)sceneDataUniformBytes.Length, sceneDataUniformBytes);

        // Set camera projection
        RenderSceneDataUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0,
        };
        RenderSceneDataUniform.AddId(sceneDataUniformBuffer);
        RenderSceneDataUniformSet = Rd.UniformSetCreate([RenderSceneDataUniform], IndirectDrawShader, 0);
    }

    static int counter = 0;

    private void TestPrintOutCameraUniform(Rid renderSceneData)
    {
        if (counter++ < 1)
        {
            byte[] cameraUniformOut = Rd.BufferGetData(renderSceneData);
            float[] cameraProjectionMatrix = new float[cameraUniformOut.Length / sizeof(float)];
            Buffer.BlockCopy(cameraUniformOut, 0, cameraProjectionMatrix, 0, cameraProjectionMatrix.Length * sizeof(float));
            GD.Print("Output: ", string.Join(", ", cameraProjectionMatrix.Select(vec => $"{vec}").ToArray()));
        }
    }
}
