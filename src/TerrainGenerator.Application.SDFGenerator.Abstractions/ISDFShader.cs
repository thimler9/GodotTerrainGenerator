using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

namespace TerrainGeneration.Application.SDFGenerator.Abstractions;
public interface ISDFShader
{
    public void Dispatch(uint chunkSize, uint lod, IShaderParameters parameters, RDUniform sdfParametersUniform, RDUniform biomeParamsUniform, RDUniform temperatureValues, RDUniform outputUniform);

    public void Dispose();
}
