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
    vec4 finalColour;
    
    #ifdef HAS_TEXTURE
    vec4 texColour = texture(tex, texCoords);
    finalColour = texColour * colour;
    #else
    finalColour = colour;
    #endif

    if (finalColour.a <= 0) {
        discard;
    }
    
    #ifdef HAS_NORMALS
    // Apply lighting from both sources: ambient + diffuse1 + diffuse2
    float light1 = max(0.0, dot(normal, lightDir));
    float light2 = max(0.0, dot(normal, lightDir2));
    
    // lightRatio controls diffuse intensity, ambient is (1 - max(lightRatio, lightRatio2))
    float ambient = 1.0 - max(lightRatio, lightRatio2);
    float totalLight = ambient + light1 * lightRatio + light2 * lightRatio2;
    finalColour.rgb *= min(1.0, totalLight);
    #endif
    
    // Apply fog
    finalColour = applyFog(finalColour, viewPosition);
    
    // Apply dithering to reduce banding
    finalColour.rgb += gradientDither(finalColour.rgb);
    
    outColour = finalColour;
}