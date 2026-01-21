using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;
using TerrainGeneration.Application.TerrainGenerator;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel.NormalsShader;

namespace GodotTerrainGenerator2.Test_Scripts;

[Tool]
[GlobalClass]
public partial class TestTerrainMeshRender : CompositorEffect
{
    public uint ChunkSize = 32;
    public uint Lod = 1;

    // Map Params
    public Vector3 ChunkOffset = new Vector3(0, 0, 0);
    public uint Seed = 1234;
    public float Scale = 32.0f;
    public float Strength = 350.0f;
    public uint NumOctaves = 8;
    public float Frequency = 1.0f;
    public float Amplitude = 1.0f;
    public float Lacunarity = 2.0f;
    public float Gain = 0.4f;

    // Transvoxel Shader Params
    public uint MaxNumOfVertices = 200000;
    public float TransitionWidth = 1.0f;

    // Transvoxel Params
    public uint MaxNumTerrainMeshesInQueue = 10;

    // Rendering ----------------------------------------------

    // Overall Rendering Objects
    public RenderingDevice Rd;
    public Rid TerrainShader;
    public Rid RenderPipeline;

    // Indirect Draw Data
    public TerrainMesh TerrainMesh = null;

    // Per frame rendering data
    public Rid EmptyVertexArray;
    public Color[] ClearColors = [new Color(0.0f, 0.0f, 0.0f, 0.0f)];
    public Rid ColorTexture;
    public Rid DepthTexture;
    public Rid ScreenBuffer;
    public RDUniform RenderSceneDataUniform;
    public Rid RenderSceneDataUniformSet;

    public TestTerrainMeshRender() : base()
    {
        EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
    }

