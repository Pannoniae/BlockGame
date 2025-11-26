#version 440 compatibility

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 colour;

uniform sampler2D blockTexture;
uniform sampler2D lightTexture;

centroid in vec2 texCoords;
in vec4 tint;

void main() {
    vec4 blockColour;
    blockColour = texture(blockTexture, texCoords);
    
    colour = vec4(blockColour.rgb * tint.rgb, blockColour.a);
    
    
    // Traditional alpha test mode
    if (colour.a <= 0.0) {
        discard;
    }
    // make it always opaque (for mipmapping)
    colour.a = max(colour.a, 1);
}