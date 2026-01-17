#[vertex]

#version 450

struct VertexInput {
    vec3 position;
    uint padding;
    vec3 normal;
    uint padding2;
};

// layout (location = 0) in vec2 inPos;
// layout (location = 1) in vec3 inColor;

layout(location = 0) out vec3 fragColor;

layout(std430, binding = 0) buffer VertexInputBuffer {
    VertexInput vertices[];
};

void main() {
    gl_Position = vec4(vertices[gl_VertexIndex].position, 1.0);
    fragColor = vec3(gl_VertexIndex / 3.0).xyz;
}

#[fragment]

#version 450

layout(location = 0) in vec3 fragColor;
layout(location = 0) out vec4 outColor;

void main() {
    outColor = vec4(fragColor, 1.0);
}