using Godot;
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
		SDFPipeline pipeline =  sdfPipelineParser.ParseFromFile(SDFPipelinePath, rd);

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
		TransvoxelTerrainGenerator transvoxelTerrainGenerator = new TransvoxelTerrainGenerator(rd, transvoxelTerrainGeneratorDescriptor);

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
		Rid terrainMeshConstantsBuffer = rd.UniformBufferCreate((uint)terrainMeshConstantsBytes.Length, terrainMeshConstantsBytes);

		TestVoxelOctreeRenderer compEffect = Camera.Compositor.CompositorEffects[0] as TestVoxelOctreeRenderer;
		compEffect.Camera = Camera;
		compEffect.VoxelOctree = VoxelOctree;
		compEffect.TerrainMeshConstantsBuffer = terrainMeshConstantsBuffer;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		VoxelOctree.UpdateAbstractTree(Camera.Position);
		VoxelOctree.ProcessEventQueue();
	}
}
