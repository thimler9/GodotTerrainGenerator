using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

namespace TerrainGeneration.Application.SDFGenerator
{

    public class SDFGenerator
    {
        RenderingDevice rd;


        Rid simplexNoiseShader;

        public SDFGenerator(string simplexNoiseShaderPath)
        {
            rd = RenderingServer.CreateLocalRenderingDevice();

            RDShaderFile simplexNoiseFile = GD.Load<RDShaderFile>(simplexNoiseShaderPath);
            RDShaderSpirV simplexNoiseByteCode = simplexNoiseFile.GetSpirV();
            simplexNoiseShader = rd.ShaderCreateFromSpirV(simplexNoiseByteCode);
        }

        public void SimplexNoise(SimplexNoiseParameters parameters)
        {

        }
    }
}
