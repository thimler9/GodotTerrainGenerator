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

const float EPSILON = 1.0;

void WriteVertex(uint index, vec3 position, vec3 normal) {
    vertex_buffer.vertex[index].position = vec4(position, 1.0);
    vertex_buffer.vertex[index].normal = vec4(normal, 1.0);
}

vec3 SampleTrilinear(vec3 position) {
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

        vec3 vertex1 = triangleVertices[lookup_tables.data[edgeIndex * 16 + i * 3 + 2 + triangle_lookup_index]];
        vec3 vertex2 = triangleVertices[lookup_tables.data[edgeIndex * 16 + i * 3 + 1 + triangle_lookup_index]];
        vec3 vertex3 = triangleVertices[lookup_tables.data[edgeIndex * 16 + i * 3 + triangle_lookup_index]];

        vec3 normal1 = SampleTrilinear(vertex1);
        vec3 normal2 = SampleTrilinear(vertex2);
        vec3 normal3 = SampleTrilinear(vertex3);

        WriteVertex(count, vertex1, normal1);
        WriteVertex(count + 1, vertex2, normal2);
        WriteVertex(count + 2, vertex3, normal3);
    }
}

const float EPISILON = 0.001;

void MakeTransitionTriangles(vec4 currVertices[13], uint windingNumber)
{
    uint caseIndexVertexValue[9] = { 0x01, 0x02, 0x04, 0x80, 0x100, 0x08, 0x40, 0x20, 0x10 };
    
    //The equivalance class is the sum of the vertices that have negative map value
    uint equClassSum = 0;
    for (uint i = 0; i < 9; i++)
    {
        equClassSum = currVertices[i].w < 0 ? equClassSum + caseIndexVertexValue[i] : equClassSum;
    }

    vec3 triangleVertices[16];
    for (uint j = 0; j < 16; j++)
    {
        vec4 voxel0 = currVertices[lookup_tables.data[j * 2 + transition_edge_to_vertices_index]];
        vec4 voxel1 = currVertices[lookup_tables.data[j * 2 + 1 + transition_edge_to_vertices_index]];

        float t = voxel0.w / (voxel0.w - voxel1.w);
        triangleVertices[j] = (voxel0 * (1 - t) + voxel1 * t).xyz;
    }

    //Used to get numTri and the winding constant
    uint equClass = lookup_tables.data[equClassSum + transition_cell_class_index];

    //This gets the first 6 bits which indicate the index in the numTri array
    uint numTriangles = lookup_tables.data[(equClass & 0x3f) + transition_cell_num_tri_index];
    uint windInReverse = equClass >> 7;

    //The triangles wind in reverse if the high bit is set to 0
    vec3 vertex1;
    vec3 vertex2;
    vec3 vertex3;
    for (uint k = 0; k < numTriangles; k++)
    {
        uint count = atomicAdd(counter.counter, 3u);
        if (count >= params.max_num_vertices) {
            atomicAdd(counter.counter, uint(-3));
            return;
        }

        int index0 = lookup_tables.data[equClassSum * 36 + k * 3 + transition_vertex_data_index];
        int index1 = lookup_tables.data[equClassSum * 36 + k * 3 + 1 + transition_vertex_data_index];
        int index2 = lookup_tables.data[equClassSum * 36 + k * 3 + 2 + transition_vertex_data_index];

        vec3 vertex1 = windInReverse == windingNumber ? triangleVertices[index0] : triangleVertices[index2];
        vec3 vertex2 = windInReverse == windingNumber ? triangleVertices[index1] : triangleVertices[index1];
        vec3 vertex3 = windInReverse == windingNumber ? triangleVertices[index2] : triangleVertices[index0];

        vec3 normal1 = SampleTrilinear(vertex1);
        vec3 normal2 = SampleTrilinear(vertex2);
        vec3 normal3 = SampleTrilinear(vertex3);

        WriteVertex(count, vertex1, normal1);
        WriteVertex(count + 1, vertex2, normal2);
        WriteVertex(count + 2, vertex3, normal3);
    }
}


