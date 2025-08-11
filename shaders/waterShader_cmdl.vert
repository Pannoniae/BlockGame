#version 460
#extension GL_NV_gpu_shader5 : enable
#extension GL_NV_command_list : enable

//layout(commandBindableNV) uniform;

layout (location = 0) in uvec3 vPos;
layout (location = 1) in uvec2 texCoord;
layout (location = 2) in vec4 colour;
layout (location = 3) in vec3 aChunkOffset;

uniform mat4 uMVP;
uniform vec3 uCameraPos;
uniform float uSkyDarken;

layout(std430, binding = 0) restrict readonly buffer ChunkPositions {
    vec4 chunkPos[];
};

struct buf {
    vec4 pos;
};

// UBO with pointer to chunkpos buffer
/*layout(std140, binding = 0) uniform ChunkPositions {
    buf* chunkPos;
};*/

//uniform vec4 *chunkPos;

out vec2 texCoords;
out float skyDarken;
out vec4 tint;
out float vertexDist;


void main() {
    // Use constant vertex attribute instead of gl_BaseInstance for NV_command_list compatibility
    vec3 chunkOffset = aChunkOffset;
    vec3 pos = chunkOffset + vPos / 256. - 16;
    gl_Position = uMVP * vec4(pos, 1.0);
    texCoords = texCoord / 32768.;
    // compute tint (light * ao * direction)
    // per-face lighting
    // float lColor = a[direction]
    skyDarken = uSkyDarken;
    tint = colour;
    vertexDist = length(pos - uCameraPos);
}