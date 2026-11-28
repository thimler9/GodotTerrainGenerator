using Godot;
using GodotTerrainGenerator2.Test_Scripts;
using System;
using System.Linq;
using TerrainGeneration.Application.SDFGenerator.Pipeline;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;
using TerrainGeneration.Application.SDFGenerator.Pipeline.GPU;
using TerrainGeneration.Application.TerrainGenerator;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;
using TerrainGeneration.Application.VoxelOctree;
using TerrainGeneration.Application.VoxelOctree.AbstractOctree;
using TerrainGeneration.Utilities.Struct;

public partial class TestVoxelOctree : Node
{
	[Export]
	public RigidBody3D CameraRigidBody3D;
	private Camera3D Camera3D;

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
	public SDFPipeline SDFPipeline;

	public VoxelOctree VoxelOctree;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
      RenderingDevice rd = RenderingServer.GetRenderingDevice();

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
		SDFPipeline = sdfPipelineParser.ParseFromFile(SDFPipelinePath, rd);

		if (SDFPipeline == null)
		{
			throw new ArgumentNullException($"{nameof(SDFPipeline)} cannot be null.");
		}

		TransvoxelTerrainGeneratorDescriptor transvoxelTerrainGeneratorDescriptor = new TransvoxelTerrainGeneratorDescriptor()
		{
			SDFPipeline = SDFPipeline.GPUSDFPipeline,
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
		TransvoxelTerrainGenerator transvoxelTerrainGenerator = new TransvoxelTerrainGenerator(rd, transvoxelTerrainGeneratorDescriptor);

		VoxelOctreeDescriptor voxelOctreeDescriptor = new VoxelOctreeDescriptor()
		{
			BorderWidth = BorderWidth,
			Center = Center,
			EventQueueWorkBudget = EventQueueWorkBudget,
			TransvoxelTerrainGeneratorDescriptor = transvoxelTerrainGeneratorDescriptor,
			MinChunkSize = MinChunkSize,
			PlayerPosition = CameraRigidBody3D.Position,
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
		Rid terrainMeshConstantsBuffer = rd.UniformBufferCreate((uint)terrainMeshConstantsBytes.Length, terrainMeshConstantsBytes);

		Camera3D = CameraRigidBody3D.GetChildren().OfType<Camera3D>().FirstOrDefault();

		if (Camera3D == null)
		{
			throw new ArgumentNullException($"{Camera3D} cannot be null");
		}

		TestVoxelOctreeRenderer compEffect = Camera3D.Compositor.CompositorEffects[0] as TestVoxelOctreeRenderer;
		compEffect.Camera = Camera3D;
		compEffect.VoxelOctree = VoxelOctree;
		compEffect.TerrainMeshConstantsBuffer = terrainMeshConstantsBuffer;
        UpdateLighting(compEffect);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		VoxelOctree.UpdateAbstractTree(CameraRigidBody3D.Position);
		VoxelOctree.ProcessEventQueue();

		TestVoxelOctreeRenderer compEffect = Camera3D.Compositor.CompositorEffects[0] as TestVoxelOctreeRenderer;
		if (compEffect != null)
		{
			UpdateLighting(compEffect);
		}
	}

	private void UpdateLighting(TestVoxelOctreeRenderer compEffect)
	{
		compEffect.SunDirection = SunDirection;
		compEffect.SunIntensity = SunIntensity;
		compEffect.SunColor = SunColor;
		compEffect.SpecularIntensity = SpecularIntensity;
	}
}
