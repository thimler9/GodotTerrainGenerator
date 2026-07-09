using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;
using TerrainGeneration.Utilities.EngineAbstractions;

namespace TerrainGeneration.Application.SDFGenerator.Abstractions;
public interface ISDFShader
{
    public void Dispatch(uint chunkSize, uint lod, IShaderParameters parameters, ComputeBuffer sdfParametersBuffer, ComputeBuffer biomeParamsBuffer, ComputeBuffer temperatureValuesBuffer, ComputeBuffer outputBuffer);

    public void Dispose();
}
