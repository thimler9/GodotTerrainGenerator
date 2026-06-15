#[compute]
#version 460

#include "Includes/simplex_noise_functions.glsl"

struct BiomeParams {
    float temperature;
    float temperature_spread;
    float depth;
    float depth_spread;
};

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;

layout(set = 0, binding = 0) restrict buffer SimplexNoiseParamsBuffer {
    SimplexNoiseParams simplex_noise_params[];
};

layout(set = 1, binding = 1) restrict readonly uniform TemperatureParams {
    SimplexNoiseParams temperature_noise_params;
} temperatureParams;

layout(set = 2, binding = 0) restrict buffer BiomeParamsBuffer{
    BiomeParams biome_params[];
};

layout(set = 3, binding = 0) restrict readonly uniform SDFParams {
    vec4 chunk_offset;
    uint chunk_size;
    uint lod;
} sdfParams;

layout(set = 4, binding = 0, std430) restrict buffer OutputBuffer {
    float data[];
}
output_buffer;

const int number_of_biomes = 1;

float sample_depth(uvec3 id)
{
    return id.y * sdfParams.lod + sdfParams.chunk_offset.y;
}

float sample_temperature(uvec3 id)
{
    return simplex_noise(temperatureParams.temperature_noise_params, id, sdfParams.chunk_offset.xyz, sdfParams.lod);
}

float terrain_probability(uvec3 id, float temperature, float temperature_spread, float depth, float depth_spread)
{
    float temperature_value = sample_temperature(id);
    float depth_value = sample_depth(id);

    float temperature_factor = 1.0 / (1.0 + (temperature_spread * temperature_spread * (temperature_value - temperature)) * (temperature_value - temperature));
    float depth_factor = 1.0 / (1.0 + (depth_spread * depth_spread * (depth_value - depth)) * (depth_value - depth));
    return temperature_factor * depth_factor;
}


void main() {
    uint adjustedSize = sdfParams.chunk_size / sdfParams.lod + 2;

    uvec3 id = gl_GlobalInvocationID;
    uint array_index = id.x + id.y * adjustedSize + id.z * adjustedSize * adjustedSize;
    if (id.x >= adjustedSize || id.y >= adjustedSize || id.z >= adjustedSize) {
        return;
    }

    vec2[number_of_biomes] biome_weights_and_values;
    float total_weight = 0.0;

    // Get the weights and noise values for each biome, and calculate the total weight    
    for (int i = 0; i < number_of_biomes; i++) {
        SimplexNoiseParams params = simplex_noise_params[i];
        BiomeParams biomeParam = biome_params[i];

        float probability = terrain_probability(id, biomeParam.temperature, biomeParam.temperature_spread, biomeParam.depth, biomeParam.depth_spread);
        float noise_value = simplex_noise(params, id, sdfParams.chunk_offset.xyz, sdfParams.lod);
        
        biome_weights_and_values[i] = vec2(probability, noise_value);
        total_weight += probability;
    }

    float noise_height = 0.0;
    // Get the final noise height by normalizing the weights and summing the contributions from each biome
    for (int i = 0; i < number_of_biomes; i++) {
        noise_height += biome_weights_and_values[i].x * biome_weights_and_values[i].y / total_weight;
    }

    output_buffer.data[array_index] = noise_height;
}