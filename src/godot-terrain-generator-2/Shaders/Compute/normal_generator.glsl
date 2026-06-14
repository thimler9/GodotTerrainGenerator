#[compute]
#version 460

#include "Includes/simplex_noise_functions.glsl"

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;

layout(set = 0, binding = 0) restrict readonly uniform Params {
    vec4 chunk_offset;
    uint chunk_size;
    uint lod;
}
params;

layout(set = 1, binding = 0, std430) restrict buffer InputMapBuffer {
    float data[];
}
input_map_buffer;

layout(set = 2, binding = 0, std430) restrict buffer OutputNormalBuffer {
    float data[];
}
output_normal_buffer;


void main() {
    uint adjusted_size = params.chunk_size / params.lod + 1u;

    uvec3 id = gl_GlobalInvocationID;    
    if (id.x >= adjusted_size || id.y >= adjusted_size || id.z >= adjusted_size) {
        return;
    }
    
    float value = input_map_buffer.data[id.x + id.y * (adjusted_size + 1u) + id.z * (adjusted_size + 1u) * (adjusted_size + 1u)];
    float dx = value - input_map_buffer.data[(id.x + 1u) + id.y * (adjusted_size + 1u) + id.z * (adjusted_size + 1u) * (adjusted_size + 1u)];
    float dy = value - input_map_buffer.data[id.x + (id.y + 1u) * (adjusted_size + 1u) + id.z * (adjusted_size + 1u) * (adjusted_size + 1u)];
    float dz = value - input_map_buffer.data[id.x + id.y * (adjusted_size + 1u) + (id.z + 1u) * (adjusted_size + 1u) * (adjusted_size + 1u)];

    vec3 normal = normalize(vec3(dx, dy, dz));
    output_normal_buffer.data[(id.x + id.y * adjusted_size + id.z * adjusted_size * adjusted_size) * 3u] = normal.x;
    output_normal_buffer.data[(id.x + id.y * adjusted_size + id.z * adjusted_size * adjusted_size) * 3u + 1u] = normal.y;
    output_normal_buffer.data[(id.x + id.y * adjusted_size + id.z * adjusted_size * adjusted_size) * 3u + 2u] = normal.z;
}