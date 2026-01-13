#[compute]
#version 460

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;

struct Vertex {
    vec3 position;
    vec3 normal;
};

struct Triangle {
    Vertex vertices[3];
};

layout(set = 0, binding = 0) restrict readonly uniform Params {
    uint chunk_size;
    vec3 chunk_offset;
    uint lod;
    float transition_width;
    uint max_num_vertices;
}
params;

layout(set = 1, binding = 0, std430) restrict buffer LookupTables {
    int data[];
}
lookup_tables;

layout(set = 2, binding = 0, std430) restrict buffer CounterBuffer {
    uint counter;
}
counter;

layout(set = 3, binding = 0, std430) restrict buffer SDFBuffer {
    float data[];
}
sdf_buffer;

layout(set = 4, binding = 0, std430) restrict buffer normalsBuffer {
    vec3 data[];
}
normals_buffer;

layout(set = 5, binding = 0, std430) restrict buffer TriangleBuffer {
    Triangle triangles[];
}
triangle_buffer;

const int edge_table_index = 0;
const int edge_to_vertices_index = 256;
const int num_tri_index = 280;
const int triangle_lookup_index = 536;
const int transition_cell_class_index = 4632;
const int transition_cell_num_tri_index = 5144;
const int transition_edge_to_vertices_index = 5200;
const int transition_vertex_data_index = 5232;

vec3 sample_trilinear(vec3 position) {
    float lod = float(params.lod);
    float size = float(params.chunk_size);

    vec3 pos = position / lod;

    pos.x = clamp(pos.x, 0.0, size / lod);
    pos.y = clamp(pos.y, 0.0, size / lod);
    pos.z = clamp(pos.z, 0.0, size / lod);

    int x = int(floor(pos.x));
    int y = int(floor(pos.y));
    int z = int(floor(pos.z));

    int X = int(params.chunk_size) / int(params.lod) + 1;
    int Y = X * X;

    float fx = pos.x - float(x);
    float fy = pos.y - float(y);
    float fz = pos.z - float(z);

    int xp1 = x + 1;
    int yp1 = y + 1;
    int zp1 = z + 1;

    vec3 x0 = normals_buffer.data[x + y * X + z * Y] * (1.0 - fx) + normals_buffer.data[xp1 + y * X + z * Y] * fx;
    vec3 x1 = normals_buffer.data[x + y * X + zp1 * Y] * (1.0 - fx) + normals_buffer.data[xp1 + y * X + zp1 * Y] * fx;
    vec3 x2 = normals_buffer.data[x + yp1 * X + z * Y] * (1.0 - fx) + normals_buffer.data[xp1 + yp1 * X + z * Y] * fx;
    vec3 x3 = normals_buffer.data[x + yp1 * X + zp1 * Y] * (1.0 - fx) + normals_buffer.data[xp1 + yp1 * X + zp1 * Y] * fx;

    vec3 z0 = x0 * (1.0 - fz) + x1 * fz;
    vec3 z1 = x1 * (1.0 - fz) + x3 * fz;

    return z0 * (1.0 - fy) + z1 * fy;
}

// ----------------------------------------------------------------------------

