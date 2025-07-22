#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 outColour;

centroid in vec2 texCoords;
in vec4 colour;
in vec4 viewPosition; // Changed from fogDepth to view position

uniform vec4 fogColor;
uniform float fogStart;
uniform float fogEnd;
uniform bool fogEnabled;
uniform int fogType; // 0 = linear, 1 = exp, 2 = exp2
uniform float fogDensity; // For exp and exp2 fog

uniform sampler2D tex;

void main() {

    vec4 texColour = texture(tex, texCoords);
    if (colour.a <= 0) {
        discard;
    }
    vec4 mixColour = texColour * colour;
    if (fogEnabled) {
        // Calculate fogDepth in the fragment shader
        float fogDepth = length(viewPosition.xyz); // Use distance from camera instead of just z

        // Calculate fog factor based on fog type
        float fogFactor = 1.0;
        
        if (fogType == 0) {
            // Linear fog
            fogFactor = (fogEnd - fogDepth) / (fogEnd - fogStart);
        } else if (fogType == 1) {
            // Exponential fog
            fogFactor = exp(-fogDensity * fogDepth);
        } else if (fogType == 2) {
            // Exponential squared fog
            fogFactor = exp(-fogDensity * fogDensity * fogDepth * fogDepth);
        }
        
        fogFactor = clamp(fogFactor, 0.0, 1.0);

        // Mix original color with fog color
        outColour = mix(fogColor, mixColour, fogFactor);
    } else {
        outColour = mixColour;
    }
}