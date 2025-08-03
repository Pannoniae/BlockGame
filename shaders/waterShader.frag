#version 440

#include "inc/fog.inc"

layout(early_fragment_tests) in;
layout(location = 0) out vec4 colour;

centroid in vec2 texCoords;
flat in int skyDarken;
in vec4 tint;
in float vertexDist;

uniform sampler2D blockTexture;

void main() {

    vec4 blockColour = texture(blockTexture, texCoords);
    float ratio = getFog(vertexDist);
    // extract skylight, 0 to 15

    // apply skyDarken - reduce lighting based on day/night cycle
    float darkenFactor = 1.0 - (skyDarken.x / 15.0);
    vec3 darkenedTint = tint.rgb * darkenFactor;
    colour = vec4(blockColour.rgb * darkenedTint, blockColour.a);

    if (colour.a <= 0) {
        discard;
    }
    // mix the fog colour between it and the sky
    vec4 mixedFogColour = mix(fogColour, skyColour, ratio);
    // mix fog
    colour = mix(colour, mixedFogColour, ratio);
}