using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.SDFGenerator.Pipeline;
public class BiomeDescriptor
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public float Temperature { get; init; }
    public float TemperatureSpread { get; init; }
    public float Depth { get; init; }
    public float DepthSpread { get; init; }
    public bool IgnoreBiome { get; init; }
}
