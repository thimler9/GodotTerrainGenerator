using Godot;
using System;
using TerrainGeneration.Application.SDFGenerator.Pipeline;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;
using TerrainGeneration.Application.VoxelOctree;
using TerrainGeneration.Application.VoxelOctree.AbstractOctree;

public partial class RemakeOctree : Button
{
    public override void _Ready()
    {
        Pressed += RemakeOctreeAction;
    }

    public void RemakeOctreeAction()
    {
        // See if there is a VoxelOctree node in the scene and gets reference to it
        var testVoxelOctreeNode = GetTree().CurrentScene.GetNodeOrNull<TestVoxelOctree>("VoxelOctree");
        if (testVoxelOctreeNode != null)
        {
            TerrainLod[] terrainLods = new TerrainLod[6] {
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
            SDFPipeline pipeline = sdfPipelineParser.ParseFromFile(testVoxelOctreeNode.SDFPipelinePath, RenderingServer.GetRenderingDevice());

            Vector3 newPlayerPosition = testVoxelOctreeNode.Camera.Position;

            Vector3 worldCenter = new Vector3(MathF.Round(newPlayerPosition.X / (testVoxelOctreeNode.StartSize / 4), MidpointRounding.ToPositiveInfinity) * (testVoxelOctreeNode.StartSize / 4),
                MathF.Round(newPlayerPosition.Y / (testVoxelOctreeNode.StartSize / 4), MidpointRounding.ToPositiveInfinity) * (testVoxelOctreeNode.StartSize / 4), MathF.Round(newPlayerPosition.Z / (testVoxelOctreeNode.StartSize / 4), MidpointRounding.ToPositiveInfinity) * (testVoxelOctreeNode.StartSize / 4));


            TransvoxelTerrainGeneratorDescriptor transvoxelTerrainGeneratorDescriptor = new TransvoxelTerrainGeneratorDescriptor()
            {
                SDFPipeline = pipeline,
                ChunkOffset = worldCenter,
                Lod = testVoxelOctreeNode.TerrainLods[0].LodDivider,
                ChunkSize = testVoxelOctreeNode.StartSize,
                IndirectArgsShaderPath = testVoxelOctreeNode.IndirectArgsShaderPath,
                MaxNumTerrainMeshesInQueue = testVoxelOctreeNode.MaxNumTerrainMeshesInQueue,
                MaxNumVertices = testVoxelOctreeNode.MaxNumVertices,
                NormalsShaderPath = testVoxelOctreeNode.NormalsShaderPath,
                BorderWidth = testVoxelOctreeNode.BorderWidth,
                TransvoxelShaderPath = testVoxelOctreeNode.TransvoxelShaderPath,
            };
            TransvoxelTerrainGenerator transvoxelTerrainGenerator = new TransvoxelTerrainGenerator(RenderingServer.GetRenderingDevice(), transvoxelTerrainGeneratorDescriptor);

            VoxelOctreeDescriptor voxelOctreeDescriptor = new VoxelOctreeDescriptor()
            {
                BorderWidth = testVoxelOctreeNode.BorderWidth,
                Center = worldCenter,
                EventQueueWorkBudget = testVoxelOctreeNode.EventQueueWorkBudget,
                TransvoxelTerrainGeneratorDescriptor = transvoxelTerrainGeneratorDescriptor,
                MinChunkSize = testVoxelOctreeNode.MinChunkSize,
                PlayerPosition = testVoxelOctreeNode.Camera.Position,
                PlayerPositionChangeThreshold = testVoxelOctreeNode.PlayerPositionChangeThreshold,
                Size = testVoxelOctreeNode.StartSize,
                TerrainLods = testVoxelOctreeNode.TerrainLods,
            };


            testVoxelOctreeNode.VoxelOctree.RemakeOctree(voxelOctreeDescriptor);
        }
    }
}
