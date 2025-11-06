uniform ivec2 texSize;
uniform ivec2 atlasSize;

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
    if (h > halfrange) {
        return mix(colours[1], colours[2], (h - halfrange) / halfrange);
    }
    else {
        return mix(colours[0], colours[1], h / halfrange);
    }
}

vec4 textureAF(sampler2D texSampler, vec2 uv) {
    const vec2 ri = vec2(1. / atlasSize);
    const vec2 i = vec2(texSize * (ri));
    // calculate subtexture boundaries for mirroring
    // atlas is 256x256 with 16x16 textures (16 textures per row/column)
    const vec2 subtexSize = vec2(1.0) * i; // each subtexture is 1/16 of atlas
    const vec2 texelSize = vec2(1.0) * ri; // size of one texel in normalized coords
    const vec2 margin = texelSize * 0.5; // half-texel margin to prevent bleeding
    
    vec2 subtexIndex = floor(uv / subtexSize);
    vec2 subtexMin = subtexIndex * subtexSize;
    vec2 subtexMax = (subtexIndex + 1.0) * subtexSize;
    vec2 subtexMinClamped = subtexMin + margin;
    vec2 subtexMaxClamped = subtexMax - margin;
    
    mat2 J = inverse(mat2(dFdx(uv), dFdy(uv)));
    J = transpose(J) * J;
    float d = det(J);
    float t = J[0][0] + J[1][1];
    float D = sqrt(abs(t * t - 4.001 * d));
    // major
    float V = (t - D) / 2.0;
    // minor
    float v = (t + D) / 2.0;
    // magnify along major axis
    float M = 1.0 / sqrt(V);
    // magnify along minor axis
    float m = 1. / sqrt(v);
    // major axis dv
    vec2 A = M * normalize(vec2(-J[0][1], J[0][0] - V));
    
    // calculate anisotropy ratio and adapt sample count
    float anisotropy = max(M / m, 1.0);
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