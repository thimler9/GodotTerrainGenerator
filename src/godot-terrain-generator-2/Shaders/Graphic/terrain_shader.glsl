#[vertex]

#version 450

#include "Includes/scene_data.glsl"
#include "Includes/scene_data_helpers.glsl"
#include "Includes/adjust_position.glsl"

struct TerrainParams {
    uint chunk_size;
    float border_width;
    uint expand_borders;
    uint retract_borders;
    vec4 chunk_offset;
};

struct VertexInput {
    vec4 position;
    vec4 normal;
};

layout(std430, set = 1, binding = 0) buffer VertexInputBuffer {
    VertexInput vertices[];
};

layout(set = 2, binding = 0) uniform TerrainParamsBuffer {
    TerrainParams terrain_params;
};

layout(location = 0) out vec3 fragNormal;

void main() {
    VertexInput vertex = vertices[gl_VertexIndex];

    vec4 positionOS = vec4(vertex.position.xyz, 1.0);
    vec3 adjustedPositionOS;
    // Fix border positions
    AdjustPosition(positionOS.xyz, vertex.normal.xyz, terrain_params.chunk_size,
        terrain_params.border_width, terrain_params.expand_borders,
        terrain_params.retract_borders, adjustedPositionOS);
    positionOS.xyz = adjustedPositionOS;
    
    vec4 positionWS = positionOS + terrain_params.chunk_offset;
    vec4 positionVS = scene.data.view_matrix * positionWS;
    vec4 positionCS = scene.data.projection_matrix * positionVS;

    gl_Position = positionCS;
    fragNormal = vertex.normal.xyz;
}

#[fragment]

#version 450

layout(location = 0) in vec3 fragNormal;
layout(location = 0) out vec4 outColor;

void main() {
    // outColor = vec4(gl_FragCoord.z, gl_FragCoord.z, gl_FragCoord.z, 1.0);
    outColor = vec4(fragNormal, 1.0);
}