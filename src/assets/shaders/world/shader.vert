#version 440 compatibility

#ifdef NV_EXTENSIONS
#extension GL_NV_gpu_shader5 : enable
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


// on AMD / intel nopersp centroid shits itself (texture sampling goes completely haywire, looks like it's dividing by zero or some shit, different tiles get different, completely nonsensical temporally artifacting texcoords, most likely reading garbage memory..)
// best we can do is force non-centroid, this will produce the edge "lines" with anisotropic *and* MSAA *and* affine mapping enabled
// but that's also a very unlikely schizoid combo so let's not worry about that. although AMD plz fix...

// note for debugging: this only happens when the normal and nopersp texcoords are mix()'ed together, it works separately. we're probably enabling some different mode when compiling the shader with that..
// note: identity mix also works (affine, affine), it needs to be a different value
#if AFFINE_MAPPING == 1
#ifdef NV_EXTENSIONS
noperspective centroid out vec2 affineCoords;
#else
noperspective out vec2 affineCoords;
#endif
centroid out vec2 texCoords; // perspective-correct
out vec3 worldPos;
#else
centroid out vec2 texCoords;
#endif
out vec4 tint;
out vec4 lightColour;
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

#if VERTEX_JITTER == 1
    vec4 snap = gl_Position;
    snap.xyz = snap.xyz / snap.w; // persp
    snap.xy = floor(snap.xy * 160.0) / 160.0; // snap to virtual 160p
    snap.xyz *= snap.w; // undo persp
    gl_Position = snap;
#endif

    texCoords = texCoord * n;
#if AFFINE_MAPPING == 1
    affineCoords = texCoords;
    worldPos = pos;
#endif

    uint light = vLight.x & 0xFFu;
    
    // extract skylight and blocklight from packed light data (0-15)
    ivec2 lightCoords = ivec2((light >> 4) & 0xFu, light & 0xFu);
    lightColour = texelFetch(lightTexture, lightCoords, 0);
    
    tint = colour;
    
    vertexDist = length(pos - uCameraPos);
}