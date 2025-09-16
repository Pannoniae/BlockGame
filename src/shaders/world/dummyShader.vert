#version 440

#ifdef NV_EXTENSIONS
#extension GL_NV_gpu_shader5 : enable
#endif

#ifdef NV_COMMAND_LIST
#extension GL_NV_command_list : enable
#endif

#ifdef INSTANCED_RENDERING
#extension GL_ARB_shader_draw_parameters : enable
#endif

layout (location = 0) in uvec3 vPos;
layout (location = 1) in uvec2 texCoord;
layout (location = 2) in vec4 colour;
layout (location = 3) in uvec2 vLight;

#ifdef NV_COMMAND_LIST
layout (location = 4) in vec3 aChunkOffset;
#endif

uniform mat4 uMVP;

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

centroid out vec2 texCoords;
out vec4 tint;

const float m = 1 / 256.;

void main() {
    vec3 chunkOffset;
    
#ifdef INSTANCED_RENDERING
    #ifdef NV_COMMAND_LIST
        chunkOffset = aChunkOffset;
    #else
        chunkOffset = chunkPos[gl_BaseInstanceARB].xyz;
    #endif
#else
    chunkOffset = uChunkPos;
#endif

    vec3 pos = chunkOffset + ((vPos * m) - 16);
    gl_Position = uMVP * vec4(pos, 1.0);
    texCoords = texCoord / 32768.;
    
    tint = colour;
}