using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Application.SDFGenerator.SimplexNoise;
public class SimplexNoiseShaderDescriptor
{
    public SimplexNoiseShaderParameters Parameters { get; set; }
    public string? ShaderPath { get; set; }
}
