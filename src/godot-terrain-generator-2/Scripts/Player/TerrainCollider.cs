using Godot;
using System;
using System.Linq;
using TerrainGeneration.Application.SDFGenerator.Pipeline;
using TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;
using TerrainGeneration.Application.SDFGenerator.Pipeline.GPU;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel.CPU;
using TerrainGeneration.Application.VoxelOctree.AbstractOctree;

public partial class TerrainCollider : Node
{
	[Export]
	public TestVoxelOctree VoxelOctree;

	[Export]
	public RigidBody3D PlayerPosition;

	/// <summary>
	/// The dimension of the collider in one direction
	/// </summary>
	[Export]
	public uint ColliderSize;

	private Vector3 OldPlayerPosition;
	private float DistanceForRecalculation;

	// We assume that the last element in the Voxel Octree's Terrain Lods is the Lod of where the player is
	private TerrainLod TerrainLodAtPlayer;

	private Transvoxel CPUMarchingCubes = new Transvoxel();
	private TransvoxelParameters TransvoxelParameters;
	private SDFPipelineCPU SDFPipelineCPU;

    // Collider Data
    private StaticBody3D CollisionBody;
    private CollisionShape3D CollisionShape;
	private ConcavePolygonShape3D CollisionShapeGeometry;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		if (VoxelOctree == null)
		{
			throw new ArgumentNullException($"{nameof(VoxelOctree)} cannot be null.");
		}

		if (VoxelOctree.TerrainLods == null)
		{
			throw new ArgumentNullException($"{nameof(VoxelOctree.TerrainLods)} cannot be null");
		}

		if (PlayerPosition == null)
		{
			throw new ArgumentNullException($"{nameof(PlayerPosition)} cannot be null.");
		}

		TerrainLodAtPlayer = VoxelOctree.TerrainLods.Last();
		DistanceForRecalculation = ColliderSize / 4.0f;
		OldPlayerPosition = PlayerPosition.Position;

		// We make a new pipeline because the chunkSize / Lod ratio is prob different than the voxel octree
        SDFPipelineParser sdfPipelineParser = new SDFPipelineParser();
        SDFPipeline sdfPipeline = sdfPipelineParser.ParseFromFile(VoxelOctree.SDFPipelinePath, RenderingServer.GetRenderingDevice());
		if (sdfPipeline == null)
		{
			throw new ArgumentException($"{nameof(sdfPipeline)} cannot be null.");
		}

        SDFPipelineCPU = sdfPipeline.CPUSDFPipeline;
		if (SDFPipelineCPU == null)
		{
			throw new ArgumentNullException($"{nameof(SDFPipelineCPU)} cannot be null.");
		}

        TransvoxelParameters = new TransvoxelParameters()
        {
            ChunkSize = ColliderSize,
            Lod = TerrainLodAtPlayer.LodDivider,
        };

		SetCollider();
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Update Collider after distance change
		if (OldPlayerPosition.DistanceSquaredTo(PlayerPosition.Position) > DistanceForRecalculation * DistanceForRecalculation)
		{
			SetCollider();
			OldPlayerPosition = PlayerPosition.Position;
		}
	}

	private void SetCollider()
	{
        Vector3 colliderOffset = OldPlayerPosition - Vector3.One * ColliderSize / 2;
        colliderOffset = (colliderOffset / TerrainLodAtPlayer.LodDivider).Floor() * TerrainLodAtPlayer.LodDivider; // Snaps the collider to the terrain gridfloat[] 

        float[] sdf = SDFPipelineCPU.GetSDF(colliderOffset, ColliderSize, TerrainLodAtPlayer.LodDivider);
        Vector3[] vertices = CPUMarchingCubes.GenerateMarchingCubes(TransvoxelParameters, sdf, colliderOffset);

		if (CollisionShapeGeometry == null)
		{
			CollisionShapeGeometry = new ConcavePolygonShape3D();
		}

		if (CollisionShape == null)
		{
            CollisionShape = new CollisionShape3D();
			CollisionShape.Shape = CollisionShapeGeometry;
		}

		if (CollisionBody == null)
		{
			CollisionBody = new StaticBody3D();
			CollisionBody.AddChild(CollisionShape);
			AddChild(CollisionBody);
		}

		((ConcavePolygonShape3D)CollisionShape.Shape).SetFaces(vertices);
    }
}
