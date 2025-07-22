#version 440

// don't, glass will be fucked
layout(early_fragment_tests) in;
layout(location = 0) out vec4 color;

centroid in vec2 texCoords;

uniform sampler2D blockTexture;

void main() {
    color = texture(blockTexture, texCoords);
}