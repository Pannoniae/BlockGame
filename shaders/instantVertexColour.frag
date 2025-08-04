#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 outColour;

in vec4 colour;
in vec4 viewPosition;

uniform vec4 fogColor;
uniform float fogStart;
uniform float fogEnd;
uniform bool fogEnabled;
uniform int fogType; // 0 = linear, 1 = exp, 2 = exp2
uniform float fogDensity; // For exp and exp2 fog

float dither(vec2 coord) {
    // use a simple 4x4 Bayer matrix pattern
    int x = int(coord.x) % 4;
    int y = int(coord.y) % 4;
    
    const float bayerMatrix[16] = float[16](
        -0.5, 0.0, -0.375, 0.125,
        0.25, -0.25, 0.375, -0.125,
        -0.3125, 0.1875, -0.4375, 0.0625,
        0.4375, -0.0625, 0.3125, -0.1875
    );
    
    return bayerMatrix[y * 4 + x] / 255.0; // scale to single RGB colour step
}

void main() {
    if (colour.a <= 0) {
        discard;
    }

    vec4 finalColour = colour;

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
        finalColour = mix(fogColor, colour, fogFactor);
    }
    
    // Apply dithering to reduce banding in dark colors
    float ditherValue = dither(gl_FragCoord.xy);
    finalColour.rgb += ditherValue;
    
    outColour = finalColour;
}