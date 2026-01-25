using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.SDFGenerator.Abstractions;
public interface ISDFShader
{
    public void SetOutputUniformSet(RDUniform outputUniform);

    public void SetSDFParametersUniformSet(RDUniform sdfUniform);

    public void Dispatch(uint chunkSize, uint lod);

    public void Dispose();
}
