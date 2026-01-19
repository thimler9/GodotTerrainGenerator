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
        private RenderingDevice Rd;
        SDFShaderParameters SDFShaderParameters;

        public RDUniform OutputBufferUniform { get; set; }
        Rid OutputBuffer;

        RDUniform SDFParametersUniform;
        Rid SDFParametersBuffer;

        SimplexNoiseShader SimplexNoiseShader;

        bool ParametersUpdated = false;

        /// <summary>
        /// Create an SDFGenerator; dispatches compute shader for some sdf function.
        /// </summary>
        /// <param name="settings"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public SDFGenerator(RenderingDevice rd, SDFGeneratorSettings settings)
        {
            if (settings.ChunkSize / 8 == 0)
            {
                throw new ArgumentException($"{nameof(settings.ChunkSize)} / 8 must be positive. {nameof(settings.ChunkSize)} = {settings.ChunkSize}");
            }

            if (settings.SimplexNoiseShaderDescriptor == null)
            {
                throw new ArgumentNullException(nameof(settings.SimplexNoiseShaderDescriptor), "Cannot be null");
            }

            Rd = rd;
            SDFShaderParameters = settings.SDFShaderParameters;

            // Create the output buffer used throughout calculations
            uint chunkSizeToLodRatio = SDFShaderParameters.ChunkSize / SDFShaderParameters.Lod;

            Rid outputBuffer = Rd.StorageBufferCreate((chunkSizeToLodRatio + 2) * (chunkSizeToLodRatio + 2) * (chunkSizeToLodRatio + 2) * sizeof(float));
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
            ParametersUpdated = true;

            // Setup the shaders
            SimplexNoiseShader = new SimplexNoiseShader(Rd, settings.SimplexNoiseShaderDescriptor, SDFParametersUniform, OutputBufferUniform);
        }

        private void SetParameters(SDFShaderParameters parameters)
        {
            if (!this.SDFShaderParameters.Equals(parameters))
            {
                Rd.BufferUpdate(SDFParametersBuffer, 0, (uint)Marshal.SizeOf<SDFShaderParameters>(), parameters.ToByteArray());
                ParametersUpdated = true;
            }
        }

        public void DispatchShaders(SDFShaderParameters parameters)
        {
            SetParameters(parameters);

            // No reason to run if parameters haven't been changed
            if (ParametersUpdated)
            {
                long computeList = Rd.ComputeListBegin();
            
                // Run the shaders
                SimplexNoise(computeList);

                Rd.ComputeListEnd();
                //Rd.Submit();
                //Rd.Sync();
                ParametersUpdated = false;
            }
        }

        private void SimplexNoise(long computeList)
        {
            SimplexNoiseShader.Dispatch(Rd, computeList, SDFShaderParameters.ChunkSize, SDFShaderParameters.Lod);
        }

        public void PrintOutBuffer()
        {
            if (Rd == null)
            {
                throw new ArgumentNullException(nameof(Rd), "Cannot be null");
            }

            var outputBytes = Rd.BufferGetData(OutputBuffer);
            var output = new float[((SDFShaderParameters.ChunkSize / SDFShaderParameters.Lod) + 2) * ((SDFShaderParameters.ChunkSize / SDFShaderParameters.Lod) + 2) * ((SDFShaderParameters.ChunkSize / SDFShaderParameters.Lod) + 2)];
            Buffer.BlockCopy(outputBytes, 0, output, 0, output.Length * sizeof(float));
            GD.Print("Output: ", string.Join(", ", output));
            Console.WriteLine(string.Join(", ", output));
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
