#[compute]
#version 460

#include "Includes/simplex_noise_functions.glsl"

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;

layout(set = 0, binding = 0) restrict readonly uniform Params {
    SimplexNoiseParams simplex_noise_params;
};

layout(set = 1, binding = 0) restrict readonly uniform SDFParams {
    vec4 chunk_offset;
    uint chunk_size;
    uint lod;
}
sdfParams;

layout(set = 2, binding = 0, std430) restrict buffer OutputBuffer {
    float data[];
}
output_buffer;





void main() {
    uint adjustedSize = sdfParams.chunk_size / sdfParams.lod + 2;

    uvec3 id = gl_GlobalInvocationID;
    uint array_index = id.x + id.y * adjustedSize + id.z * adjustedSize * adjustedSize;
    if (id.x >= adjustedSize || id.y >= adjustedSize || id.z >= adjustedSize) {
        return;
    }

    float noise_height = simplex_noise(simplex_noise_params, id, sdfParams.chunk_offset.xyz, sdfParams.lod);

    output_buffer.data[array_index] = noise_height;
}