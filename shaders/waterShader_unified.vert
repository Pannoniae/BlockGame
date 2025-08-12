#version 440

#ifdef NV_EXTENSIONS
#extension GL_NV_gpu_shader5 : enable
#extension GL_NV_command_list : enable
#endif

#extension GL_ARB_shader_draw_parameters : enable


layout (location = 0) in uvec3 vPos;
layout (location = 1) in uvec2 texCoord;
layout (location = 2) in vec4 colour;

#ifdef INSTANCED_RENDERING
    #ifdef NV_COMMAND_LIST
    layout (location = 3) in vec3 aChunkOffset;
    #endif
#endif

uniform mat4 uMVP;
uniform vec3 uCameraPos;
uniform float uSkyDarken;

#ifndef INSTANCED_RENDERING
uniform vec3 uChunkPos;
#endif

#ifdef INSTANCED_RENDERING
    #ifndef NV_COMMAND_LIST
    layout(std430, binding = 0) restrict readonly buffer ChunkPositions {
        vec4 chunkPos[];
    };
    #endif
#endif

out vec2 texCoords;
out float skyDarken;
out vec4 tint;
out float vertexDist;

void main() {
    vec3 chunkOffset;
    
#ifdef INSTANCED_RENDERING
    #ifdef NV_COMMAND_LIST
        chunkOffset = aChunkOffset;
    #else
        chunkOffset = chunkPos[gl_BaseInstance].xyz;
    #endif
#else
    chunkOffset = uChunkPos;
#endif

    vec3 pos = chunkOffset + vPos / 256. - 16;
    gl_Position = uMVP * vec4(pos, 1.0);
    texCoords = texCoord / 32768.;
    skyDarken = uSkyDarken;
    tint = colour;
    vertexDist = length(pos - uCameraPos);
}