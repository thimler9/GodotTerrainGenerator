using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.SDFGenerator.Pipeline;
public class SDFPipeline
{
    public SDFPipelineCPU? cPUSDFPipeline;
    public SDFPipelineGPU? gPUSDFPipeline;
}
