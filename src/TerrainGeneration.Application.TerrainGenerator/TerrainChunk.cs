using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.TerrainGenerator.Abstractions;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;

namespace TerrainGeneration.Application.TerrainGenerator
{
    public class TerrainChunk
    {
        //private TerrainSpawns TerrainSpawns;
        
        private TerrainMesh TerrainMesh;
        private TerrainMeshParameters TerrainMeshParameters;

        public TerrainChunk(TransvoxelTerrainGenerator transvoxelTerrainGenerator, TerrainChunkDescriptor descriptor)
        {
            transvoxelTerrainGenerator.SetSDFShaderParameters(new SDFGenerator.SDFShaderParameters(descriptor.ChunkOffset, descriptor.ChunkSize, descriptor.Lod));
            TerrainMesh = transvoxelTerrainGenerator.GetTerrainMesh();

            // Set the terrian mesh params
            TerrainMeshParameters = new TerrainMeshParameters()
            {
                ExpandBorders = descriptor.ExpandBorders,
                RetractBorders = descriptor.RetractBorders,
                ChunkOffset = new Vector4(descriptor.ChunkOffset.X, descriptor.ChunkOffset.Y, descriptor.ChunkOffset.Z, 0.0f),
                ChunkSize = descriptor.ChunkSize,
                BorderWidth = descriptor.BorderWidth
            };
            TerrainMesh.SetParamsBuffer(TerrainMeshParameters);
        }

        public void Dispose()
        {
            TerrainMesh.Dispose();
        }

        public void SetTerrainMeshBorders(uint retractBorders, uint expandBorders)
        {
            SetTerrainMeshParamsBuffer(new TerrainMeshParameters()
            {
                ExpandBorders = expandBorders,
                RetractBorders = retractBorders,
                ChunkOffset = TerrainMeshParameters.ChunkOffset,
                ChunkSize = TerrainMeshParameters.ChunkSize,
                BorderWidth = TerrainMeshParameters.BorderWidth
            });
        }

        private void SetTerrainMeshParamsBuffer(TerrainMeshParameters newParams)
        {
            if (newParams != TerrainMeshParameters)
            {
                TerrainMesh.SetParamsBuffer(newParams);
            }
        }

        public void Render(TerrainMeshRenderDescriptor terrainMeshRenderDescriptor)
        {
            TerrainMesh.Render(terrainMeshRenderDescriptor);
        }
    }
}
