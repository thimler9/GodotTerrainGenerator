#[compute]
#version 460

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) restrict readonly buffer CounterBuffer {
    uint counter[];
}
counter_buffer;

// This buffer will hold the indirect draw arguments (uints)
layout(set = 1, binding = 0, std430) restrict buffer IndirectArgs {
    uint data[];
}
indirect_args;


void main() {
    uint vertexCount = counter_buffer.counter[0];

    // Vulkan DrawIndirect command expects: vertexCount, instanceCount, firstVertex, firstInstance
    indirect_args.data[0] = vertexCount;
    indirect_args.data[1] = 1u; // instanceCount
    indirect_args.data[2] = 0u; // firstVertex
    indirect_args.data[3] = 0u; // firstInstance
}