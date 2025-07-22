#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 color;

centroid in vec2 texCoords;
in vec4 tint;

in vec3 vertexPos;

uniform sampler2D blockTexture;

void main() {
    vec4 blockColour = texture(blockTexture, texCoords);
    color = vec4(blockColour.rgb * tint.rgb, blockColour.a);
    if (color.a <= 0) {
        discard;
    }
    color.a = max(color.a, 1);
}