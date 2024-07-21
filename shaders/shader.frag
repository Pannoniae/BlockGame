#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 colour;

in vec2 texCoords;
in vec2 texOffset;

in vec4 tint;

in float vertexDist;


uniform int fogMax;
uniform int fogMin;

uniform sampler2D blockTexture;
uniform sampler2D lightTexture;

uniform vec4 fogColour;
uniform vec4 skyColour;

float getFog(float d) {
    // fog starts at 75% of drawdistance
    // clamp fog
    // also make it not linear
    float ratio = clamp((d - fogMin) / (fogMax - fogMin), 0.0, 1.0);
    return ratio;
}

void main() {

    vec4 blockColour = texture(blockTexture, texCoords);
    float ratio = getFog(vertexDist);
    // extract skylight, 0 to 15

    colour = vec4(blockColour.rgb * tint.rgb, blockColour.a);

    if (colour.a <= 0) {
        discard;
    }
    // make it always opaque (for mipmapping)
    colour.a = max(colour.a, 1);

    // mix the fog colour between it and the sky
    vec4 mixedFogColour = mix(fogColour, skyColour, ratio);
    // mix fog
    colour = mix(colour, mixedFogColour, ratio);
}