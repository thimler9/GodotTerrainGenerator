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
        RenderingDevice Rd;
        uint ChunkSize;

        RDUniform OutputBufferUniform;
        Rid OutputBuffer;


        SimplexNoiseShader SimplexNoiseShader;

        public SDFGenerator(SDFGeneratorSettings settings)
        {
            if (settings.ChunkSize / 8 == 0)
            {
                throw new ArgumentException($"{nameof(settings.ChunkSize)} / 8 must be positive. {nameof(settings.ChunkSize)} = {settings.ChunkSize}");
            }

            Rd = RenderingServer.CreateLocalRenderingDevice();
            Rid outputBuffer = Rd.StorageBufferCreate(ChunkSize * ChunkSize * ChunkSize * sizeof(float));
            RDUniform outputBufferUniform = new RDUniform()
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 0
            };
            outputBufferUniform.AddId(outputBuffer);
            OutputBufferUniform = outputBufferUniform;
            OutputBuffer = outputBuffer;

            SimplexNoiseShader = new SimplexNoiseShader(Rd, settings.SimplexNoiseShaderDescriptor, outputBufferUniform);
        }

        public void SimplexNoise(SimplexNoiseShaderParameters parameters, long computeList)
        {
            SimplexNoiseShader.Dispatch(Rd, computeList, ChunkSize);
        }

        public void Dispose()
        {
            // Free the shaders
            SimplexNoiseShader.Dispose(Rd);


            Rd.FreeRid(OutputBuffer);
            Rd.Free();
        }
    }
}
