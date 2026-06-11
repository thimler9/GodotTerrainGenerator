#[compute]
#version 460

#include "Includes/simplex_noise_functions.glsl"

layout(local_size_x = 64, local_size_y = 1, local_size_z = 1) in;

struct Candidate {
    vec4 position;
    vec4 normal_and_depth;
    vec4 environment_and_random;
};

struct SpawnDefinition {
    float preferred_temperature;
    float temperature_tolerance;
    float preferred_underwater_depth;
    float underwater_depth_tolerance;
    float preferred_light;
    float light_tolerance;
    float max_slope_cosine;
    float base_weight;
    float min_scale;
    float max_scale;
    uint type_index;
    uint padding;
};

struct SpawnSelection {
    vec4 position_and_scale;
    vec4 normal_and_type;
};

layout(set = 0, binding = 0, std430) restrict readonly buffer Candidates {
    Candidate candidates[];
}
candidates;

layout(set = 1, binding = 0, std430) restrict readonly buffer CandidateCounter {
    uint counter;
}
candidate_counter;

layout(set = 2, binding = 0, std430) restrict readonly buffer SpawnDefinitions {
    SpawnDefinition definitions[];
}
spawn_definitions;

layout(set = 3, binding = 0, std430) restrict buffer SpawnSelections {
    SpawnSelection selections[];
}
selections;

layout(set = 4, binding = 0, std430) restrict buffer SelectionCounter {
    uint counter;
}
selection_counter;

layout(set = 5, binding = 0) restrict readonly uniform Params {
    uint max_candidates;
    uint definition_count;
    uint max_selections;
    uint seed;
}
params;

float hash01(uint value) {
    value = wang(value);
    return float(value & 0x00ffffffu) / 16777216.0;
}

float preference(float value, float preferred, float tolerance) {
    return clamp(1.0 - abs(value - preferred) / max(tolerance, 0.0001), 0.0, 1.0);
}

float definition_weight(SpawnDefinition definition, Candidate candidate) {
    vec3 normal = normalize(candidate.normal_and_depth.xyz);
    if (normal.y < definition.max_slope_cosine) {
        return 0.0;
    }

    float temperature = candidate.environment_and_random.x;
    float light = candidate.environment_and_random.y;
    float underwater_depth = candidate.normal_and_depth.w;

    float temperature_weight = preference(temperature, definition.preferred_temperature, definition.temperature_tolerance);
    float depth_weight = preference(underwater_depth, definition.preferred_underwater_depth, definition.underwater_depth_tolerance);
    float light_weight = preference(light, definition.preferred_light, definition.light_tolerance);
    return definition.base_weight * temperature_weight * depth_weight * light_weight;
}

void main() {
    uint candidate_index = gl_GlobalInvocationID.x;
    uint candidate_count = min(candidate_counter.counter, params.max_candidates);
    if (candidate_index >= candidate_count) {
        return;
    }

    Candidate candidate = candidates.candidates[candidate_index];
    float total_weight = 0.0;
    for (uint i = 0u; i < params.definition_count; i++) {
        total_weight += definition_weight(spawn_definitions.definitions[i], candidate);
    }

    if (total_weight <= 0.0) {
        return;
    }

    float random_pick = hash01(params.seed ^ candidate_index * 1597334677u) * total_weight;
    float accumulated = 0.0;
    uint selected_type = 0u;
    SpawnDefinition selected_definition = spawn_definitions.definitions[0];

    for (uint i = 0u; i < params.definition_count; i++) {
        SpawnDefinition definition = spawn_definitions.definitions[i];
        accumulated += definition_weight(definition, candidate);
        if (random_pick <= accumulated) {
            selected_type = definition.type_index;
            selected_definition = definition;
            break;
        }
    }

    uint write_index = atomicAdd(selection_counter.counter, 1u);
    if (write_index >= params.max_selections) {
        atomicAdd(selection_counter.counter, uint(-1));
        return;
    }

    float scale_random = hash01(params.seed ^ candidate_index * 3812015801u ^ 0x68bc21ebu);
    float scale = mix(selected_definition.min_scale, selected_definition.max_scale, scale_random);
    selections.selections[write_index].position_and_scale = vec4(candidate.position.xyz, scale);
    selections.selections[write_index].normal_and_type = vec4(normalize(candidate.normal_and_depth.xyz), float(selected_type));
}
