#[compute]
#version 450
#extension GL_EXT_scalar_block_layout : require

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;

layout(set = 0, binding = 0, std430) uniform Params {
    vec3 chunk_offset;
    uint chunk_size;
    uint lod;
    vec3 padding;
}
params;

layout(set = 0, binding = 1, std430) buffer OutputBuffer {
    float data[];
}
output_buffer;

void main() {
    uint array_index = gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * params.chunk_size + gl_GlobalInvocationID.z * params.chunk_size * params.chunk_size;

    output_buffer.data[array_index] = array_index;
}