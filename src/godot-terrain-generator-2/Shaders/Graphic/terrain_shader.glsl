#[vertex]

#version 450

#include "Includes/scene_data.glsl"
#include "Includes/scene_data_helpers.glsl"
#include "Includes/adjust_position.glsl"

struct TerrainParams {
    vec4 chunk_offset;
    uint chunk_size;
    uint expand_borders;
    uint retract_borders;
    uint lod;
};

struct TerrainConstants {
    float border_width;
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

layout(set = 3, binding = 0) uniform TerrainConstantsBuffer {
    TerrainConstants terrain_constants;
};

layout(location = 0) out vec3 fragNormal;
layout(location = 1) out uint lod;

void main() {
    VertexInput vertex = vertices[gl_VertexIndex];

    vec4 positionOS = vec4(vertex.position.xyz, 1.0);
    // Fix border positions
    vec3 adjustedPositionOS = AdjustPosition(positionOS.xyz, vertex.normal.xyz, terrain_params.chunk_size,
        terrain_constants.border_width, terrain_params.lod, terrain_params.expand_borders,
        terrain_params.retract_borders);

    positionOS.xyz = adjustedPositionOS;
    
    vec4 positionWS = positionOS + terrain_params.chunk_offset;
    vec4 positionVS = scene.data.view_matrix * positionWS;
    vec4 positionCS = scene.data.projection_matrix * positionVS;

    gl_Position = positionCS;
    fragNormal = vertex.normal.xyz;
    lod = terrain_params.lod;
}

#[fragment]

#version 450

layout(location = 0) in vec3 fragNormal;
layout(location = 1) in flat uint lod;

layout(location = 0) out vec4 outColor;

void main() {
    // outColor = vec4(gl_FragCoord.z, gl_FragCoord.z, gl_FragCoord.z, 1.0);
    vec3 colorValue = vec3(0.0);

    if (lod == 64)
    {
        colorValue = vec3(1.0, 0.0, 0.0); // Red for LOD 64
    }
    else if (lod == 32)
    {
        colorValue = vec3(0.0, 1.0, 0.0); // Green for LOD 32
    }
    else if (lod == 16)
    {
        colorValue = vec3(0.0, 0.0, 1.0); // Blue for LOD 16
    }
    else if (lod == 8)
    {
        colorValue = vec3(1.0, 1.0, 0.0); // Yellow for LOD 8
    }
    else if (lod == 4)
    {
        colorValue = vec3(1.0, 0.0, 1.0); // Magenta for LOD 4
    }
    else if (lod == 2)
    {
        colorValue = vec3(0.0, 1.0, 1.0); // Cyan for LOD 2
    }
    else if (lod == 1)
    {
        colorValue = vec3(1.0, 1.0, 1.0); // White for LOD 1
    }

    vec3 normalColor = (fragNormal + 1.0) * 0.5; // Map normal from [-1, 1] to [0, 1]
    vec3 finalColor = normalColor * colorValue; // Modulate normal color with LOD color


    outColor = vec4(finalColor, 1.0);
}