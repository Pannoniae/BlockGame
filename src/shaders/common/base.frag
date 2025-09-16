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
uniform vec3 lightDir2;
// ratio of light between direct (is that how it's called?) and ambient
uniform float lightRatio;
uniform float lightRatio2;
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
    // Apply lighting from both sources
    float light1 = max(0.0, dot(lightDir, normal));
    float light2 = max(0.0, dot(lightDir2, normal));
    float combined1 = clamp(light1 * lightRatio + (1 - lightRatio), 0.0, 1.0);
    float combined2 = clamp(light2 * lightRatio2 + (1 - lightRatio2), 0.0, 1.0);
    float totalLight = min(1.0, combined1 + combined2);
    finalColour = vec4(finalColour.rgb * totalLight, finalColour.a);
#endif
    
    // Apply fog
    finalColour = applyFog(finalColour, viewPosition);
    
    // Apply dithering to reduce banding
    finalColour.rgb += gradientDither(finalColour.rgb);
    
    outColour = finalColour;
}