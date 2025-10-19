#version 440

layout (location = 0) out vec4 outColour;

#include "/shaders/inc/fog.inc.glsl"
#include "/shaders/inc/dither.inc.glsl"

centroid in vec2 texCoords;
in vec4 colour;
in vec4 viewPosition;
in vec3 worldPos;

uniform sampler2D tex;
uniform vec3 cameraPos;
uniform float layerIndex;  // 0-3 for the 4 layers
uniform float worldTick;

// simple 3D noise function (not great but fast)
float hash(vec3 p) {
    p = fract(p * 0.3183099 + 0.1);
    p *= 17.0;
    return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float noise3D(vec3 p) {
    vec3 i = floor(p);
    vec3 f = fract(p);
    f = f * f * (3.0 - 2.0 * f); // smoothstep

    return mix(
        mix(mix(hash(i + vec3(0,0,0)), hash(i + vec3(1,0,0)), f.x),
            mix(hash(i + vec3(0,1,0)), hash(i + vec3(1,1,0)), f.x), f.y),
        mix(mix(hash(i + vec3(0,0,1)), hash(i + vec3(1,0,1)), f.x),
            mix(hash(i + vec3(0,1,1)), hash(i + vec3(1,1,1)), f.x), f.y),
        f.z
    );
}

void main() {
    const float noiseFreq = 12;

    float zScroll = worldTick * 0.015;

    // apply scroll BEFORE quantizing so noise moves with clouds
    vec3 scrolledPos = worldPos;
    scrolledPos.z += zScroll;

    // quantize world position to grid (makes it pixelated instead of blobby)
    vec3 quantizedPos = floor(scrolledPos * 4);

    // sample noise at quantized position
    vec3 noiseCoord = quantizedPos * noiseFreq;
    float noise1 = noise3D(noiseCoord);
    float noise2 = noise3D(noiseCoord * 1.7 + vec3(100, 100, 100)); // offset for different pattern

    // subtle UV distortion based on noise (breaks up the grid slightly)
    //vec2 uvDistort = vec2(noise1, noise2) * 0.003 * layerIndex; // stronger on outer layers
    vec2 finalUVs = texCoords;

    vec4 texColour = texture(tex, finalUVs);
    vec4 finalColour = texColour * colour;

    if (finalColour.a <= 0) {
        discard;
    }

    // modulate alpha with noise
    float alphaMod = -0.05 + ((noise1) * ((layerIndex + 1) / 12));
    finalColour.a += alphaMod;
    finalColour.a = finalColour.a * 0.6 + alphaMod * 1.2;

    finalColour.a = finalColour.a < 0.5 ? 0.0 : 1.0; // threshold

    if (finalColour.a <= 0) {
        discard;
    }

    finalColour = applyFog(finalColour, viewPosition);

    finalColour.rgb += gradientDither(finalColour.rgb);

    outColour = finalColour;
}