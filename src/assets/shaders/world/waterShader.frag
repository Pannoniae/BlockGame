#version 440 compatibility
#ifdef NV_COMMAND_LIST
#extension GL_NV_command_list : enable
#endif

//layout(commandBindableNV) uniform;

#include "/shaders/inc/fog.inc.glsl"
#include "/shaders/inc/dither.inc.glsl"
#include "/shaders/inc/af.inc.glsl"

layout(early_fragment_tests) in;
layout(location = 0) out vec4 colour;

#if AFFINE_MAPPING == 1
#ifdef NV_EXTENSIONS
noperspective centroid in vec2 affineCoords;
#else
noperspective in vec2 affineCoords;
#endif
centroid in vec2 texCoords;
in vec3 worldPos;
#else
centroid in vec2 texCoords;
#endif
in vec4 tint;
in float vertexDist;

uniform sampler2D blockTexture;
uniform vec3 uCameraPos;

void main() {
    // blend between affine and perspective UVs based on distance (clamp affine up close)
#if AFFINE_MAPPING == 1
    float dist = length(worldPos - uCameraPos);
    float affineBlend = smoothstep(1.0, 2.0, dist); // blend from 1 to 2 blocks
    vec2 finalCoords = mix(texCoords, affineCoords, affineBlend);
#else
    vec2 finalCoords = texCoords;
#endif

    vec4 blockColour;
    #if ANISO_LEVEL == 0
        // no anisotropic filtering, use regular texture lookup
        blockColour = texture(blockTexture, finalCoords);
    #else
        // use anisotropic filtering
        blockColour = textureAF(blockTexture, finalCoords);
    #endif
    float ratio = calculateFogFactor(vertexDist);
    
    // combine block color with lighting and base tint
    colour = vec4(blockColour.rgb * tint.rgb, blockColour.a);

    if (colour.a <= 0) {
        discard;
    }
    // mix the fog colour between it and the sky
    vec4 mixedFogColour = mix(fogColour, horizonColour, ratio);
    // mix fog
    colour = mix(colour, mixedFogColour, ratio);
    
    colour.rgb += gradientDither(colour.rgb);
}