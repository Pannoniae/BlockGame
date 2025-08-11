#version 440

#extension GL_ARB_texture_query_lod : enable
#extension GL_EXT_gpu_shader4 : enable
#extension GL_NV_command_list : enable

layout(commandBindableNV) uniform;

#include "inc/fog.inc.glsl"

//#if not defined(ANISO_LEVEL)
//#define ANISO_LEVEL 0
//#endif

float det(mat2 matrix) {
    return matrix[0].x * matrix[1].y - matrix[0].y * matrix[1].x;
}

vec2 mirror(vec2 uv, vec2 minBounds, vec2 maxBounds) {
    vec2 range = maxBounds - minBounds;
    vec2 normalized = (uv - minBounds) / range;
    
    normalized = 1.0 - abs(mod(normalized, 2.0) - 1.0);
    
    return minBounds + normalized * range;
}

vec4 mapAniso(float h, float maxrange) {
    vec4 colours[3];
    colours[0] = vec4(0., 0., 1., 1.);
    colours[1] = vec4(1., 1., 0., 1.);
    colours[2] = vec4(1., 0., 0., 1.);

    float halfrange = maxrange / 2.0;
    h = clamp(h, 0, maxrange);
    if(h > halfrange) {
        return mix(colours[1], colours[2], (h-halfrange)/halfrange);
    }
    else {
        return mix(colours[0], colours[1], h/halfrange);
    }
}

vec4 textureAF(sampler2D texSampler, vec2 uv) {

    // calculate subtexture boundaries for mirroring
    // atlas is 256x256 with 16x16 textures (16 textures per row/column)
    const float subtexSize = 1.0/16.0; // each subtexture is 1/16 of atlas
    const float texelSize = 1.0/256.0; // size of one texel in normalized coords
    const float margin = texelSize * 0.5; // half-texel margin to prevent bleeding

    vec2 subtexIndex = floor(uv / subtexSize);
    vec2 subtexMin = subtexIndex * subtexSize;
    vec2 subtexMax = (subtexIndex + 1.0) * subtexSize;
    vec2 subtexMinClamped = subtexMin + margin;
    vec2 subtexMaxClamped = subtexMax - margin;

    mat2 J = inverse(mat2(dFdx(uv), dFdy(uv)));
    J = transpose(J)*J;
    float d = det(J);
    float t = J[0][0]+J[1][1];
    float D = sqrt(abs(t*t-4.001*d));
    // major
    float V = (t-D)/2.0;
    // minor
    float v = (t+D)/2.0;
    // magnify along major axis
    float M = 1.0/sqrt(V);
    // magnify along minor axis
    float m = 1./sqrt(v);
    // major axis dv
    vec2 A = M * normalize(vec2(-J[0][1], J[0][0]-V));

    // calculate anisotropy ratio and adapt sample count
    float anisotropy = max(M/m, 1.0);
    float sampleCount = min(ANISO_LEVEL, ceil(anisotropy));
    
    // debug mode: return anisotropy visualization
    if (DEBUG_ANISO != 0) {
        vec4 baseColor = texture(texSampler, clamp(mirror(uv, subtexMin, subtexMax), subtexMinClamped, subtexMaxClamped));
        vec4 anisoColor = mapAniso(anisotropy, 256.0);
        return mix(anisoColor, baseColor, 0.4);
    }
    
    float lod = 0.0;

    float samplesHalf = sampleCount / 2.0;
    vec2 ADivSamples = A / sampleCount;

    vec4 c = vec4(0.0);
    for (float i = -samplesHalf + 0.5; i < samplesHalf; i++) {
        vec2 sampleUV = uv + ADivSamples * i;
        sampleUV = clamp(mirror(sampleUV, subtexMin, subtexMax), subtexMinClamped, subtexMaxClamped);
        vec4 colorSample = textureLod(texSampler, sampleUV, lod);
        
        c.rgb += colorSample.rgb * colorSample.a;
        c.a += colorSample.a;
    }
    c.rgb /= c.a;
    c.a /= sampleCount;

    return c;
}


// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 colour;

centroid in vec2 texCoords;
flat in float skyDarken;
in vec4 tint;
in float vertexDist;

uniform sampler2D blockTexture;

void main() {
    vec4 blockColour;
#if ANISO_LEVEL == 0
    // no anisotropic filtering, use regular texture lookup
    blockColour = texture(blockTexture, texCoords);
#else
    // use anisotropic filtering
    blockColour = textureAF(blockTexture, texCoords);
#endif
    float ratio = getFog(vertexDist);
    // extract skylight, 0 to 15

    // apply skyDarken - reduce lighting based on day/night cycle
    float darkenFactor = 1.0 - (skyDarken / 15.0);
    vec3 darkenedTint = tint.rgb * darkenFactor;
    colour = vec4(blockColour.rgb * darkenedTint, blockColour.a);

    if (colour.a <= 0) {
        discard;
    }
    // make it always opaque (for mipmapping)
    colour.a = max(colour.a, 1);

    // mix the fog colour between it and the sky
    vec4 mixedFogColour = mix(fogColour, horizonColour, ratio);
    // mix fog
    colour = mix(colour, mixedFogColour, ratio);
}