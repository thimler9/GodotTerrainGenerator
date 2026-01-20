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
layout(location = 0) out vec3 fragColor;

void main() {
    VertexInput vertex = vertices[gl_VertexIndex];
    gl_Position = vec4(vertex.position.xyz, 1.0);
    // gl_Position = scene.data.projection_matrix * gl_Position;

    fragColor = vertex.normal.xyz;
}

#[fragment]

#version 450

layout(location = 0) in vec3 fragColor;
layout(location = 0) out vec4 outColor;

void main() {
    outColor = vec4(fragColor, 1.0);
}