#[vertex]

#version 450

#include "Includes/scene_data.glsl"
#include "Includes/scene_data_helpers.glsl"

struct VertexInput {
    vec4 position;
    vec4 normal;
};

layout(std430, set = 1, binding = 0) buffer VertexInputBuffer {
    VertexInput vertices[];
};

layout(location = 0) out vec3 fragNormal;

void main() {
    VertexInput vertex = vertices[gl_VertexIndex];

    vec4 positionWS = vec4(vertex.position.xyz, 1.0);
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
    outColor = vec4(fragNormal, 1.0);
}