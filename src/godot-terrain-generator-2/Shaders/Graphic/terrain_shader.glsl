#[vertex]

#version 450

#include "Includes/scene_data.glsl"
#include "Includes/scene_data_helpers.glsl"

struct VertexInput {
    vec3 position;
    uint padding;
    vec3 normal;
    uint padding2;
};

layout(std430, set = 1, binding = 0) buffer VertexInputBuffer {
    VertexInput vertices[];
};

layout(location = 0) out vec3 fragNormal;

void main() {
    VertexInput vertex = vertices[gl_VertexIndex];

    vec4 position = vec4(vertex.position, 1.0);
    // Multiply position by view matrix
    // position = scene.data.projection_matrix * position;
    
    gl_Position = position;
    fragNormal = vertex.normal;
}

#[fragment]

#version 450

layout(location = 0) in vec3 fragNormal;
layout(location = 0) out vec4 outColor;

void main() {
    outColor = vec4(fragNormal, 1.0);
}