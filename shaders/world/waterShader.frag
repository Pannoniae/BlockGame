#version 440
#extension GL_NV_command_list : enable

//layout(commandBindableNV) uniform;

#include "/shaders/inc/fog.inc.glsl"

layout(early_fragment_tests) in;
layout(location = 0) out vec4 colour;

centroid in vec2 texCoords;
in vec4 tint;
in float vertexDist;

uniform sampler2D blockTexture;
uniform sampler2D lightTexture;


void main() {

    vec4 blockColour = texture(blockTexture, texCoords);
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
}