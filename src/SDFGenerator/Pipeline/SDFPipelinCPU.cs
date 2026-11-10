using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

namespace TerrainGeneration.Application.SDFGenerator.Pipeline;
public class SDFPipelinCPU
{
    public IReadOnlyList<BiomeDescriptor> Biomes { get; }
    public TemperatureDescriptor Termperature { get; }

    public SDFPipelinCPU(TemperatureDescriptor temperature, IReadOnlyList<BiomeDescriptor> biomes)
    {
        Biomes = biomes ?? throw new ArgumentNullException("Biomes cannot be null.");
        Termperature = temperature ?? throw new ArgumentNullException("Temperature cannot be null");
    }

    public float[,,] GetSDF(Vector3 chunkOffset, uint chunkSize, uint lod)
    {

    }
}
