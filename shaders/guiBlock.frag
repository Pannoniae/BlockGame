﻿#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 color;

in vec2 texCoords;
flat in uint direction;
in vec3 vertexPos;

uniform sampler2D blockTexture;

const float a[6] = float[6](0.8, 0.8, 0.6, 0.6, 0.6, 1);

void main() {
    // per-face lighting
    float lColor = a[direction];
    color = texture(blockTexture, texCoords);
    color = vec4(color.rgb * lColor, color.a);
    if (color.a <= 0) {
        discard;
    }
}