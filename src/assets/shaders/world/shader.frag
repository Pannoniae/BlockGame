#version 440

#extension GL_ARB_texture_query_lod : enable
#extension GL_EXT_gpu_shader4 : enable

#ifdef NV_COMMAND_LIST
#extension GL_NV_command_list : enable
#endif

#ifdef NV_COMMAND_LIST
layout(commandBindableNV) uniform;
#endif

#include "/shaders/inc/fog.inc.glsl"
#include "/shaders/inc/dither.inc.glsl"
#include "/shaders/inc/af.inc.glsl"

// don't, glass will be fucked
//layout(early_fragment_tests) in;
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
in vec4 lightColour;
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
    
    float ratio = calculateFogFactor(vertexDist);
    
    // combine with lightColour, 1 = unlit, 0 = fully lit based on alpha
    colour = vec4(mix(blockColour.rgb, blockColour.rgb * lightColour.rgb * tint.rgb, blockColour.a), blockColour.a);
#else
    // use anisotropic filtering
    vec4 og = texture(blockTexture, finalCoords);
    blockColour = textureAF(blockTexture, finalCoords);
    
    float ratio = calculateFogFactor(vertexDist);

    // combine with lightColour, 1 = unlit, 0 = fully lit based on alpha
    // use og.a for emissive pixels (no AF bleed), blockColour.a for lit pixels (smooth edges)
    float mask = 1.0 - og.a;
    float finalAlpha = mix(blockColour.a, og.a, mask);
    colour = vec4(mix(og.rgb, blockColour.rgb * lightColour.rgb * tint.rgb, og.a), finalAlpha);
#endif

#if ALPHA_TO_COVERAGE == 1
    // A2C mode: let fragments through with their alpha values for coverage conversion
    // Apply fog to RGB only, preserve alpha for coverage conversion
    vec4 mixedFogColour = mix(fogColour, horizonColour, ratio);
    colour.rgb = mix(colour.rgb, mixedFogColour.rgb, ratio);
    
    //colour.a = (colour.a - 0.01) / max(fwidth(colour.a), 0.0001) + 0.5;
    
    // Alpha stays unchanged for A2C
#else
    // Traditional alpha test mode
    if (colour.a <= 0.0) {
        discard;
    }
    // make it always opaque (for mipmapping)
    colour.a = max(colour.a, 1);
    
    // mix the fog colour between it and the sky
    vec4 mixedFogColour = mix(fogColour, horizonColour, ratio);
    colour = mix(colour, mixedFogColour, ratio);
    
    colour.rgb += gradientDither(colour.rgb);
#endif
}