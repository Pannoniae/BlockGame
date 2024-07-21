#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 outColour;

in vec2 texCoords;
in vec4 colour;

uniform sampler2D tex;

void main() {

    outColour = texture(tex, texCoords);
    if (colour.a <= 0) {
        discard;
    }
    outColour = outColour * colour;
}