/*
    Makes the voxels using the noise map and the normals.
*/
void MainVoxels(uvec3 id) {
    uint lod = params.lod;

    uint tempX = id.x * lod;
    uint tempY = id.y * lod;
    uint tempZ = id.z * lod;

    uint adjustedSize = params.chunk_size / params.lod + 2; //After checking if it's in the chunk, we need to convert it
        //to the size of the noise array since that's where we get the data points from
    
    vec4 currVertices[8] = vec4[8](
        vec4(tempX, tempY, tempZ, sdf_buffer.data[id.x + id.y * adjustedSize + id.z * adjustedSize * adjustedSize]),
        vec4(tempX, tempY + lod, tempZ, sdf_buffer.data[id.x + (id.y + 1) * adjustedSize + id.z * adjustedSize * adjustedSize]),
        vec4(tempX + lod, tempY + lod, tempZ, sdf_buffer.data[(id.x + 1) + (id.y + 1) * adjustedSize + id.z * adjustedSize * adjustedSize]),
        vec4(tempX + lod, tempY, tempZ, sdf_buffer.data[(id.x + 1) + id.y * adjustedSize + id.z * adjustedSize * adjustedSize]),
        vec4(tempX, tempY, tempZ + lod, sdf_buffer.data[id.x + id.y * adjustedSize + (id.z + 1) * adjustedSize * adjustedSize]),
        vec4(tempX, tempY + lod, tempZ + lod, sdf_buffer.data[id.x + (id.y + 1) * adjustedSize + (id.z + 1) * adjustedSize * adjustedSize]),
        vec4(tempX + lod, tempY + lod, tempZ + lod, sdf_buffer.data[(id.x + 1) + (id.y + 1) * adjustedSize + (id.z + 1) * adjustedSize * adjustedSize]),
        vec4(tempX + lod, tempY, tempZ + lod, sdf_buffer.data[(id.x + 1) + id.y * adjustedSize + (id.z + 1) * adjustedSize * adjustedSize])
    );

    int edgeIndex = 0;
    int edgeCuts = 0;
    vec3 triangleVertices[12];

    for (int i = 7 /*currVertices.Length - 1*/; i >= 0; i--) {
        int vertexValue = currVertices[i].w >= 0 ? 1 : 0;
        edgeIndex |= vertexValue;
        edgeIndex <<= 1;

    }
    edgeIndex >>= 1;

    edgeCuts = lookup_tables.data[edge_table_index + edgeIndex];
    

    for (int j = 0; j < 12; j++) {
        int firstVoxelIndex = lookup_tables.data[edge_to_vertices_index + j * 2];
        int secondVoxelIndex = lookup_tables.data[edge_to_vertices_index + j * 2 + 1];
        vec4 v0 = currVertices[firstVoxelIndex];
        vec4 v1 = currVertices[secondVoxelIndex];
        float t = v0.w / (v0.w - v1.w);
        triangleVertices[j] = mix(v0, v1, t).xyz;
    }

    for (int i = 0; i < lookup_tables.data[edgeIndex + num_tri_index]; i = i++)
    {
        uint count = atomicAdd(counter.counter, 1u);
        if (count >= params.max_num_vertices) {
            return;
        }

        Triangle newTriangle;
        newTriangle.vertices[0] = Vertex(vec3(0, 0, 0), vec3(0, 0, 0));
        newTriangle.vertices[1] = Vertex(vec3(0, 0, 0), vec3(0, 0, 0));
        newTriangle.vertices[2] = Vertex(vec3(0, 0, 0), vec3(0, 0, 0));

        newTriangle.vertices[0].position = triangleVertices[lookup_tables.data[edgeIndex * 16 + i * 3 + 2 + triangle_lookup_index]];
        newTriangle.vertices[1].position = triangleVertices[lookup_tables.data[edgeIndex * 16 + i * 3 + 1 + triangle_lookup_index]];
        newTriangle.vertices[2].position = triangleVertices[lookup_tables.data[edgeIndex * 16 + i * 3 + triangle_lookup_index]];

        newTriangle.vertices[0].normal = sample_trilinear(newTriangle.vertices[0].position);
        newTriangle.vertices[1].normal = sample_trilinear(newTriangle.vertices[1].position);
        newTriangle.vertices[2].normal = sample_trilinear(newTriangle.vertices[2].position);

        // write the triangle into the buffer
        triangle_buffer.triangles[count] = newTriangle;
    }
}

void main() {
    uint adjusted_size = params.chunk_size / params.lod;
    uvec3 id = gl_GlobalInvocationID.xyz;
    if (id.x >= adjusted_size || id.y >= adjusted_size || id.z >= adjusted_size) {
        return;
    }

    // Create the main voxel mesh using marching cubes
    MainVoxels(id);
}