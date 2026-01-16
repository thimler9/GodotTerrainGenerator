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
public partial class TestTerrainMeshRender : CompositorEffect
{
    public uint ChunkSize = 8;
    public uint Lod = 1;

    // Map Params
    public Vector3 ChunkOffset = new Vector3(128, 128, 128);
    public uint Seed = 1;
    public float Scale = 1.0f;
    public float Strength = 1.0f;
    public uint NumOctaves = 1;
    public float Frequency = 1.0f;
    public float Amplitude = 1.0f;
    public float Lacunarity = 1.0f;
    public float Gain = 1.0f;

    // Transvoxel Shader Params
    public uint MaxNumOfVertices = 10000;
    public float TransitionWidth = 1.0f;

    // Transvoxel Params
    public uint MaxNumTerrainMeshesInQueue = 10;

    public RenderingDevice Rd;
    public Rid TerrainShader;
    public TerrainMesh TerrainMesh = null;
    public TerrainMeshDrawDescriptor terrainMeshDrawDescriptor = null;
    public Rid CurrentImageTexture;
    public Rid CurrentDepthTexture;
    public Rid ScreenBuffer;
    public Color[] ClearColors = [new Color(0.2f, 0.2f, 0.2f, 1.0f)];
    public Rid RenderPipeline;
    public Rid VertexArray;

    public void InitializeRendering(RenderSceneBuffersRD renderSceneBuffers)
    {
        Rd = RenderingServer.CreateLocalRenderingDevice();

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
        //normalsShader.PrintOutBuffer();

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

        SetRenderPipeline(Rd, "res://Shaders/Graphic/terrain_shader.glsl", renderSceneBuffers);
    }

    public override void _RenderCallback(int effectCallbackType, RenderData renderData)
    {
        if (effectCallbackType == (int)EffectCallbackTypeEnum.PostTransparent)
        {
            RenderSceneBuffersRD renderSceneBuffers = (RenderSceneBuffersRD)renderData.GetRenderSceneBuffers();

            if (renderSceneBuffers != null)
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
                    InitializeRendering(renderSceneBuffers);
                }
                // Update camera buffers
                else
                {
                    var newImageTexture = renderSceneBuffers.GetColorTexture();
                    var newDepthTexture = renderSceneBuffers.GetDepthTexture();

                    if (newImageTexture != CurrentImageTexture || newDepthTexture != CurrentDepthTexture)
                    {
                        CurrentImageTexture = newImageTexture;
                        CurrentDepthTexture = newDepthTexture;

                        if (Rd.FramebufferIsValid(ScreenBuffer))
                        {
                            Rd.FreeRid(ScreenBuffer);
                        }

                        ScreenBuffer = Rd.FramebufferCreate([newImageTexture, newDepthTexture]);
                    }
                }

                Rd.DrawCommandBeginLabel("Draw Terrain", new Color(1.0f, 1.0f, 1.0f, 1.0f));

                var drawList = Rd.DrawListBegin(ScreenBuffer, RenderingDevice.DrawFlags.ClearColor0, ClearColors);
                Rd.DrawListBindRenderPipeline(drawList, RenderPipeline);
                Rd.DrawListBindVertexArray(drawList, VertexArray);

                Rid vertexUniformSet = Rd.UniformSetCreate([TerrainMesh.VertexBufferUniform], TerrainShader, 0);
                Rd.DrawListBindUniformSet(drawList, vertexUniformSet, 0);
                Rd.DrawListDrawIndirect(drawList, false, TerrainMesh.IndirectArgsBuffer, 0, 1, 4);

            }
        }
    }

    private void SetRenderPipeline(RenderingDevice rd, string? shaderPath, RenderSceneBuffersRD renderSceneBuffers)
    {
        RDShaderFile shaderFile = GD.Load<RDShaderFile>(shaderPath);
        RDShaderSpirV shaderBytecode = shaderFile.GetSpirV();
        Rid shader = rd.ShaderCreateFromSpirV(shaderBytecode);

        // Position descriptor for vertices
        RDVertexAttribute vertexAttributePosition = new RDVertexAttribute()
        {
            Format = RenderingDevice.DataFormat.R32G32Sfloat,
            Frequency = RenderingDevice.VertexFrequency.Vertex,
            Location = 0,
            Offset = 0,
            Stride = (uint)Marshal.SizeOf<Vector3>()
        };

        // Normal descriptor for vertices
        RDVertexAttribute vertexAttributeNormal = new RDVertexAttribute()
        {
            Format = RenderingDevice.DataFormat.R32G32Sfloat,
            Frequency = RenderingDevice.VertexFrequency.Vertex,
            Location = 1,
            Offset = 0,
            Stride = (uint)Marshal.SizeOf<Vector3>()
        };

        long vertexFormat = rd.VertexFormatCreate([vertexAttributePosition, vertexAttributeNormal]);

        // Vertex array can be empty since we're doing indirect drawing
        VertexArray = rd.VertexArrayCreate(1, vertexFormat, []);

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

        Rid imageTexture = renderSceneBuffers.GetColorTexture();
        Rid depthTexture = renderSceneBuffers.GetDepthTexture();
        Rid screenBuffer = Rd.FramebufferCreate([imageTexture, depthTexture]);

        long frameBufferFormat = Rd.FramebufferGetFormat(screenBuffer);

        RenderPipeline = Rd.RenderPipelineCreate(
            shader,
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

        Color[] clearColors = new Color[] { new Color(0.2f, 0.2f, 0.2f, 1.0f) };
    }

}
