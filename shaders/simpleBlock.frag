﻿#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 color;

in vec2 texCoords;
flat in uint direction;

in vec3 vertexPos;

uniform sampler2D blockTexture;
uniform sampler2D lightTexture;

const float a[6] = float[6](0.8, 0.8, 0.6, 0.6, 0.6, 1);

void main() {
    // per-face lighting
    float lColor = a[direction];
    vec4 blockColour = texture(blockTexture, texCoords);
    color = vec4(blockColour.rgb * lColor, blockColour.a);
    if (color.a <= 0) {
        discard;
    }
}