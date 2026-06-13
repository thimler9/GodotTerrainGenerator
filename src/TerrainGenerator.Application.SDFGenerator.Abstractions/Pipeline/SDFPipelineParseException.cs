using System;

namespace TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline;

public sealed class SDFPipelineParseException : Exception
{
    public SDFPipelineParseException(string message)
        : base(message)
    {
    }

    public SDFPipelineParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
