#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout (location = 0) out vec4 outColour;

centroid in vec2 texCoords;
in vec4 colour;
in vec4 viewPosition;

uniform vec4 fogColor;
uniform float fogStart;
uniform float fogEnd;
uniform bool fogEnabled;
uniform int fogType; // 0 = linear, 1 = exp, 2 = exp2
uniform float fogDensity; // For exp and exp2 fog

uniform sampler2D tex;

float dither(vec2 coord) {
    // use a simple 4x4 Bayer matrix pattern
    int x = int(coord.x) % 4;
    int y = int(coord.y) % 4;
    
    const mat4 bayerMatrix = mat4(
    0.0 / 16.0, 8.0 / 16.0, 2.0 / 16.0, 10.0 / 16.0,
    12.0 / 16.0, 4.0 / 16.0, 14.0 / 16.0, 6.0 / 16.0,
    3.0 / 16.0, 11.0 / 16.0, 1.0 / 16.0, 9.0 / 16.0,
    15.0 / 16.0, 7.0 / 16.0, 13.0 / 16.0, 5.0 / 16.0
    );
    
    return bayerMatrix[y][x] / 255.0; // scale to RGB
}

void main() {
    
    vec4 texColour = texture(tex, texCoords);
    if (colour.a <= 0) {
        discard;
    }
    vec4 mixColour = texColour * colour;
    
    vec4 finalColour = mixColour;
    
    if (fogEnabled) {
        // Calculate fogDepth in the fragment shader
        float fogDepth = length(viewPosition.xyz); // Use distance from camera instead of just z
        
        // Calculate fog factor based on fog type
        float fogFactor = 1.0;
        
        switch (fogType) {
            case 0:
        // Linear fog
                fogFactor = (fogEnd - fogDepth) / (fogEnd - fogStart);
                break;
            
            case 1:
        // Exponential fog
                fogFactor = exp(-fogDensity * fogDepth);
                break;
            
            case 2:
        // Exponential squared fog
                fogFactor = exp(-fogDensity * fogDensity * fogDepth * fogDepth);
                break;
        }
        
        fogFactor = clamp(fogFactor, 0.0, 1.0);
        
        // Mix original color with fog color
        finalColour = mix(fogColor, mixColour, fogFactor);
    }
    // Apply dithering to reduce banding in dark colors
    float ditherValue = dither(gl_FragCoord.xy);
    finalColour.rgb += ditherValue;
    
    outColour = finalColour;
}