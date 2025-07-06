#version 440

#include "inc/fog.inc"
#line 4
// don't, glass will be fucked
//layout(early_fragment_tests) in;
layout(location = 0) out vec4 colour;

in vec2 texCoords;
in vec4 tint;
in float vertexDist;

uniform sampler2D blockTexture;
uniform vec2 wh;

float calculateShrinkage(float mipLevel, float tileTexelSize, float atlasSize) {
    // At each mip level, texels are 2^mipLevel times larger
    // We need to stay at least 0.5 texels away from edge at that mip level
    float texelSize = exp2(mipLevel);
    float paddingTexels = texelSize * 0.5;

    // Convert to UV space (0-1 within the tile)
    float paddingUV = paddingTexels / tileTexelSize;

    // Clamp to prevent over-shrinking (max 50% shrink from each edge)
    paddingUV = min(paddingUV, 0.5);

    // Convert to a lerp factor (1.0 = no shrink, 0.0 = fully shrunk)
    //return 1.0 - (paddingUV * 2.0);
    
    switch (int(mipLevel)) {
        case 0:
            return 1.0;
        case 1:
            return 0.75; // 25% shrink
        case 2:
            return 0.5;  // 50% shrink
        case 3:
            return 0.25; // 75% shrink
        case 4:
        default:
            return 0.0;  // Fully shrunk for higher mip levels
    }
    return 0.0;
}

vec2 getHackUV(inout vec2 uvs, float mipLevel) {
    // based on mipLevel, remap the original UVs (0 to 0.0625) to smaller ranges
    vec2 ratio = uvs / vec2(16. / 256.); // 16 pixels in a tile, 256 pixels in atlas
    
    vec2 range;
    switch (int(mipLevel)) {
        case 0:
            return uvs; // No change, full tile UVs
        case 1:
            //from 1/8 to 7/8 of the tile
            range = vec2(0.125, 0.875) * vec2(16. / 256.);
            return mix(vec2(range.x), vec2(range.y), ratio);
        case 2:
            //from 1/4 to 3/4 of the tile
            range = vec2(0.25, 0.75) * vec2(16. / 256.);
            return mix(vec2(range.x), vec2(range.y), ratio);
        case 3:
        case 4:
            //from 4/8 to 4/8 of the tile
            range = vec2(0.5, 0.5) * vec2(16. / 256.);
            return mix(vec2(range.x), vec2(range.y), ratio);
    }
}

vec4 textureAniso(sampler2D T, vec2 p) {
    mat2 J = inverse(mat2(dFdx(p),dFdy(p)));       // dFdxy: pixel footprint in texture space
    J = transpose(J)*J;                            // quadratic form
    float d = determinant(J), t = J[0][0]+J[1][1], // find ellipse: eigenvalues, max eigenvector
    D = sqrt(abs(t*t-4.*d)),                 // abs() fix a bug: in weird view angles 0 can be slightly negative
    V = (t-D)/2., v = (t+D)/2.,                     // eigenvalues. ( ATTENTION: not sorted )
    M = 1./sqrt(V), m = 1./sqrt(v), l =log2(m*wh.y); // = 1./radii^2
    //if (M/m>16.) l = log2(M/16.*R.y);                     // optional
    vec2 A = M * normalize(vec2( -J[0][1] , J[0][0]-V )); // max eigenvector = main axis
    vec4 O = vec4(0);
    for (float i = -7.5; i<8.; i++)                       // sample x16 along main axis at LOD min-radius
    O += textureLod(T, p+(i/16.)*A, l);
    return O/16.;
}

const float AF_SAMPLES = 16.0; // number of samples for anisotropic filtering

float manualDeterminant(const in mat2 matrix) {
    return matrix[0].x * matrix[1].y - matrix[0].y * matrix[1].x;
}

mat2 inverse2(const in mat2 m) {
    mat2 adj;
    adj[0][0] =  m[1][1];
    adj[0][1] = -m[0][1];
    adj[1][0] = -m[1][0];
    adj[1][1] =  m[0][0];
    return adj / manualDeterminant(m);
}

vec4 textureAnisotropic(const in sampler2D sampler, const in vec2 uv) {
    vec4 spriteBounds = vec4(0.0, 0.0, 16.0 / 256.0, 16.0 / 256.0); // UV bounds of the sprite in the atlas
    
    mat2 J = inverse(mat2(dFdx(uv), dFdy(uv)));
    J = transpose(J) * J;     // quadratic form

    float d = manualDeterminant(J), t = J[0][0]+J[1][1],  // find ellipse: eigenvalues, max eigenvector
    D = sqrt(abs(t*t-4.0*d)),                 // abs() fix a bug: in weird view angles 0 can be slightly negative
    V = (t-D)/2.0, v = (t+D)/2.0,                // eigenvalues
    M = 1.0/sqrt(V), m = 1.0/sqrt(v);             // = 1./radii^2

    vec2 A = M * normalize(vec2(-J[0][1], J[0][0]-V)); // max eigenvector = main axis

    float lod;
    if (M/m > 16.0) {
        lod = log2(M / 16.0 * wh.y);
    } else {
        lod = log2(m * wh.y);
    }

    const float AnisotropicSamplesInv = 1. / AF_SAMPLES;
    vec2 ADivSamples = A * AnisotropicSamplesInv;

    vec2 spriteDimensions = vec2(spriteBounds.z - spriteBounds.x, spriteBounds.w - spriteBounds.y);

    vec4 final;
    final.rgb = vec3(0.0);

    // preserve original alpha to prevent artifacts
    final.a = textureLod(sampler, uv, lod).a;

    const float samplesDiv2 = AF_SAMPLES / 2.0;
    for (float i = -samplesDiv2 + 0.5; i < samplesDiv2; i++) { // sample along main axis at LOD min-radius
        vec2 sampleUV = uv + ADivSamples * i;
        sampleUV = mod(sampleUV - spriteBounds.xy, spriteDimensions) + spriteBounds.xy; // wrap sample UV to fit inside sprite
        final.rgb += textureLod(sampler, sampleUV, lod).rgb;
    }

    final.rgb *= AnisotropicSamplesInv;
    return final;
}

void main() {
    vec2 uvs = texCoords;
    vec2 tileMin = vec2(0.); // UV coords of tile's top-left
    vec2 tileMax = vec2(16. / 256.); // UV coords of tile's bottom-right
    vec2 tileCenter = (tileMin + tileMax) * 0.5;

    // Local UV within tile (0-1)
    vec2 localUV = (uvs - tileMin) / (tileMax - tileMin);
    
    // Query LOD
    float mipLevel = textureQueryLod(blockTexture, uvs).x;

    // Calculate shrinkage
    //float shrinkFactor = calculateShrinkage(mipLevel, 16.0, 256.0);

    // Apply shrinkage toward center
    //vec2 shrunkLocalUV = mix(vec2(0.5), uvs, shrinkFactor);

    // Convert back to atlas UV space
    //vec2 finalUV = tileMin + shrunkLocalUV * (tileMax - tileMin);
    
    //vec2 finalUV = getHackUV(uvs, mipLevel);
    
    vec4 blockColour = textureAnisotropic(blockTexture, uvs);
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