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
layout(location = 1) out vec3 fragPositionWS;
layout(location = 2) out uint lod;

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
    fragPositionWS = positionWS.xyz;
    lod = terrain_params.lod;
}

#[fragment]

#version 450

#include "Includes/scene_data.glsl"
#include "Includes/scene_data_helpers.glsl"

struct DirectionalLightParams {
    // Direction from the sun toward the surface.
    vec4 direction_intensity;

    // RGB sun color.
    vec4 color;

    // x = specular intensity.
    vec4 specular;
};

layout(set = 4, binding = 0) uniform DirectionalLightParamsBuffer {
    DirectionalLightParams sun;
};

layout(location = 0) in vec3 fragNormal;
layout(location = 1) in vec3 fragPositionWS;
layout(location = 2) in flat uint lod;

layout(location = 0) out vec4 outColor;

const float SPECULAR_POWER = 32.0;

vec3 GetLodColor(uint value) {
    if (value == 64u)
    {
        return vec3(1.0, 0.0, 0.0); // Red for LOD 64
    }
    else if (value == 32u)
    {
        return vec3(0.0, 1.0, 0.0); // Green for LOD 32
    }
    else if (value == 16u)
    {
        return vec3(0.0, 0.0, 1.0); // Blue for LOD 16
    }
    else if (value == 8u)
    {
        return vec3(1.0, 1.0, 0.0); // Yellow for LOD 8
    }
    else if (value == 4u)
    {
        return vec3(1.0, 0.0, 1.0); // Magenta for LOD 4
    }
    else if (value == 2u)
    {
        return vec3(0.0, 1.0, 1.0); // Cyan for LOD 2
    }
    else if (value == 1u)
    {
        return vec3(1.0); // White for LOD 1
    }

    return vec3(0.0);
}

void main() {
    // vec3 baseColor = GetLodColor(lod);
    vec3 baseColor = vec3(1.0);
    vec3 normal = normalize(fragNormal);
    vec3 viewDirection = normalize(scene.data.inv_view_matrix[3].xyz - fragPositionWS);

    // The incoming direction points from the sun toward the surface.
    vec3 lightDirection = normalize(-sun.direction_intensity.xyz);
    float diffuseFactor = max(dot(normal, lightDirection), 0.0);

    vec3 ambientColor = scene.data.ambient_light_color_energy.rgb *
        scene.data.ambient_light_color_energy.a;
    vec3 diffuseLighting = sun.color.rgb * sun.direction_intensity.w * diffuseFactor;

    vec3 halfwayDirection = normalize(lightDirection + viewDirection);
    float specularFactor = pow(max(dot(normal, halfwayDirection), 0.0), SPECULAR_POWER);
    vec3 specularLighting = sun.color.rgb * sun.specular.x * specularFactor;

    vec3 finalColor = baseColor * (ambientColor + diffuseLighting) + specularLighting;

    // Apply fog relative to the active camera/player position. SceneData uses
    // the fog depth values as the start and end distances for the effect.
    vec3 playerPositionWS = scene.data.inv_view_matrix[3].xyz;
    float playerDistance = distance(fragPositionWS, playerPositionWS);
    float fogFactor = 0.0;

    if (scene.data.fog_depth_end > scene.data.fog_depth_begin)
    {
        fogFactor = smoothstep(
            scene.data.fog_depth_begin,
            scene.data.fog_depth_end,
            playerDistance);
    }

    finalColor = mix(finalColor, scene.data.fog_light_color, fogFactor);
    outColor = vec4(finalColor, 1.0);
}