    public void Init(RenderSceneBuffersRD renderSceneBuffers, Rid renderSceneDataBuffer)
    {
        InitializeMesh();
        if (TerrainMesh == null)
        {
            GD.PrintErr("Terrain Mesh is null, cannot initialize rendering.");
            return;
        }

        SetRenderPipeline(renderSceneBuffers, TerrainMesh, renderSceneDataBuffer);
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

            if (TerrainMesh != null)
            {
                long drawList = Rd.DrawListBegin(ScreenBuffer, RenderingDevice.DrawFlags.IgnoreColorAll, ClearColors);
                Rd.DrawCommandBeginLabel("Draw Terrain", new Color(0.0f, 0.0f, 0.0f, 0.0f));
                Rd.DrawListBindRenderPipeline(drawList, RenderPipeline);
                Rd.DrawListBindVertexArray(drawList, EmptyVertexArray);
                Rd.DrawListBindUniformSet(drawList, RenderSceneDataUniformSet, 0);
                Rd.DrawListBindUniformSet(drawList, TerrainMesh.VertexBufferUniformSet, 1);
                Rd.DrawListDrawIndirect(drawList, false, TerrainMesh.IndirectArgsBuffer);
                Rd.DrawListEnd();
                Rd.DrawCommandEndLabel();
            }

        }
    }

    public void InitializeMesh()
    {
        Rd = RenderingServer.GetRenderingDevice();

        SDFShaderParameters sdfShaderParameters = new SDFShaderParameters(ChunkSize, Lod);
        SimplexNoiseShaderParameters simplexNoiseShaderParameters = new SimplexNoiseShaderParameters(
            ChunkOffset,
            Seed,
            Scale,
            Strength,
            NumOctaves,
            Frequency,
            Amplitude,
            Lacunarity,
            Gain
        );
        SimplexNoiseShaderDescriptor simplexNoiseShaderDescriptor = new SimplexNoiseShaderDescriptor()
        {
            ShaderPath = "res://Shaders/Compute/simplex_noise.glsl",
            Parameters = simplexNoiseShaderParameters,
        };

        SDFGeneratorSettings sdfGeneratorSettings = new SDFGeneratorSettings()
        {
            ChunkSize = ChunkSize,
            SDFShaderParameters = sdfShaderParameters,
            SimplexNoiseShaderDescriptor = simplexNoiseShaderDescriptor
        };
        SDFGenerator sdfGenerator = new SDFGenerator(Rd, sdfGeneratorSettings);

        sdfGenerator.DispatchShaders(sdfShaderParameters);

        // ---- Get normals
        RDUniform sdfBufferUniform = sdfGenerator.OutputBufferUniform;

        NormalsShaderParameters normalsParaters = new NormalsShaderParameters()
        {
            ChunkSize = ChunkSize,
            Lod = Lod,
        };
        NormalsShaderDescriptor normalsDescriptor = new NormalsShaderDescriptor()
        {
            Parameters = normalsParaters,
            ShaderPath = "res://Shaders/Compute/normal_generator.glsl"
        };

        NormalsShader normalsShader = new NormalsShader(Rd, normalsDescriptor);
        normalsShader.Dispatch(normalsParaters, sdfBufferUniform);

        // --------------------------------------------------------------------------------------------------------------------
        RDUniform normalsBufferUniform = normalsShader.OutputNormalsUniform;

        TransvoxelShaderParameters transvoxelShaderParameters = new TransvoxelShaderParameters()
        {
            ChunkOffset = ChunkOffset,
            Lod = Lod,
            ChunkSize = ChunkSize,
            MaxNumVertices = MaxNumOfVertices,
            TransitionWidth = TransitionWidth,
        };

        TransvoxelShaderDescriptor transvoxelShaderDescriptor = new TransvoxelShaderDescriptor()
        {
            Parameters = transvoxelShaderParameters,
            ShaderPath = "res://Shaders/Compute/mesh_generator.glsl"
        };

        IndirectArgsShaderDescriptor indirectArgsShaderDescriptor = new IndirectArgsShaderDescriptor()
        {
            ShaderPath = "res://Shaders/Compute/indirect_args.glsl"
        };

        TransvoxelDescriptor transvoxelDescriptor = new TransvoxelDescriptor()
        {
            TransvoxelShaderDescriptor = transvoxelShaderDescriptor,
            IndirectArgsShaderDescriptor = indirectArgsShaderDescriptor,
            MaxNumTerrainMeshesInQueue = MaxNumTerrainMeshesInQueue,
        };

        Transvoxel transvoxel = new Transvoxel(Rd, transvoxelDescriptor);

        TerrainMesh = transvoxel.GetTerrainMesh(transvoxelShaderParameters, sdfBufferUniform, normalsBufferUniform);
    }

    private void SetRenderPipeline(RenderSceneBuffersRD renderSceneBuffers, TerrainMesh terrainMesh, Rid renderSceneDataBuffer)
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
        TerrainMesh.VertexBufferUniformSet = Rd.UniformSetCreate([TerrainMesh.VertexBufferUniform], TerrainShader, 1);

        // Set camera projection
        RenderSceneDataUniform = new RDUniform()
        {
            UniformType = RenderingDevice.UniformType.UniformBuffer,
            Binding = 0,
        };
        RenderSceneDataUniform.AddId(renderSceneDataBuffer);
        RenderSceneDataUniformSet = Rd.UniformSetCreate([RenderSceneDataUniform], TerrainShader, 0);
    }


    private void TestPrintOutCameraUniform(Rid renderSceneData)
    {
        byte[] cameraUniformOut = Rd.BufferGetData(renderSceneData);
        float[] cameraProjectionMatrix = new float[16 + 16 + 12 + 12];
        Buffer.BlockCopy(cameraUniformOut, 0, cameraProjectionMatrix, 0, cameraProjectionMatrix.Length * sizeof(float));

        GD.Print("Projection Matrix:");
        for (int i = 0; i < 4; i++)
        {
            GD.Print($"{cameraProjectionMatrix[i * 4]} {cameraProjectionMatrix[i * 4 + 1]} {cameraProjectionMatrix[i * 4 + 2]} {cameraProjectionMatrix[i * 4 + 3]}");
        }

        GD.Print("\nInverse Projection:");
        for (int i = 4; i < 8; i++)
        {
            GD.Print($"{cameraProjectionMatrix[i * 4]} {cameraProjectionMatrix[i * 4 + 1]} {cameraProjectionMatrix[i * 4 + 2]} {cameraProjectionMatrix[i * 4 + 3]}");
        }

        GD.Print("\nInverse View Matrix:");
        for (int i = 8; i < 12; i++)
        {
            GD.Print($"{cameraProjectionMatrix[i * 3]} {cameraProjectionMatrix[i * 3 + 1]} {cameraProjectionMatrix[i * 3 + 2]}");
        }

        GD.Print("\nView Matrix:");
        for (int i = 12; i < 16; i++)
        {
            GD.Print($"{cameraProjectionMatrix[i * 3]} {cameraProjectionMatrix[i * 3 + 1]} {cameraProjectionMatrix[i * 3 + 2]}");
        }

        GD.Print("\n");
    }
}
