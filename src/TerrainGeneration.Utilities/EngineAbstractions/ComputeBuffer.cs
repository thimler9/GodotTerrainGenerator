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

    public ComputeBuffer(RenderingDevice rd, uint size, RenderingDevice.UniformType uniformType, int binding = 0, byte[]? data = null, RenderingDevice.StorageBufferUsage? storageBufferUsage = null)
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
        if (uniformType == RenderingDevice.UniformType.StorageBuffer)
        {
            if (storageBufferUsage != null)
            {
                Buffer = Rd.StorageBufferCreate(size, usage: storageBufferUsage.Value);
            }
            else
            {
                Buffer = Rd.StorageBufferCreate(size);
            }
        }
        else
        {
            Buffer = Rd.UniformBufferCreate(size);
        }

        if (data != null)
        {
            Rd.BufferUpdate(Buffer, 0, (uint)data.Length, data);
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

    public void ClearData(uint offset, uint size)
    {
        if (offset < 0)
        {
            throw new ArgumentException($"{nameof(offset)} cannot be negative.");
        }
        if (size <= 0)
        {
            throw new ArgumentException($"{nameof(size)} must be positive.");
        }

        Rd.BufferClear(Buffer, offset, size);
    }

    public T[]? GetData<T>()
    {
        byte[]? bufferData = Rd.BufferGetData(Buffer);
        T[]? outputData = new T[bufferData.Length / Marshal.SizeOf<T>()];
        System.Buffer.BlockCopy(bufferData, 0, outputData, 0, bufferData.Length);

        return outputData;
    }

    public Rid CreateUniformSet(ComputeShader shader, uint shaderSet)
    {
        if (shaderSet < 0)
        {
            throw new ArgumentException($"{nameof(shader)} is not valid.");
        }

        return Rd.UniformSetCreate([Uniform], shader.Shader, shaderSet);
    }

    public Rid CreateUniformSet(GraphicShader shader, uint shaderSet)
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

    public Rid GetBuffer()
    {
        return Buffer;
    }
}
