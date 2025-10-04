// Bayer matrix dithering for color banding reduction
float dither(vec2 coord) {
    // use a simple 4x4 Bayer matrix pattern
    int x = int(coord.x) % 4;
    int y = int(coord.y) % 4;
    
    const mat4 bayerMatrix = mat4(
    0.0 / 16.0, 8.0 / 16.0, 2.0 / 16.0, 10.0 / 16.0,
    12.0 / 16.0, 4.0 / 16.0, 14.0 / 16.0, 6.0 / 16.0,
    3.0 / 16.0, 11.0 / 16.0, 1.0 / 16.0, 9.0 / 16.0,
    15.0 / 16.0, 7.0 / 16.0, 13.0 / 16.0, 5.0 / 16.0
    );
    
    return (bayerMatrix[y][x] - 0.5) / 255.0; // scale to RGB
}

float gradientNoise(in vec2 uv) {
    return fract(52.9829189 * fract(dot(uv, vec2(0.06711056, 0.00583715))));
}

vec3 gradientDither(vec3 color) {
    return (1.0 / 255.0) * vec3(gradientNoise(gl_FragCoord.xy)) - (0.5 / 255.0);
}