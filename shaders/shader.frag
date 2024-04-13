#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 color;

in vec2 texCoords;
flat in uint direction;
in float ao;
in vec3 vertexPos;

uniform vec3 uCameraPos;
uniform int drawDistance;

uniform sampler2D blockTexture;

const float a[6] = float[6](0.8, 0.8, 0.6, 0.6, 0.6, 1);
uniform vec4 fogColour;

float getFog(float d) {
    float fogMax = drawDistance - 32.0;
    float fogMin = drawDistance - 48.0;
    if (d >= fogMax) return 1.0;
    if (d <= fogMin) return 0.0;
    return 1.0 - (fogMax - d) / (fogMax - fogMin);
}

void main() {
    // per-face lighting
    float lColor = a[direction];
    vec4 blockColour = texture(blockTexture, texCoords);
    float ratio = getFog(distance(uCameraPos, vertexPos));
    // mix fog
    color = vec4(blockColour.rgb * lColor * ao, blockColour.a);
    color = mix(color, fogColour, ratio);
    if (color.a < 0.005) {
        discard;
    }
}