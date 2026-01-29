using Godot;
using TerrainGeneration.Application.TerrainGenerator.Transvoxel;

namespace TerrainGeneration.Application.TerrainGenerator
{
    public class TerrainChunk
    {
        //private TerrainSpawns TerrainSpawns;

        public TerrainMesh TerrainMesh;
        private TerrainMeshShaderParameters TerrainMeshParameters;


        /// <summary>
        /// Creates a terrain chunk. Pass in the mesh generator and the chunk descriptor.
        /// </summary>
        /// <param name="transvoxelTerrainGenerator"></param>
        /// <param name="descriptor"></param>
        public TerrainChunk(TransvoxelTerrainGenerator transvoxelTerrainGenerator, TerrainChunkDescriptor descriptor)
        {
            transvoxelTerrainGenerator.SetSDFShaderParameters(new SDFGenerator.SDFShaderParameters(descriptor.ChunkOffset, descriptor.ChunkSize, descriptor.Lod));
            TerrainMesh = transvoxelTerrainGenerator.GetTerrainMesh();

            // Set the terrian mesh params
            TerrainMeshParameters = new TerrainMeshShaderParameters()
            {
                ExpandBorders = 0, // These have to be set later
                RetractBorders = 0,
                ChunkOffset = new Vector4(descriptor.ChunkOffset.X, descriptor.ChunkOffset.Y, descriptor.ChunkOffset.Z, 0.0f),
                ChunkSize = descriptor.ChunkSize,
                BorderWidth = descriptor.BorderWidth
            };
            TerrainMesh.SetShaderParameters(TerrainMeshParameters);
        }

        /// <summary>
        /// Disposes of all gpu resources for the terrain chunk
        /// </summary>
        public void Dispose()
        {
            TerrainMesh.Dispose();
        }


        /// <summary>
        /// Sets the borders for the draw shader
        /// </summary>
        /// <param name="retractBorders"></param>
        /// <param name="expandBorders"></param>
        public void SetTerrainMeshBorders(int retractBorders, int expandBorders)
        {
            SetTerrainMeshParamsBuffer(new TerrainMeshShaderParameters()
            {
                ExpandBorders = expandBorders,
                RetractBorders = retractBorders,
                ChunkOffset = TerrainMeshParameters.ChunkOffset,
                ChunkSize = TerrainMeshParameters.ChunkSize,
                BorderWidth = TerrainMeshParameters.BorderWidth
            });
        }

        /// <summary>
        /// Used to update parameters for the draw shader.
        /// </summary>
        /// <param name="newParams"></param>
        private void SetTerrainMeshParamsBuffer(TerrainMeshShaderParameters newParams)
        {
            if (TerrainMesh != null)
            {
                if (newParams != TerrainMeshParameters)
                {
                    TerrainMesh.SetShaderParameters(newParams);
                }
            }
            else
            {
                GD.PrintErr("Tried to set terrain mesh params buffer when terrain mesh was null");
            }
        }

        /// <summary>
        /// Renders the terrainchunk
        /// </summary>
        /// <param name="terrainMeshRenderDescriptor"></param>
        public void Render(TerrainMeshRenderDescriptor terrainMeshRenderDescriptor)
        {
            TerrainMesh.Render(terrainMeshRenderDescriptor);
        }
    }
}
