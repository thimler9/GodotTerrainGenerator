using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

namespace TerrainGeneration.Application.SDFGenerator.Pipeline.CPU;
public class TemperatureDescriptorCPU
{
    public IReadOnlyList<ISDFPipelineStageCPU> Sdfs { get; init; } = Array.Empty<ISDFPipelineStageCPU>();

}
