using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.SimplexNoise;

namespace TerrainGeneration.Application.SDFGenerator
{

    public class SDFGenerator
    {
        RenderingDevice Rd;
        uint ChunkSize;

        RDUniform OutputBufferUniform;
        Rid OutputBuffer;

        RDUniform SDFParametersUniform;
        Rid SDFParametersBuffer;

        SimplexNoiseShader SimplexNoiseShader;

        public SDFGenerator(SDFGeneratorSettings settings)
        {
            if (settings.ChunkSize / 8 == 0)
            {
                throw new ArgumentException($"{nameof(settings.ChunkSize)} / 8 must be positive. {nameof(settings.ChunkSize)} = {settings.ChunkSize}");
            }

            if (settings.SimplexNoiseShaderDescriptor == null)
            {
                throw new ArgumentNullException(nameof(settings.SimplexNoiseShaderDescriptor), "Cannot be null");
            }

            Rd = RenderingServer.CreateLocalRenderingDevice();

            // Create the output buffer used throughout calculations
            Rid outputBuffer = Rd.StorageBufferCreate(ChunkSize * ChunkSize * ChunkSize * sizeof(float));
            OutputBufferUniform = new RDUniform()
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 0
            };
            OutputBufferUniform.AddId(outputBuffer);
            OutputBuffer = outputBuffer;

            // Create the sdf paramters buffer that has info used in all shaders
            byte[] sdfParametersBytes = settings.SDFShaderParameters.ToByteArray();
            SDFParametersBuffer = Rd.UniformBufferCreate((uint)Marshal.SizeOf<SDFShaderParameters>(), sdfParametersBytes);
            SDFParametersUniform = new RDUniform()
            {
                UniformType = RenderingDevice.UniformType.UniformBuffer,
                Binding = 0
            };
            SDFParametersUniform.AddId(SDFParametersBuffer);

            // Setup the shaders
            SimplexNoiseShader = new SimplexNoiseShader(Rd, settings.SimplexNoiseShaderDescriptor, SDFParametersUniform, OutputBufferUniform);
        }

        public void SimplexNoise(SimplexNoiseShaderParameters parameters, long computeList)
        {
            SimplexNoiseShader.Dispatch(Rd, computeList, ChunkSize);
        }

        public void Dispose()
        {
            // Free the shaders
            SimplexNoiseShader.Dispose(Rd);

            Rd.FreeRid(SDFParametersBuffer);
            Rd.FreeRid(OutputBuffer);
            Rd.Free();
        }
    }
}