//The following are different functions that generate the different side transition vertices.
//The only different between them are the offsets in the "currVertices" array and the winding numbers
//Epsilon is used to differentiate the border voxels to the main mesh voxels in the vertex shader
void ETransitionVoxels(uvec3 id)
{
    uint lod = params.lod;
    uint chunkSize = params.chunk_size;
    float width = params.transition_width * lod;

    uint maxX = chunkSize / lod * lod;
    uint tempY = id.y * lod * 2;
    uint tempZ = id.z * lod * 2;

    id.y *= 2;
    id.z *= 2;
    
    uint adjustedSize = chunkSize / lod + 2; //After checking if it's in the chunk, we need to convert it
        //to the size of the noise array since that's where we get the data points from
	
    //Points on the voxel
    //The first 9 are taken from the the current chunk
    //The last 4 are the new ones for the transition triangles
    vec4 currVertices[13] =
    {
        { maxX + EPSILON, tempY, tempZ, sdf_buffer.data[(adjustedSize - 2) + id.y * adjustedSize + id.z * adjustedSize * adjustedSize] },
        { maxX + EPSILON, tempY, tempZ + lod, sdf_buffer.data[(adjustedSize - 2) + id.y * adjustedSize + (id.z + 1) * adjustedSize * adjustedSize] },
        { maxX + EPSILON, tempY, tempZ + lod * 2, sdf_buffer.data[(adjustedSize - 2) + id.y * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] },
        { maxX + EPSILON, tempY + lod, tempZ, sdf_buffer.data[(adjustedSize - 2) + (id.y + 1) * adjustedSize + id.z * adjustedSize * adjustedSize] },
        { maxX + EPSILON, tempY + lod, tempZ + lod, sdf_buffer.data[(adjustedSize - 2) + (id.y + 1) * adjustedSize + (id.z + 1) * adjustedSize * adjustedSize] },
        { maxX + EPSILON, tempY + lod, tempZ + lod * 2, sdf_buffer.data[(adjustedSize - 2) + (id.y + 1) * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] },
        { maxX + EPSILON, tempY + lod * 2, tempZ, sdf_buffer.data[(adjustedSize - 2) + (id.y + 2) * adjustedSize + id.z * adjustedSize * adjustedSize] },
        { maxX + EPSILON, tempY + lod * 2, tempZ + lod, sdf_buffer.data[(adjustedSize - 2) + (id.y + 2) * adjustedSize + (id.z + 1) * adjustedSize * adjustedSize] },
        { maxX + EPSILON, tempY + lod * 2, tempZ + lod * 2, sdf_buffer.data[(adjustedSize - 2) + (id.y + 2) * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] },

        { maxX + width + EPSILON, tempY, tempZ, sdf_buffer.data[(adjustedSize - 2) + id.y * adjustedSize + id.z * adjustedSize * adjustedSize] },
        { maxX + width + EPSILON, tempY, tempZ + lod * 2, sdf_buffer.data[(adjustedSize - 2) + id.y * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] },
        { maxX + width + EPSILON, tempY + lod * 2, tempZ, sdf_buffer.data[(adjustedSize - 2) + (id.y + 2) * adjustedSize + id.z * adjustedSize * adjustedSize] },
        { maxX + width + EPSILON, tempY + lod * 2, tempZ + lod * 2, sdf_buffer.data[(adjustedSize - 2) + (id.y + 2) * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] }
    };

    MakeTransitionTriangles(currVertices, 1);    
}

void WTransitionVoxels(uvec3 id)
{
    uint lod = params.lod;
    uint size = params.chunk_size;
    float width = params.transition_width * lod;

    uint tempY = id.y * lod * 2;
    uint tempZ = id.z * lod * 2;

    id.y *= 2;
    id.z *= 2;

    uint adjustedSize = size / lod + 2;

    vec4 currVertices[13] =
    {
        { -EPSILON, tempY, tempZ, sdf_buffer.data[id.y * adjustedSize + id.z * adjustedSize * adjustedSize] },
        { -EPSILON, tempY, tempZ + lod, sdf_buffer.data[id.y * adjustedSize + (id.z + 1) * adjustedSize * adjustedSize] },
        { -EPSILON, tempY, tempZ + lod * 2, sdf_buffer.data[id.y * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] },
        { -EPSILON, tempY + lod, tempZ, sdf_buffer.data[(id.y + 1) * adjustedSize + id.z * adjustedSize * adjustedSize] },
        { -EPSILON, tempY + lod, tempZ + lod, sdf_buffer.data[(id.y + 1) * adjustedSize + (id.z + 1) * adjustedSize * adjustedSize] },
        { -EPSILON, tempY + lod, tempZ + lod * 2, sdf_buffer.data[(id.y + 1) * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] },
        { -EPSILON, tempY + lod * 2, tempZ, sdf_buffer.data[(id.y + 2) * adjustedSize + id.z * adjustedSize * adjustedSize] },
        { -EPSILON, tempY + lod * 2, tempZ + lod, sdf_buffer.data[(id.y + 2) * adjustedSize + (id.z + 1) * adjustedSize * adjustedSize] },
        { -EPSILON, tempY + lod * 2, tempZ + lod * 2, sdf_buffer.data[(id.y + 2) * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] },

        { -width - EPSILON, tempY, tempZ, sdf_buffer.data[0 + id.y * adjustedSize + id.z * adjustedSize * adjustedSize] },
        { -width - EPSILON, tempY, tempZ + lod * 2, sdf_buffer.data[0 + id.y * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] },
        { -width - EPSILON, tempY + lod * 2, tempZ, sdf_buffer.data[0 + (id.y + 2) * adjustedSize + id.z * adjustedSize * adjustedSize] },
        { -width - EPSILON, tempY + lod * 2, tempZ + lod * 2, sdf_buffer.data[0 + (id.y + 2) * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] }
    };

    MakeTransitionTriangles(currVertices, 0);
}

