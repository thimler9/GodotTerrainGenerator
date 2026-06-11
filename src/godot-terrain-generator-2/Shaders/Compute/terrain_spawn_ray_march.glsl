#[compute]
#version 460

#include "Includes/simplex_noise_functions.glsl"

layout(local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

struct Candidate {
    vec4 position;
    vec4 normal_and_depth;
    vec4 environment_and_random;
};

layout(set = 0, binding = 0, std430) restrict readonly buffer PdsPoints {
    vec4 points[];
}
pds_points;

layout(set = 1, binding = 0, std430) restrict buffer Candidates {
    Candidate candidates[];
}
candidates;

layout(set = 2, binding = 0, std430) restrict buffer CandidateCounter {
    uint counter;
}
candidate_counter;

layout(set = 3, binding = 0) restrict readonly uniform Params {
    vec4 chunk_offset;
    uint chunk_size;
    uint lod;
    uint point_count;
    uint max_candidates;
    uint max_hits_per_ray;
    float top_y;
    float bottom_y;
    float step_size;
    uint refine_steps;
    float sea_level;
    float sun_light;
    uint seed;
    uint noise_seed;
    float noise_scale;
    float noise_strength;
    uint noise_octaves;
    float noise_frequency;
    float noise_amplitude;
    float noise_lacunarity;
    float noise_gain;
}
params;

float hash01(uint value) {
    value = wang(value);
    return float(value & 0x00ffffffu) / 16777216.0;
}

float terrain_noise(vec3 world_position) {
    float noise_height = 0.0;
    float frequency = params.noise_frequency;
    float amplitude = params.noise_amplitude;
    uint seed = params.noise_seed;

    for (uint i = 0u; i < params.noise_octaves; i++) {
        float seed_value = float(mod(wang(seed), seed_modulo));
        vec3 sample_point = (world_position + seed_value * vec3(1.0)) / params.noise_scale * frequency;
        noise_height += snoise(sample_point) * amplitude;
        amplitude *= params.noise_gain;
        frequency *= params.noise_lacunarity;
        seed++;
    }

    return noise_height * params.noise_strength;
}

float density(vec3 world_position) {
    return terrain_noise(world_position);
}

vec3 estimate_normal(vec3 world_position) {
    const float e = 0.75;
    float dx = density(world_position + vec3(e, 0.0, 0.0)) - density(world_position - vec3(e, 0.0, 0.0));
    float dy = density(world_position + vec3(0.0, e, 0.0)) - density(world_position - vec3(0.0, e, 0.0));
    float dz = density(world_position + vec3(0.0, 0.0, e)) - density(world_position - vec3(0.0, 0.0, e));
    return normalize(vec3(dx, dy, dz));
}

vec3 refine_hit(vec3 high_point, vec3 low_point) {
    float high_density = density(high_point);
    vec3 a = high_point;
    vec3 b = low_point;

    for (uint i = 0u; i < params.refine_steps; i++) {
        vec3 mid = (a + b) * 0.5;
        float mid_density = density(mid);
        if ((high_density > 0.0 && mid_density > 0.0) || (high_density <= 0.0 && mid_density <= 0.0)) {
            a = mid;
            high_density = mid_density;
        } else {
            b = mid;
        }
    }

    return (a + b) * 0.5;
}

void write_candidate(vec3 position, uint point_index, uint hit_index) {
    uint write_index = atomicAdd(candidate_counter.counter, 1u);
    if (write_index >= params.max_candidates) {
        atomicAdd(candidate_counter.counter, uint(-1));
        return;
    }

    vec3 normal = estimate_normal(position);
    float underwater_depth = max(params.sea_level - position.y, 0.0);
    float temperature = clamp(1.0 - abs(position.y - params.sea_level) / max(params.top_y - params.bottom_y, 1.0), 0.0, 1.0);
    float light = clamp(max(normal.y, 0.0) * params.sun_light, 0.0, 1.0);
    float random_value = hash01(params.seed ^ point_index * 747796405u ^ hit_index * 2891336453u);

    candidates.candidates[write_index].position = vec4(position, 1.0);
    candidates.candidates[write_index].normal_and_depth = vec4(normal, underwater_depth);
    candidates.candidates[write_index].environment_and_random = vec4(temperature, light, clamp(1.0 - normal.y, 0.0, 1.0), random_value);
}

void main() {
    uint point_index = gl_GlobalInvocationID.x;
    if (point_index >= params.point_count) {
        return;
    }

    vec2 xz = pds_points.points[point_index].xy;
    vec2 chunk_min = params.chunk_offset.xz;
    vec2 chunk_max = chunk_min + vec2(float(params.chunk_size));
    if (xz.x < chunk_min.x || xz.y < chunk_min.y || xz.x >= chunk_max.x || xz.y >= chunk_max.y) {
        return;
    }

    vec3 previous_position = vec3(xz.x, params.top_y, xz.y);
    float previous_density = density(previous_position);
    uint hit_count = 0u;

    for (float y = params.top_y - params.step_size; y >= params.bottom_y && hit_count < params.max_hits_per_ray; y -= params.step_size) {
        vec3 current_position = vec3(xz.x, y, xz.y);
        float current_density = density(current_position);

        if (previous_density > 0.0 && current_density <= 0.0) {
            vec3 hit_position = refine_hit(previous_position, current_position);
            write_candidate(hit_position, point_index, hit_count);
            hit_count++;
        }

        previous_position = current_position;
        previous_density = current_density;
    }
}
