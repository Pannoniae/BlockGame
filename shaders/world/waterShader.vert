#version 440

#ifdef NV_EXTENSIONS
#extension GL_NV_gpu_shader5 : enable
#extension GL_NV_command_list : enable
#endif

#extension GL_ARB_shader_draw_parameters : enable


layout (location = 0) in uvec3 vPos;
layout (location = 1) in uvec2 texCoord;
layout (location = 2) in vec4 colour;
layout (location = 3) in uint vLight;

#ifdef NV_COMMAND_LIST
layout (location = 4) in vec3 aChunkOffset;
#endif

uniform mat4 uMVP;
uniform vec3 uCameraPos;

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
out vec4 tint;
out float vertexDist;

uniform sampler2D lightTexture;

const float m = 1 / 256.;
const float n = 1 / 32768.;

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
    texCoords = texCoord * n;
    
    uint light = vLight & 0xFFu;
    
    // extract skylight and blocklight from packed light data (0-15)
    ivec2 lightCoords = ivec2((light >> 4) & 0xFu, light & 0xFu);
    vec4 lightColour = texelFetch(lightTexture, lightCoords, 0);
    
    tint = colour * lightColour;
    
    vertexDist = length(pos - uCameraPos);
}