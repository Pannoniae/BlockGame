#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 outColour;

in vec4 colour;
in vec4 viewPosition; // Changed from fogDepth to view position

uniform vec4 fogColor;
uniform float fogStart;
uniform float fogEnd;
uniform bool fogEnabled;

void main() {
    if (colour.a <= 0) {
        discard;
    }

    if (fogEnabled) {
        // Calculate fogDepth in the fragment shader
        float fogDepth = length(viewPosition.xyz); // Use distance from camera instead of just z

        // Calculate linear fog factor
        float fogFactor = (fogEnd - fogDepth) / (fogEnd - fogStart);
        fogFactor = clamp(fogFactor, 0.0, 1.0);

        // Mix original color with fog color
        outColour = mix(fogColor, colour, fogFactor);
    } else {
        outColour = colour;
    }
}