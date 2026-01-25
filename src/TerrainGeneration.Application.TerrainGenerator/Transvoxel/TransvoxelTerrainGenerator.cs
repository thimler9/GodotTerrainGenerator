using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.TerrainGenerator.Transvoxel;
public class TransvoxelTerrainGenerator
{
    RenderingDevice Rd;

    SDFGenerator.SDFGenerator SDFGenerator;
    Transvoxel Transvoxel;

    public TransvoxelTerrainGenerator(RenderingDevice rd)
    {
        Rd = rd;
    }

    //public TerrainMesh GetTerrainMesh()
    //{

    //}
}
