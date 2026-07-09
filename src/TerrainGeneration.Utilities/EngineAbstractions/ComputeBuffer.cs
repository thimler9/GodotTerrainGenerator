using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerrainGeneration.Utilities.Struct;

namespace TerrainGeneration.Utilities.EngineAbstractions;

public class ComputeBuffer
{
    private RenderingDevice Rd;
    private Rid Buffer;
    private RDUniform Uniform;

    public ComputeBuffer(RenderingDevice rd, uint size, RenderingDevice.UniformType uniformType, int binding = 0, byte[]? data = null)
    {
        if (rd == null)
        {
            throw new ArgumentNullException(nameof(rd));
        }

        if (size == 0)
        {
            throw new ArgumentException($"{nameof(size)} must be greater than zero.");
        }

        if (binding < 0)
        {
            throw new ArgumentException($"{nameof(binding)} must be positive.");
        }

        if (data != null && data.Length == 0)
        {
            throw new ArgumentException($"{nameof(data)} must be greater than 0");
        }

        Rd = rd;
        if (data == null)
        {
            Buffer = Rd.UniformBufferCreate(size, data);
        }
        else
        {
            Buffer = Rd.UniformBufferCreate(size);
        }

        Uniform = new RDUniform()
        {
            UniformType = uniformType,
            Binding = binding
        };
        Uniform.AddId(Buffer);
    }

    public void SetData(uint offset, uint size, byte[] data)
    {
        if (offset < 0)
        {
            throw new ArgumentException($"{nameof(offset)} cannot be negative.");
        }

        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        if (size <= 0)
        {
            throw new ArgumentException($"{nameof(size)} must be positive.");
        }

        Rd.BufferUpdate(Buffer, offset, size, data);
    }

    public byte[]? GetData()
    {
        return Rd.BufferGetData(Buffer);
    }

    public Rid CreateUniformSet(ComputeShader shader, uint shaderSet)
    {
        if (shaderSet < 0)
        {
            throw new ArgumentException($"{nameof(shader)} is not valid.");
        }

        return Rd.UniformSetCreate([Uniform], shader.Shader, shaderSet);
    }

    public void Dispose()
    {
        Rd.FreeRid(Buffer);
    }
}