void NTransitionVoxels(uvec3 id)
{
    uint lod = params.lod;
    uint size = params.chunk_size;
    float width = params.transition_width * lod;
    
    uint maxZ = size / lod * lod;
    uint tempY = id.y * lod * 2;
    uint tempX = id.x * lod * 2;

    id.y *= 2;
    id.x *= 2;

    uint adjustedSize = size / lod + 2;

    vec4 currVertices[13] =
    {
        { tempX, tempY, maxZ + EPSILON, sdf_buffer.data[id.x + id.y * adjustedSize + (adjustedSize - 2) * adjustedSize * adjustedSize] },
        { tempX + lod, tempY, maxZ + EPSILON, sdf_buffer.data[id.x + 1 + id.y * adjustedSize + (adjustedSize - 2) * adjustedSize * adjustedSize] },
        { tempX + lod * 2, tempY, maxZ + EPSILON, sdf_buffer.data[id.x + 2 + id.y * adjustedSize + (adjustedSize - 2) * adjustedSize * adjustedSize] },
        { tempX, tempY + lod, maxZ + EPSILON, sdf_buffer.data[id.x + (id.y + 1) * adjustedSize + (adjustedSize - 2) * adjustedSize * adjustedSize] },
        { tempX + lod, tempY + lod, maxZ + EPSILON, sdf_buffer.data[id.x + 1 + (id.y + 1) * adjustedSize + (adjustedSize - 2) * adjustedSize * adjustedSize] },
        { tempX + lod * 2, tempY + lod, maxZ + EPSILON, sdf_buffer.data[id.x + 2 + (id.y + 1) * adjustedSize + (adjustedSize - 2) * adjustedSize * adjustedSize] },
        { tempX, tempY + lod * 2, maxZ + EPSILON, sdf_buffer.data[id.x + (id.y + 2) * adjustedSize + (adjustedSize - 2) * adjustedSize * adjustedSize] },
        { tempX + lod, tempY + lod * 2, maxZ + EPSILON, sdf_buffer.data[id.x + 1 + (id.y + 2) * adjustedSize + (adjustedSize - 2) * adjustedSize * adjustedSize] },
        { tempX + lod * 2, tempY + lod * 2, maxZ + EPSILON, sdf_buffer.data[id.x + 2 + (id.y + 2) * adjustedSize + (adjustedSize - 2) * adjustedSize * adjustedSize] },

        { tempX, tempY, maxZ + width + EPSILON, sdf_buffer.data[id.x + id.y * adjustedSize + (adjustedSize - 2) * adjustedSize * adjustedSize] },
        { tempX + lod * 2, tempY, maxZ + width + EPSILON, sdf_buffer.data[id.x + 2 + id.y * adjustedSize + (adjustedSize - 2) * adjustedSize * adjustedSize] },
        { tempX, tempY + lod * 2, maxZ + width + EPSILON, sdf_buffer.data[id.x + (id.y + 2) * adjustedSize + (adjustedSize - 2) * adjustedSize * adjustedSize] },
        { tempX + lod * 2, tempY + lod * 2, maxZ + width + EPSILON, sdf_buffer.data[id.x + 2 + (id.y + 2) * adjustedSize + (adjustedSize - 2) * adjustedSize * adjustedSize] }
    };

    MakeTransitionTriangles(currVertices, 0);
}

