#[compute]
#version 460

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;

struct Vertex {
    vec4 position;
    vec4 normal;
};

layout(set = 0, binding = 0) restrict readonly uniform Params {
    uint chunk_size;
    uint lod;
    float transition_width;
    uint max_num_vertices;
    vec3 chunk_offset;
}
params;

layout(set = 1, binding = 0, std430) restrict readonly buffer LookupTables {
    int data[];
}
lookup_tables;

layout(set = 2, binding = 0, std430) restrict buffer CounterBuffer {
    uint counter;
}
counter;

layout(set = 3, binding = 0, std430) restrict readonly buffer SDFBuffer {
    float data[];
}
sdf_buffer;

layout(set = 4, binding = 0, std430) restrict readonly buffer NormalsBuffer {
    float data[];
}
normals_buffer;

layout(set = 5, binding = 0, std140) restrict buffer VertexBuffer {
    Vertex vertex[];
}
vertex_buffer;

const int edge_table_index = 0;
const int edge_to_vertices_index = 256;
const int num_tri_index = 280;
const int triangle_lookup_index = 536;
const int transition_cell_class_index = 4632;
const int transition_cell_num_tri_index = 5144;
const int transition_edge_to_vertices_index = 5200;
const int transition_vertex_data_index = 5232;

void WriteVertex(uint index, vec3 position, vec3 normal) {
    vertex_buffer.vertex[index].position = vec4(position, 1.0);
    vertex_buffer.vertex[index].normal = vec4(normal, 1.0);
}

vec3 SampleTrilinear(vec3 position, uvec3 id) {
    float lod = float(params.lod);
    float size = float(params.chunk_size);

    vec3 pos = position / lod;

    pos.x = clamp(pos.x, 0.0, size / lod);
    pos.y = clamp(pos.y, 0.0, size / lod);
    pos.z = clamp(pos.z, 0.0, size / lod);

    uint x = uint(pos.x);
    uint y = uint(pos.y);
    uint z = uint(pos.z);

    uint X = params.chunk_size / params.lod + 1;
    uint Y = X * X;

    float fx = pos.x - float(x);
    float fy = pos.y - float(y);
    float fz = pos.z - float(z);

    uint xp1 = x + 1;
    uint yp1 = y + 1;
    uint zp1 = z + 1;

    vec3 x0 =   vec3(normals_buffer.data[(x + y * X + z * Y) * 3], 
                    normals_buffer.data[(x + y * X + z * Y) * 3 + 1], 
                    normals_buffer.data[(x + y * X + z * Y) * 3 + 2]) * (1.0 - fx) + 
                vec3(normals_buffer.data[(xp1 + y * X + z * Y) * 3], 
                     normals_buffer.data[(xp1 + y * X + z * Y) * 3 + 1], 
                     normals_buffer.data[(xp1 + y * X + z * Y) * 3 + 2]) * fx;

    vec3 x1 =   vec3(normals_buffer.data[(x + y * X + zp1 * Y) * 3], 
                    normals_buffer.data[(x + y * X + zp1 * Y) * 3+ 1], 
                    normals_buffer.data[(x + y * X + zp1 * Y) * 3 + 2]) * (1.0 - fx) + 
                vec3(normals_buffer.data[(xp1 + y * X + zp1 * Y) * 3], 
                     normals_buffer.data[(xp1 + y * X + zp1 * Y) * 3 + 1], 
                     normals_buffer.data[(xp1 + y * X + zp1 * Y) * 3 + 2]) * fx;

    vec3 x2 =   vec3(normals_buffer.data[(x + yp1 * X + z * Y) * 3], 
                    normals_buffer.data[(x + yp1 * X + z * Y) * 3 + 1], 
                    normals_buffer.data[(x + yp1 * X + z * Y) * 3 + 2]) * (1.0 - fx) + 
                vec3(normals_buffer.data[(xp1 + yp1 * X + z * Y) * 3], 
                     normals_buffer.data[(xp1 + yp1 * X + z * Y) * 3 + 1], 
                     normals_buffer.data[(xp1 + yp1 * X + z * Y) * 3 + 2]) * fx;

    vec3 x3 =   vec3(normals_buffer.data[(x + yp1 * X + zp1 * Y) * 3], 
                    normals_buffer.data[(x + yp1 * X + zp1 * Y) * 3 + 1], 
                    normals_buffer.data[(x + yp1 * X + zp1 * Y) * 3 + 2]) * (1.0 - fx) + 
                vec3(normals_buffer.data[(xp1 + yp1 * X + zp1 * Y) * 3], 
                     normals_buffer.data[(xp1 + yp1 * X + zp1 * Y) * 3 + 1], 
                     normals_buffer.data[(xp1 + yp1 * X + zp1 * Y) * 3 + 2]) * fx;

    vec3 z0 = x0 * (1.0 - fz) + x1 * fz;
    vec3 z1 = x2 * (1.0 - fz) + x3 * fz;

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
        int vertexValue = currVertices[i].w >= 0.0 ? 1 : 0;
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

    for (int i = 0; i < lookup_tables.data[edgeIndex + num_tri_index]; i++)
    {
        uint count = atomicAdd(counter.counter, 3u);
        if (count >= params.max_num_vertices) {
            atomicAdd(counter.counter, uint(-3));
            return;
        }

        vec3 vertex1 = triangleVertices[lookup_tables.data[edgeIndex * 16 + i * 3 + triangle_lookup_index]];
        vec3 vertex2 = triangleVertices[lookup_tables.data[edgeIndex * 16 + i * 3 + 1 + triangle_lookup_index]];
        vec3 vertex3 = triangleVertices[lookup_tables.data[edgeIndex * 16 + i * 3 + 2 + triangle_lookup_index]];

        vec3 normal1 = SampleTrilinear(vertex1, id);
        vec3 normal2 = SampleTrilinear(vertex2, id);
        vec3 normal3 = SampleTrilinear(vertex3, id);

        WriteVertex(count, vertex1, normal1);
        WriteVertex(count + 1, vertex2, normal2);
        WriteVertex(count + 2, vertex3, normal3);

        // Testing output
        // vec4 vertex1 = currVertices[0];
        // vertex1.x = float(edgeIndex);
        // vec4 vertex2 = currVertices[1];
        // vec4 vertex3 = currVertices[2];

        // WriteVertex(count, vertex1, vec4(0));
        // WriteVertex(count + 1, vertex2, vec4(0));
        // WriteVertex(count + 2, vertex3, vec4(0));
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