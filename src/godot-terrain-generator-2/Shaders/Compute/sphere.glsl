#[compute]
#version 460

#include "Includes/operation_types.glsl"

struct SphereParams {
    float radius;
    vec3 center;
    uint operation;
};

struct BiomeParams {
    float temperature;
    float temperature_spread;
    float depth;
    float depth_spread;
    int ignore_biome;
};

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;

layout(set = 0, binding = 0) restrict readonly uniform SphereParamsBuffer {
    SphereParams sphere_params;
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
    uint adjusted_size = sdf_params.chunk_size / sdf_params.lod + 2;
    uvec3 id = gl_GlobalInvocationID;
    if (id.x >= adjusted_size || id.y >= adjusted_size || id.z >= adjusted_size) {
        return;
    }

    uint array_index = id.x + id.y * adjusted_size + id.z * adjusted_size * adjusted_size;
    vec3 world_position = sdf_params.chunk_offset.xyz + vec3(id) * float(sdf_params.lod);
    float sphere_distance = length(world_position - sphere_params.center) - sphere_params.radius;

    if (sphere_params.operation == Add) {
        output_buffer.data[array_index] += sphere_distance;
    } else if (sphere_params.operation == Subtract) {
        output_buffer.data[array_index] -= sphere_distance;
    } else if (sphere_params.operation == Multiply) {
        output_buffer.data[array_index] *= sphere_distance;
    } else if (sphere_params.operation == Divide) {
        output_buffer.data[array_index] /= sphere_distance;
    } else if (sphere_params.operation == Set) {
        output_buffer.data[array_index] = sphere_distance;
    }
}
