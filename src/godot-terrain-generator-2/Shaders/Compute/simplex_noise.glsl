#[compute]
#version 460

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;

layout(set = 0, binding = 0) restrict readonly uniform Params {
    vec3 chunk_offset;
    uint seed;
    vec3 scale;
    float strength;
    uint num_octaves;
    float frequency;
    float amplitude;
    float lacunarity;
    float gain;
}
params;

layout(set = 1, binding = 0) restrict readonly uniform SDFParams {
    uint chunk_size;
    uint lod;
}
params;

layout(set = 2, binding = 0, std430) restrict buffer OutputBuffer {
    float data[];
}
output_buffer;

void main() {
    uint chunkSize = params.chunk_size;
    uint array_index = gl_GlobalInvocationID.x + gl_GlobalInvocationID.y * chunkSize + gl_GlobalInvocationID.z * chunkSize * chunkSize;
    output_buffer.data[array_index] = float(array_index);
}