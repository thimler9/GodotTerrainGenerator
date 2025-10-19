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
        //private TerrainMesh TerrainMesh;
        //public Bounds Bounds;

        private Vector3 Offset;
        private float Size;

        public TerrainChunk(Vector3 offset, int size, int lod, int depth)
        {
            Vector3 center = offset + Vector3.One * (size / 2);
            //Bounds = new Bounds(center, Vector3.One * (size + 2 * lod * Settings.settings.mapData.terrainRenderingSettings.transitionWidthMult + 20));

            //TerrainMesh = MapGenerator.GetMesh(offset, size, lod);
            //TerrainMesh.meshProps.SetFloat("_chunkSize", size);
            //TerrainMesh.meshProps.SetFloat("_width", lod * Settings.settings.mapData.terrainRenderingSettings.transitionWidthMult);

            //Matrix4x4 localToWorld = new Matrix4x4();
            //localToWorld.SetTRS(offset, Quaternion.identity, Vector3.One);
            //TerrainMesh.meshProps.SetMatrix("_ObjMat", localToWorld);

            //TerrainSpawns = new TerrainSpawns(offset, size, depth, Bounds);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Render(Vector3 playerPosition)
        {
            throw new NotImplementedException();
        }
    }
}