void STransitionVoxels(uvec3 id)
{
    uint lod = params.lod;
    uint size = params.chunk_size;
    float width = params.transition_width * lod;

    uint tempY = id.y * lod * 2;
    uint tempX = id.x * lod * 2;

    id.y *= 2;
    id.x *= 2;

    uint adjustedSize = size / lod + 2;

    vec4 currVertices[13] =
    {
        { tempX, tempY, -EPSILON, sdf_buffer.data[id.x + id.y * adjustedSize] },
        { tempX + lod, tempY, -EPSILON, sdf_buffer.data[id.x + 1 + id.y * adjustedSize] },
        { tempX + lod * 2, tempY, -EPSILON, sdf_buffer.data[id.x + 2 + id.y * adjustedSize] },
        { tempX, tempY + lod, -EPSILON, sdf_buffer.data[id.x + (id.y + 1) * adjustedSize] },
        { tempX + lod, tempY + lod, -EPSILON, sdf_buffer.data[id.x + 1 + (id.y + 1) * adjustedSize] },
        { tempX + lod * 2, tempY + lod, -EPSILON, sdf_buffer.data[id.x + 2 + (id.y + 1) * adjustedSize] },
        { tempX, tempY + lod * 2, -EPSILON, sdf_buffer.data[id.x + (id.y + 2) * adjustedSize] },
        { tempX + lod, tempY + lod * 2, -EPSILON, sdf_buffer.data[id.x + 1 + (id.y + 2) * adjustedSize] },
        { tempX + lod * 2, tempY + lod * 2, -EPSILON, sdf_buffer.data[id.x + 2 + (id.y + 2) * adjustedSize] },

        { tempX, tempY, -width - EPSILON, sdf_buffer.data[id.x + id.y * adjustedSize] },
        { tempX + lod * 2, tempY, -width - EPSILON, sdf_buffer.data[id.x + 2 + id.y * adjustedSize] },
        { tempX, tempY + lod * 2, -width - EPSILON, sdf_buffer.data[id.x + (id.y + 2) * adjustedSize] },
        { tempX + lod * 2, tempY + lod * 2, -width - EPSILON, sdf_buffer.data[id.x + 2 + (id.y + 2) * adjustedSize] }
    };

    MakeTransitionTriangles(currVertices, 1);
}

void TTransitionVoxels(uvec3 id)
{
    uint lod = params.lod;
    uint size = params.chunk_size;
    float width = params.transition_width * lod;

    uint maxY = size / lod * lod;
    uint tempX = id.x * lod * 2;
    uint tempZ = id.z * lod * 2;

    id.x *= 2;
    id.z *= 2;

    uint adjustedSize = size / lod + 2;

    vec4 currVertices[13] =
    {
        { tempX, maxY + EPSILON, tempZ, sdf_buffer.data[id.x + (adjustedSize - 2) * adjustedSize + id.z * adjustedSize * adjustedSize] },
        { tempX + lod, maxY + EPSILON, tempZ, sdf_buffer.data[(id.x + 1) + (adjustedSize - 2) * adjustedSize + (id.z) * adjustedSize * adjustedSize] },
        { tempX + lod * 2, maxY + EPSILON, tempZ, sdf_buffer.data[(id.x + 2) + (adjustedSize - 2) * adjustedSize + (id.z) * adjustedSize * adjustedSize] },
        { tempX, maxY + EPSILON, tempZ + lod, sdf_buffer.data[id.x + (adjustedSize - 2) * adjustedSize + (id.z + 1) * adjustedSize * adjustedSize] },
        { tempX + lod, maxY + EPSILON, tempZ + lod, sdf_buffer.data[id.x + 1 + (adjustedSize - 2) * adjustedSize + (id.z + 1) * adjustedSize * adjustedSize] },
        { tempX + lod * 2, maxY + EPSILON, tempZ + lod, sdf_buffer.data[id.x + 2 + (adjustedSize - 2) * adjustedSize + (id.z + 1) * adjustedSize * adjustedSize] },
        { tempX, maxY + EPSILON, tempZ + lod * 2, sdf_buffer.data[id.x + (adjustedSize - 2) * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] },
        { tempX + lod, maxY + EPSILON, tempZ + lod * 2, sdf_buffer.data[id.x + 1 + (adjustedSize - 2) * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] },
        { tempX + lod * 2, maxY + EPSILON, tempZ + lod * 2, sdf_buffer.data[id.x + 2 + (adjustedSize - 2) * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] },

        { tempX, maxY + width + EPSILON, tempZ, sdf_buffer.data[id.x + (adjustedSize - 2) * adjustedSize + id.z * adjustedSize * adjustedSize] },
        { tempX + lod * 2, maxY + width + EPSILON, tempZ, sdf_buffer.data[(id.x + 2) + (adjustedSize - 2) * adjustedSize + id.z * adjustedSize * adjustedSize] },
        { tempX, maxY + width + EPSILON, tempZ + lod * 2, sdf_buffer.data[id.x + (adjustedSize - 2) * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] },
        { tempX + lod * 2, maxY + width + EPSILON, tempZ + lod * 2, sdf_buffer.data[(id.x + 2) + (adjustedSize - 2) * adjustedSize + (id.z + 2) * adjustedSize * adjustedSize] }
    };

    MakeTransitionTriangles(currVertices, 1);
}

