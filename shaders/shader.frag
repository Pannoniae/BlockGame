#version 440

#extension GL_ARB_texture_query_lod : enable
#extension GL_EXT_gpu_shader4 : enable

#include "inc/fog.inc"


uniform float aniso = 0f;

float det(mat2 matrix) {
    return matrix[0].x * matrix[1].y - matrix[0].y * matrix[1].x;
}

vec4 textureAF(sampler2D texSampler, vec2 uv) {

    // calculate subtexture boundaries for clamping
    // atlas is 256x256 with 16x16 textures (16 textures per row/column)
    const float subtexSize = 1.0/16.0; // each subtexture is 1/16 of atlas
    const float texelSize = 1.0/256.0; // size of one texel in normalized coords
    const float margin = texelSize * 0.5; // half-texel margin to prevent bleeding

    vec2 subtexIndex = floor(uv / subtexSize);
    vec2 subtexMin = subtexIndex * subtexSize + margin;
    vec2 subtexMax = (subtexIndex + 1.0) * subtexSize - margin;

    mat2 J = inverse(mat2(dFdx(uv), dFdy(uv)));
    J = transpose(J)*J;
    float d = det(J), t = J[0][0]+J[1][1],
    D = sqrt(abs(t*t-4.001*d)), // using 4.001 instead of 4.0 fixes a rare texture glitch with square texture atlas
    V = (t-D)/2.0, v = (t+D)/2.0,
    M = 1.0/sqrt(V), m = 1./sqrt(v);
    vec2 A = M * normalize(vec2(-J[0][1], J[0][0]-V));

    float lod = 0.0;

    float samplesDiv2 = aniso / 2.0;
    vec2 ADivSamples = A / aniso;

    vec4 c = vec4(0.0);
    for (float i = -samplesDiv2 + 0.5; i < samplesDiv2; i++) {
        vec2 sampleUV = uv + ADivSamples * i;
        sampleUV = clamp(sampleUV, subtexMin, subtexMax);
        vec4 colorSample = textureLod(texSampler, sampleUV, lod);
        
        c.rgb += colorSample.rgb * colorSample.a;
        c.a += colorSample.a;
    }
    c.rgb /= aniso;
    c.a /= aniso;

    return c;
}


// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 colour;

centroid in vec2 texCoords;
in vec4 tint;
in float vertexDist;

uniform sampler2D blockTexture;

void main() {

    vec4 blockColour = textureAF(blockTexture, texCoords);
    float ratio = getFog(vertexDist);
    // extract skylight, 0 to 15

    colour = vec4(blockColour.rgb * tint.rgb, blockColour.a);

    if (colour.a <= 0) {
        discard;
    }
    // make it always opaque (for mipmapping)
    colour.a = max(colour.a, 1);

    // mix the fog colour between it and the sky
    vec4 mixedFogColour = mix(fogColour, skyColour, ratio);
    // mix fog
    colour = mix(colour, mixedFogColour, ratio);
}