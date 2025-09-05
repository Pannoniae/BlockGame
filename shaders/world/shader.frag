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
#include "/shaders/inc/af.inc.glsl"

//#if not defined(ANISO_LEVEL)
//#define ANISO_LEVEL 0
//#endif




// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 colour;

centroid in vec2 texCoords;
in vec4 tint;
in float vertexDist;

uniform sampler2D blockTexture;
uniform sampler2D lightTexture;

void main() {
    vec4 blockColour;
#if ANISO_LEVEL == 0
    // no anisotropic filtering, use regular texture lookup
    blockColour = texture(blockTexture, texCoords);
#else
    // use anisotropic filtering
    blockColour = textureAF(blockTexture, texCoords);
#endif
    float ratio = calculateFogFactor(vertexDist);
    
    // combine block color with lighting and base tint
    colour = vec4(blockColour.rgb * tint.rgb, blockColour.a);

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
#endif
}