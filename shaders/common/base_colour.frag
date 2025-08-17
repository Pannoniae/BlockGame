#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout (location = 0) out vec4 outColour;

#include "/shaders/inc/fog.inc.glsl"
#include "/shaders/inc/dither.inc.glsl"

in vec4 colour;
in vec4 viewPosition;

void main() {
    if (colour.a <= 0) {
        discard;
    }
    
    vec4 finalColour = colour;
    
    // Apply fog
    finalColour = applyFog(finalColour, viewPosition);
    
    // Apply dithering to reduce banding
    float ditherValue = dither(gl_FragCoord.xy);
    finalColour.rgb += ditherValue;
    
    outColour = finalColour;
}