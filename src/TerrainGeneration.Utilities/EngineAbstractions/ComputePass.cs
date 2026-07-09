using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrainGeneration.Utilities.EngineAbstractions;

public class ComputePass : IDisposable
{
    private RenderingDevice Rd;
    private ComputeShader ComputeShader;
    private long ComputeList;
    private Rid Pipeline;

    private List<Rid> ComputeShaderUniformSets = new List<Rid>();

    public ComputePass(RenderingDevice rd, ComputeShader computeShader, Rid pipeline)
    {
        if (rd == null)
        {
            throw new ArgumentNullException($"{nameof(rd)} cannot be null.");
        }

        if(!pipeline.IsValid)
        {
            throw new ArgumentNullException($"{nameof(Pipeline)} is not valid");
        }

        Pipeline = pipeline;
        Rd = rd;
        ComputeShader = computeShader;
        ComputeList = Rd.ComputeListBegin();
    }

    public void BindComputeBuffer(ComputeBuffer buffer, uint setIndex)
    {
        Rid bufferUniformSet = buffer.CreateUniformSet(ComputeShader, setIndex);

        if (!bufferUniformSet.IsValid)
        {
            throw new ArgumentException($"{nameof(bufferUniformSet)} is not valid");
        }

        ComputeShaderUniformSets.Add(bufferUniformSet);

        Rd.ComputeListBindUniformSet(ComputeList, bufferUniformSet, setIndex);
    }

    public void Dispatch(uint x, uint y, uint z)
    {
        Rd.ComputeListDispatch(ComputeList, xGroups: x, yGroups: y, zGroups: z);
        Rd.ComputeListEnd();
    }

    public void Dispose()
    {
        foreach (var uniformSet in ComputeShaderUniformSets)
        {
            Rd.FreeRid(uniformSet);
        }
    }
}
