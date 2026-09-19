#version 440 compatibility

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
#include "/shaders/inc/tc.inc.glsl"

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 colour;

#if AFFINE_MAPPING == 1
#ifdef NV_EXTENSIONS
noperspective TC_QUAL in vec2 affineCoords;
#else
noperspective in vec2 affineCoords;
#endif
TC_QUAL in vec2 texCoords;
in vec3 worldPos;
#else
TC_QUAL in vec2 texCoords;
#endif
in vec2 dTexCoords;
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
    
    vec2 ddx = dFdx(dTexCoords);
    vec2 ddy = dFdy(dTexCoords);
    
    vec4 blockColour;

#if ANISO_LEVEL == 0
    blockColour = textureGrad(blockTexture, finalCoords, ddx, ddy);
    colour = vec4(mix(blockColour.rgb, blockColour.rgb * lightColour.rgb * tint.rgb, blockColour.a), blockColour.a);
#else
    vec4 og = textureGrad(blockTexture, finalCoords, ddx, ddy);
    blockColour = textureAF(blockTexture, finalCoords, ddx, ddy);
    
    float mask = 1.0 - og.a;
    float finalAlpha = mix(blockColour.a, og.a, mask);
    colour = vec4(mix(og.rgb, blockColour.rgb * lightColour.rgb * tint.rgb, og.a), finalAlpha);
#endif

    
    if (colour.a <= 0.0) {
        discard;
    }
    colour.a = 1.0;

    float ratio = calculateFogFactor(vertexDist);
    vec4 mixedFogColour = mix(fogColour, horizonColour, ratio);
    colour.rgb = mix(colour.rgb, mixedFogColour.rgb, ratio);
    colour.rgb += gradientDither(colour.rgb);
}
