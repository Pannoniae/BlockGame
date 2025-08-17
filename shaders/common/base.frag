#version 440

// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout (location = 0) out vec4 outColour;

#include "/shaders/inc/fog.inc.glsl"
#include "/shaders/inc/dither.inc.glsl"

centroid in vec2 texCoords;
in vec4 colour;
in vec4 viewPosition;

#ifdef HAS_NORMALS
in vec3 normal;
uniform vec3 lightDir;
// ratio of light between direct (is that how it's called?) and ambient
uniform float lightRatio;
#endif

#ifdef HAS_TEXTURE
uniform sampler2D tex;
#endif

void main() {
    if (colour.a <= 0) {
        discard;
    }
    
    vec4 finalColour;
    
#ifdef HAS_TEXTURE
    vec4 texColour = texture(tex, texCoords);
    finalColour = texColour * colour;
#else
    finalColour = colour;
#endif
    
#ifdef HAS_NORMALS
    // Apply lighting
    float light = max(0.0, dot(lightDir, normal));
    float combined = clamp(light * lightRatio + (1 - lightRatio), 0.0, 1.0);
    finalColour = vec4(finalColour.rgb * combined, finalColour.a);
#endif
    
    // Apply fog
    finalColour = applyFog(finalColour, viewPosition);
    
    // Apply dithering to reduce banding
    float ditherValue = dither(gl_FragCoord.xy);
    finalColour.rgb += ditherValue;
    
    outColour = finalColour;
}