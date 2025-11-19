#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 colour;

centroid in vec2 texCoords;
in vec4 tint;
in vec4 lightColour;

uniform sampler2D blockTexture;

void main() {
    vec4 blockColour = texture(blockTexture, texCoords);
    colour = vec4(mix(blockColour.rgb, blockColour.rgb * lightColour.rgb * tint.rgb, blockColour.a), blockColour.a);
    if (colour.a <= 0) {
        discard;
    }
    colour.a = max(colour.a, 1);
}