#[compute]
#version 460

#include "Includes/simplex_noise_functions.glsl"
#include "Includes/biome_probability.glsl"

struct BiomeParams {
    float temperature;
    float temperature_spread;
    float depth;
    float depth_spread;
    int ignore_biome;
};

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;

layout(set = 0, binding = 0) restrict readonly uniform SimplexNoiseParamsBuffer {
    SimplexNoiseParams simplex_noise_params;
};

layout(set = 1, binding = 0) restrict readonly uniform SDFParamsUniform {
    vec4 chunk_offset;
    uint chunk_size;
    uint lod;
} sdf_params;

layout(set = 2, binding = 0) restrict readonly uniform BiomeParamsUniform {
    BiomeParams params;
} biome_params;

layout(set = 3, binding = 0, std430) restrict buffer TemperatureValues {
    float data[];
} temperature_values;

layout(set = 4, binding = 0, std430) restrict buffer OutputBuffer {
    float data[];
} output_buffer;

void main() {
    uint adjustedSize = sdf_params.chunk_size / sdf_params.lod + 2;

    uvec3 id = gl_GlobalInvocationID;
    uint array_index = id.x + id.y * adjustedSize + id.z * adjustedSize * adjustedSize;
    if (id.x >= adjustedSize || id.y >= adjustedSize || id.z >= adjustedSize) {
        return;
    }

    float depth_value = float(id.y * sdf_params.lod) + sdf_params.chunk_offset.y;
    float probability = biome_params.params.ignore_biome == -1
        ? 1.0
        : biome_depth_probability(depth_value, biome_params.params.depth, biome_params.params.depth_spread);
    float noise_value = simplex_noise(simplex_noise_params, id, sdf_params.chunk_offset.xyz, sdf_params.lod);

    output_buffer.data[array_index] += noise_value * probability;
}