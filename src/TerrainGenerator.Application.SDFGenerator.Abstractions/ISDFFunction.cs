using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Utilities.EngineAbstractions;

namespace TerrainGeneration.Application.SDFGenerator.Abstractions;
public interface ISDFFunction
{
    public void Dispatch(ISDFFunctionParameters parameters, SDFParameters sdfParameters, ComputeBuffer biomeParamsBuffer, ComputeBuffer temperatureValuesBuffer, ComputeBuffer outputBuffer);

}
