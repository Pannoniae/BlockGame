#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 color;

in vec2 texCoords;
in vec2 texOffset;

flat in uint direction;
in float ao;
in vec4 light;

in vec3 vertexPos;
in vec3 vertexPosFromCamera;


uniform int drawDistance;

uniform sampler2D blockTexture;
uniform sampler2D lightTexture;

const float a[6] = float[6](0.8, 0.8, 0.6, 0.6, 0.6, 1);
uniform vec4 fogColour;
uniform vec4 skyColour;

float getFog(float d) {
    float fogMax = drawDistance - 16.0;
    // fog starts at 75% of drawdistance
    float fogMin = drawDistance * 0.5;
    // clamp fog
    // also make it not linear
    float ratio = clamp(1.0 - (fogMax - d) / (fogMax - fogMin), 0.0, 1.0);
    return ratio;
}

void main() {
    // per-face lighting
    float lColor = a[direction];
    vec4 blockColour = texture(blockTexture, texCoords);
    float ratio = getFog(length(vertexPosFromCamera));
    // extract skylight, 0 to 15

    color = vec4(blockColour.rgb * lColor * ao * light.rgb, blockColour.a);

    if (color.a <= 0) {
        discard;
    }
    // mix the fog colour between it and the sky
    vec4 mixedFogColour = mix(fogColour, skyColour, ratio);
    // mix fog
    color = mix(color, mixedFogColour, ratio);
}