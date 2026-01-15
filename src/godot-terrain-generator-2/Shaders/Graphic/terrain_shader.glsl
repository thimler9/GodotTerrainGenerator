#[vertex]

#version 450

struct VertexInput {
    vec3 position;
    uint padding;
    vec3 normal;
    uint padding2;
};

layout(std430, binding = 0) buffer ParticleBuffer {
    VertexInput vertices[];
};

void main() {
    gl_Position = vec4(vertices[gl_VertexID].position, 1.0);
}

#[fragment]

#version 450

layout(location = 0) out vec4 outColor;

void main() {
    outColor = vec4(0.2, 0.3, 0.7, 1.0);
}