void BTransitionVoxels(uvec3 id)
{
    uint lod = params.lod;
    uint size = params.chunk_size;
    float width = params.transition_width * lod;

    uint tempX = id.x * lod * 2;
    uint tempZ = id.z * lod * 2;

    id.x *= 2;
    id.z *= 2;

    uint adjustedSize = size / lod + 2;

    vec4 currVertices[13] =
    {
        { tempX, -EPSILON, tempZ, sdf_buffer.data[id.x + id.z * adjustedSize * adjustedSize] },
        { tempX + lod, -EPSILON, tempZ, sdf_buffer.data[id.x + 1 + id.z * adjustedSize * adjustedSize] },
        { tempX + lod * 2, -EPSILON, tempZ, sdf_buffer.data[id.x + 2 + id.z * adjustedSize * adjustedSize] },
        { tempX, -EPSILON, tempZ + lod, sdf_buffer.data[id.x + (id.z + 1) * adjustedSize * adjustedSize] },
        { tempX + lod, -EPSILON, tempZ + lod, sdf_buffer.data[id.x + 1 + (id.z + 1) * adjustedSize * adjustedSize] },
        { tempX + lod * 2, -EPSILON, tempZ + lod, sdf_buffer.data[id.x + 2 + (id.z + 1) * adjustedSize * adjustedSize] },
        { tempX, -EPSILON, tempZ + lod * 2, sdf_buffer.data[id.x + (id.z + 2) * adjustedSize * adjustedSize] },
        { tempX + lod, -EPSILON, tempZ + lod * 2, sdf_buffer.data[id.x + 1 + (id.z + 2) * adjustedSize * adjustedSize] },
        { tempX + lod * 2, -EPSILON, tempZ + lod * 2, sdf_buffer.data[id.x + 2 + (id.z + 2) * adjustedSize * adjustedSize] },

        { tempX, -width - EPSILON, tempZ, sdf_buffer.data[id.x + id.z * adjustedSize * adjustedSize] },
        { tempX + lod * 2, -width - EPSILON, tempZ, sdf_buffer.data[id.x + 2 + id.z * adjustedSize * adjustedSize] },
        { tempX, -width - EPSILON, tempZ + lod * 2, sdf_buffer.data[id.x + (id.z + 2) * adjustedSize * adjustedSize] },
        { tempX + lod * 2, -width - EPSILON, tempZ + lod * 2, sdf_buffer.data[id.x + 2 + (id.z + 2) * adjustedSize * adjustedSize] }
    };

    MakeTransitionTriangles(currVertices, 0);
}

void main() {
    uint adjusted_size = params.chunk_size / params.lod;
    uvec3 id = gl_GlobalInvocationID.xyz;
    if (id.x >= adjusted_size || id.y >= adjusted_size || id.z >= adjusted_size) {
        return;
    }

    // Create the main voxel mesh using marching cubes
    MainVoxels(id);

    if (id.x * 2 < adjusted_size && id.y * 2 < adjusted_size) {
        NTransitionVoxels(uvec3(id.x, id.y, 1));
        STransitionVoxels(uvec3(id.x, id.y, 1));
        return;
    }

    if (id.x * 2 < adjusted_size && id.z * 2 < adjusted_size) {
        TTransitionVoxels(uvec3(id.x, 1, id.z));
        BTransitionVoxels(uvec3(id.x, 1, id.z));
        return;
    }

    if (id.y * 2 < adjusted_size && id.z * 2 < adjusted_size) {
        ETransitionVoxels(uvec3(1, id.y, id.z));
        WTransitionVoxels(uvec3(1, id.y, id.z));
        return;
    }
}