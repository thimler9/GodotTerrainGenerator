#[compute]
#version 460

#include "Includes/simplex_noise_functions.glsl"

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

float terrain_probability(uvec3 id, uint array_index, float temperature, float temperature_spread, float depth, float depth_spread)
{
    float temperature_value = temperature_values.data[array_index];
    float depth_value = id.y * sdf_params.lod + sdf_params.chunk_offset.y;

    float temperature_factor = 1.0 / (1.0 + ((temperature_value - temperature) * (temperature_value - temperature) / (temperature_spread * temperature_spread)));
    float depth_factor = 1.0 / (1.0 + ((depth_value - depth) * (depth_value - depth) / (depth_spread * depth_spread)));
    return depth_factor * temperature_factor;
}


void main() {
    uint adjustedSize = sdf_params.chunk_size / sdf_params.lod + 2;

    uvec3 id = gl_GlobalInvocationID;
    uint array_index = id.x + id.y * adjustedSize + id.z * adjustedSize * adjustedSize;
    if (id.x >= adjustedSize || id.y >= adjustedSize || id.z >= adjustedSize) {
        return;
    }

    float probability = biome_params.params.ignore_biome == -1 ? 1.0 : terrain_probability(id, array_index, biome_params.params.temperature, biome_params.params.temperature_spread, biome_params.params.depth, biome_params.params.depth_spread);
    float noise_value = simplex_noise(simplex_noise_params, id, sdf_params.chunk_offset.xyz, sdf_params.lod);

    // We are going to test against another noise value to see if the biomes are working.
    SimplexNoiseParams newParams;
    newParams.seed = 1;
    newParams.scale = simplex_noise_params.scale * 3;
    newParams.strength = 1;
    newParams.num_octaves = 4;
    newParams.frequency = 0.9;
    newParams.amplitude = 1.0;
    newParams.lacunarity = 2.5;
    newParams.gain = 0.35;
    float noise_value2 = simplex_noise(newParams, id, sdf_params.chunk_offset.xyz, sdf_params.lod);
    float probability2 = biome_params.params.ignore_biome == -1 ? 1.0 : terrain_probability(id, array_index, 60, 10.0, 1000, 250.0);


    output_buffer.data[array_index] = noise_value * probability + noise_value2 * probability2;
    // output_buffer.data[array_index] = noise_value * probability;
}