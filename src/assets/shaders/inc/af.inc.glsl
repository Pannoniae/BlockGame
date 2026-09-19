uniform ivec2 texSize;
uniform ivec2 atlasSize;

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


vec4 textureAF(sampler2D texSampler, vec2 uv, vec2 ddx, vec2 ddy) {
    vec2 ri = 1.0 / vec2(atlasSize); // one texel in uv
    vec2 subtexSize = vec2(texSize) * ri;
    vec2 subtexMin = floor(uv / subtexSize) * subtexSize;
    vec2 subtexMax = subtexMin + subtexSize;

    // pixel footprint ellipse in texels
    // J = (M M^T)^-1 w/ M = [ddx | ddy]; eigenvalues are 1/axis^2
    mat2 J = inverse(mat2(ddx * vec2(atlasSize), ddy * vec2(atlasSize)));
    J = transpose(J) * J;
    float d = determinant(J);
    float t = J[0][0] + J[1][1];
    float D = sqrt(abs(t * t - 4.001 * d));
    // smaller  = major axis, larger = minor
    float V = (t - D) / 2.0;
    float v = (t + D) / 2.0;
    float M = inversesqrt(V); // major
    float m = inversesqrt(v); // minor
    
    vec2 e0 = vec2(-J[0][1], J[0][0] - V);
    vec2 e1 = vec2(J[1][1] - V, -J[0][1]);
    vec2 A = M * normalize(dot(e0, e0) > dot(e1, e1) ? e0 : e1);

    float anisotropy = max(M / m, 1.0);
    float sampleCount = min(float(ANISO_LEVEL), ceil(anisotropy));

#if DEBUG_ANISO != 0
    vec4 baseColor = texture(texSampler, clamp(mirror(uv, subtexMin, subtexMax), subtexMin + ri * 0.5, subtexMax - ri * 0.5));
    return mix(mapAniso(anisotropy, 256.0), baseColor, 0.4);
#endif
    
    float lod = min(log2(max(max(m, M / float(ANISO_LEVEL)), 1.0)), log2(float(texSize.x)));

    // neighbour bleed fix
    vec2 margin = ri * (0.5 * exp2(ceil(lod)));
    vec2 lo = subtexMin + margin;
    vec2 hi = subtexMax - margin;

    vec2 step = (A * ri) / sampleCount;
    vec2 start = uv - step * (sampleCount - 1.0) * 0.5;

    // transparent texels are black in the atlas so we must not darken the result
    vec4 c = vec4(0.0);
    for (float i = 0.0; i < sampleCount; i++) {
        vec2 sampleUV = clamp(mirror(start + step * i, subtexMin, subtexMax), lo, hi);
        vec4 s = textureLod(texSampler, sampleUV, lod);
        c.rgb += s.rgb * s.a;
        c.a += s.a;
    }
    // nan fix
    c.rgb /= max(c.a, 1e-4);
    c.a /= sampleCount;
    return c;
}
