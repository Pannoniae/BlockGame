#version 440

layout(early_fragment_tests) in;
layout(location = 0) out vec4 color;

in vec2 texCoords;
flat in uint data;

uniform sampler2D blockTexture;

const float a[6] = float[6](0.8, 0.8, 0.6, 0.6, 0.6, 1);

void main() {
    // per-face lighting
    float lColor = a[data];
    color = texture(blockTexture, texCoords) * vec4(lColor, lColor, lColor, 1.0);
    if (color.a < 0.005) {
        discard;
    }
}