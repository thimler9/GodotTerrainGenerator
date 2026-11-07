using Godot;
using System;
using GodotTerrainGenerator2.Test_Scripts;
using TerrainGeneration.Application.SDFGenerator.Abstractions;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Application.SDFGenerator.Pipeline;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;
using TerrainGeneration.Application.TerrainGenerator;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;
using TerrainGeneration.Application.VoxelOctree;
using TerrainGeneration.Application.VoxelOctree.AbstractOctree;
using TerrainGeneration.Utilities.Struct;

public partial class TestVoxelOctree : Node
{
	[Export]
	public Camera3D Camera;

	// Tree params
	[Export]
	public Vector3 Center;
	[Export]
	public uint StartSize;
	[Export]
	public uint MinChunkSize;
	public TerrainLod[] TerrainLods;
	[Export]
	public float BorderWidth;
    [Export]
	public Vector3 SunDirection = new(0.0f, -1.0f, 0.0f);
	[Export]
	public float SunIntensity = 1.0f;
	[Export]
	public Color SunColor = Colors.White;
	[Export]
	public float SpecularIntensity = 0.5f;
	[Export]
	public float PlayerPositionChangeThreshold;
	[Export]
	public uint EventQueueWorkBudget;

	// Transvoxel Params
	public string NormalsShaderPath = "res://Shaders/Compute/normal_generator.glsl";
	public string TransvoxelShaderPath = "res://Shaders/Compute/mesh_generator.glsl";
	public string IndirectArgsShaderPath = "res://Shaders/Compute/indirect_args.glsl";
	[Export]
	public uint MaxNumVertices;
	[Export]
	public uint MaxNumTerrainMeshesInQueue;

	// Simplex Noise Params
	public string SimplexNoiseShaderPath = "res://Shaders/Compute/map_generator.glsl";
	public string SDFPipelinePath = "res://Data/TerrainPipeline.json";

	public VoxelOctree VoxelOctree;
	private RenderingDevice Rd;
	private Rid DirectionalLightBuffer;
	private Vector3 UploadedSunDirection;
	private float UploadedSunIntensity;
	private Color UploadedSunColor;
	private float UploadedSpecularIntensity;
	private bool DirectionalLightValuesUploaded;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Rd = RenderingServer.GetRenderingDevice();
		DirectionalLightBuffer = Rd.UniformBufferCreate(48, CreateDirectionalLightData());

		TerrainLods = new TerrainLod[6]
		{
			new TerrainLod()
			{
				LodDivider = 128,
				LodDistanceCutoff = 30000.0f
			},
			new TerrainLod()
			{
				LodDivider = 64,
				LodDistanceCutoff = 20000.0f
			},
			new TerrainLod()
			{
				LodDivider = 32,
				LodDistanceCutoff = 5000.0f
			},
			new TerrainLod()
			{
				LodDivider = 16,
				LodDistanceCutoff = 1200.0f
			},
			new TerrainLod()
			{
				LodDivider = 8,
				LodDistanceCutoff = 750.0f
			},
			new TerrainLod()
			{
				LodDivider = 4,
				LodDistanceCutoff = 350.0f
			}
		};

      SDFPipelineParser sdfPipelineParser = new SDFPipelineParser();
		SDFPipeline pipeline =  sdfPipelineParser.ParseFromFile(SDFPipelinePath, Rd);

		TransvoxelTerrainGeneratorDescriptor transvoxelTerrainGeneratorDescriptor = new TransvoxelTerrainGeneratorDescriptor()
		{
			SDFPipeline = pipeline,
            ChunkOffset = Center,
			Lod = TerrainLods[0].LodDivider,
			ChunkSize = StartSize,
			IndirectArgsShaderPath = IndirectArgsShaderPath,
			MaxNumTerrainMeshesInQueue = MaxNumTerrainMeshesInQueue,
			MaxNumVertices = MaxNumVertices,
			NormalsShaderPath = NormalsShaderPath,
			BorderWidth = BorderWidth,
			TransvoxelShaderPath = TransvoxelShaderPath,
		};
       TransvoxelTerrainGenerator transvoxelTerrainGenerator = new TransvoxelTerrainGenerator(Rd, transvoxelTerrainGeneratorDescriptor);

		VoxelOctreeDescriptor voxelOctreeDescriptor = new VoxelOctreeDescriptor()
		{
			BorderWidth = BorderWidth,
			Center = Center,
			EventQueueWorkBudget = EventQueueWorkBudget,
			TransvoxelTerrainGeneratorDescriptor = transvoxelTerrainGeneratorDescriptor,
			MinChunkSize = MinChunkSize,
			PlayerPosition = Camera.Position,
			PlayerPositionChangeThreshold = PlayerPositionChangeThreshold,
			Size = StartSize,
			TerrainLods = TerrainLods,
		};

		VoxelOctree = new VoxelOctree(voxelOctreeDescriptor);


		TerrainMeshShaderConstants terrainMeshConstants = new TerrainMeshShaderConstants()
		{
			BorderWidth = BorderWidth
		};
		byte[] terrainMeshConstantsBytes = StructHelpers.ToByteArray(terrainMeshConstants);
		Rid terrainMeshConstantsBuffer = Rd.UniformBufferCreate((uint)terrainMeshConstantsBytes.Length, terrainMeshConstantsBytes);

		TestVoxelOctreeRenderer compEffect = Camera.Compositor.CompositorEffects[0] as TestVoxelOctreeRenderer;
		compEffect.Camera = Camera;
		compEffect.VoxelOctree = VoxelOctree;
		compEffect.TerrainMeshConstantsBuffer = terrainMeshConstantsBuffer;
		compEffect.DirectionalLightBuffer = DirectionalLightBuffer;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		VoxelOctree.UpdateAbstractTree(Camera.Position);
		VoxelOctree.ProcessEventQueue();

		UpdateDirectionalLightBuffer();
	}

	private void UpdateDirectionalLightBuffer()
	{
		if (DirectionalLightValuesUploaded &&
			UploadedSunDirection == SunDirection &&
			UploadedSunIntensity == SunIntensity &&
			UploadedSunColor == SunColor &&
			UploadedSpecularIntensity == SpecularIntensity)
		{
			return;
		}

		Rd.BufferUpdate(DirectionalLightBuffer, 0, 48, CreateDirectionalLightData());
		UploadedSunDirection = SunDirection;
		UploadedSunIntensity = SunIntensity;
		UploadedSunColor = SunColor;
		UploadedSpecularIntensity = SpecularIntensity;
		DirectionalLightValuesUploaded = true;
	}

	private byte[] CreateDirectionalLightData()
	{
		Vector3 direction = SunDirection.LengthSquared() > 0.0f
			? SunDirection
			: new Vector3(0.0f, -1.0f, 0.0f);

		float[] directionalLightData = [
			direction.X, direction.Y, direction.Z, SunIntensity,
			SunColor.R, SunColor.G, SunColor.B, 0.0f,
			SpecularIntensity, 0.0f, 0.0f, 0.0f
		];

		byte[] directionalLightBytes = new byte[directionalLightData.Length * sizeof(float)];
		Buffer.BlockCopy(directionalLightData, 0, directionalLightBytes, 0, directionalLightBytes.Length);
		return directionalLightBytes;
	}
}
