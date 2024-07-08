#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 color;

in vec2 texCoords;
in vec2 texOffset;

in vec4 tint;

in vec3 vertexPosFromCamera;


uniform int fogMax;
uniform int fogMin;

uniform sampler2D blockTexture;
uniform sampler2D lightTexture;

uniform vec4 fogColour;
uniform vec4 skyColour;

float getFog(float d) {
    // fog starts at 75% of drawdistance
    // clamp fog
    // also make it not linear
    float ratio = clamp(1.0 - (fogMax - d) / (fogMax - fogMin), 0.0, 1.0);
    return ratio;
}

void main() {

    vec4 blockColour = texture(blockTexture, texCoords);
    float ratio = getFog(length(vertexPosFromCamera));
    // extract skylight, 0 to 15

    color = vec4(blockColour.rgb * tint.rgb, blockColour.a);

    if (color.a <= 0) {
        discard;
    }
    // mix the fog colour between it and the sky
    vec4 mixedFogColour = mix(fogColour, skyColour, ratio);
    // mix fog
    color = mix(color, mixedFogColour, ratio);
}