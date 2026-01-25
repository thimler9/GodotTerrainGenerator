using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.TerrainGenerator.Abstractions;

namespace TerrainGeneration.Application.TerrainGenerator
{
    public class TerrainChunk : IOctreeRenderable
    {
        //private TerrainSpawns TerrainSpawns;
        private TerrainMesh TerrainMesh;

        private Vector3 Offset;
        private uint Size;

        public TerrainChunk(TerrainChunkDescriptor descriptor)
        {
            Offset = descriptor.Offset;
            Size = descriptor.Size;

            //Vector3 center = offset + Vector3.One * (size / 2);
            //TerrainMesh = MapGenerator.GetMesh(offset, size, lod);
            //TerrainSpawns = new TerrainSpawns(offset, size, depth, Bounds);
        }

        public void Dispose()
        {
            TerrainMesh.Dispose();
        }

        public void Render(Vector3 playerPosition)
        {
            throw new NotImplementedException();
        }
    }
}
