#version 440 compatibility

// CRT Shader by Harrison Allen (Public Domain)
// V3

#include "/shaders/inc/dither.inc.glsl"

layout (binding = 0) uniform sampler2D u_colorTexture;

// CRT effect uniforms
//uniform int u_maskType; // 0=Off, 1=Dots, 2=Grille, 3=Wide Grille, 4=Wide Soft Grille, 5=Slot Mask
uniform float u_curve; // Screen curvature
uniform float u_sharpness; // Image sharpness
uniform float u_colorOffset; // Color channel offset
uniform float u_brightness; // Overall brightness
uniform float u_aspect; // Screen aspect ratio
uniform float u_minScanlineThickness; // Minimum scanline thickness
uniform float u_wobbleStrength; // Horizontal shake strength
uniform float u_time; // Time for animations
uniform float u_scanlineRes; // Target scanline resolution (e.g. 240, 480)

#define u_maskType 3

centroid in vec2 v_texCoord;

out vec4 fragColour;

vec2 warp(vec2 uv, float _aspect, float _curve) {
    // Centralizes coordinates (0 is in the middle)
    uv -= 0.5;
    
    uv.x /= _aspect;
    
    // Squared distance from the middle
    float warping = dot(uv, uv) * _curve;
    
    // Compensate for shrinking
    warping -= _curve * 0.25;
    
    // Warp the coordinates
    uv /= 1.0 - warping;
    
    uv.x *= _aspect;
    
    // Decentralize the coordinates
    uv += 0.5;
    
    return uv;
}

vec3 linear_to_srgb(vec3 col) {
    return mix(
        (pow(col, vec3(1.0 / 2.4)) * 1.055) - 0.055,
        col * 12.92,
        lessThan(col, vec3(0.0031318))
    );
}

vec3 srgb_to_linear(vec3 col) {
    return mix(
        pow((col + 0.055) / 1.055, vec3(2.4)),
        col / 12.92,
        lessThan(col, vec3(0.04045))
    );
}

// Get scanlines from coordinates (returns in linear color)
vec3 scanlines(vec2 uv) {
    // Use texture width for horizontal, but scanline resolution for vertical
    vec2 texSize = vec2(textureSize(u_colorTexture, 0));
    vec2 scaledUV = vec2(uv.x * texSize.x, uv.y * u_scanlineRes);
    
    // Vertical coordinate scanline samples based on scanline resolution
    int y = int(floor((scaledUV.y / u_scanlineRes) * texSize.y - 0.5));
    
    float x = floor(scaledUV.x);
    
    // Horizontal coordinates for the texture samples
    float ax = x - 2.0;
    float bx = x - 1.0;
    float cx = x;
    float dx = x + 1.0;
    float ex = x + 2.0;
    
    // Sample the texture at various points
    vec3 upper_a = texelFetch(u_colorTexture, ivec2(int(ax), y), 0).rgb;
    vec3 upper_b = texelFetch(u_colorTexture, ivec2(int(bx), y), 0).rgb;
    vec3 upper_c = texelFetch(u_colorTexture, ivec2(int(cx), y), 0).rgb;
    vec3 upper_d = texelFetch(u_colorTexture, ivec2(int(dx), y), 0).rgb;
    vec3 upper_e = texelFetch(u_colorTexture, ivec2(int(ex), y), 0).rgb;
    
    // Adjust the vertical coordinate for the lower scanline
    y += 1;
    
    // Sample the texture at various points
    vec3 lower_a = texelFetch(u_colorTexture, ivec2(int(ax), y), 0).rgb;
    vec3 lower_b = texelFetch(u_colorTexture, ivec2(int(bx), y), 0).rgb;
    vec3 lower_c = texelFetch(u_colorTexture, ivec2(int(cx), y), 0).rgb;
    vec3 lower_d = texelFetch(u_colorTexture, ivec2(int(dx), y), 0).rgb;
    vec3 lower_e = texelFetch(u_colorTexture, ivec2(int(ex), y), 0).rgb;
    
    // Convert every sample to linear color
    upper_a = srgb_to_linear(upper_a);
    upper_b = srgb_to_linear(upper_b);
    upper_c = srgb_to_linear(upper_c);
    upper_d = srgb_to_linear(upper_d);
    upper_e = srgb_to_linear(upper_e);
    
    lower_a = srgb_to_linear(lower_a);
    lower_b = srgb_to_linear(lower_b);
    lower_c = srgb_to_linear(lower_c);
    lower_d = srgb_to_linear(lower_d);
    lower_e = srgb_to_linear(lower_e);
    
    // The x coordinates of the closest
    vec3 beam = vec3(scaledUV.x - 0.5);
    beam.r -= u_colorOffset;
    beam.b += u_colorOffset;
    
    // Calculate weights
    vec3 weight_a = smoothstep(1, 0, (beam - ax) * u_sharpness);
    vec3 weight_b = smoothstep(1, 0, (beam - bx) * u_sharpness);
    vec3 weight_c = smoothstep(1, 0, abs(beam - cx) * u_sharpness);
    vec3 weight_d = smoothstep(1, 0, (dx - beam) * u_sharpness);
    vec3 weight_e = smoothstep(1, 0, (ex - beam) * u_sharpness);
    
    // Mix samples into the upper scanline color
    vec3 upper_col = vec3(
    upper_a * weight_a +
    upper_b * weight_b +
    upper_c * weight_c +
    upper_d * weight_d +
    upper_e * weight_e
    );
    
    // Mix samples into the lower scanline color
    vec3 lower_col = vec3(
    lower_a * weight_a +
    lower_b * weight_b +
    lower_c * weight_c +
    lower_d * weight_d +
    lower_e * weight_e
    );
    
    vec3 weight_scaler = vec3(1.0) / (weight_a + weight_b + weight_c + weight_d + weight_e);
    
    // Normalize weight
    upper_col *= weight_scaler;
    lower_col *= weight_scaler;
    
    // Scanline size (and roughly the apperent brightness of this line)
    vec3 upper_thickness = mix(vec3(u_minScanlineThickness), vec3(1.0), upper_col);
    vec3 lower_thickness = mix(vec3(u_minScanlineThickness), vec3(1.0), lower_col);
    
    // Vertical sawtooth wave used to generate scanlines based on scanline resolution
    float sawtooth = fract(scaledUV.y + 0.5);
    
    vec3 upper_line = vec3(sawtooth) / upper_thickness;
    upper_line = smoothstep(1.0, 0.0, upper_line);
    
    vec3 lower_line = vec3(1.0 - sawtooth) / lower_thickness;
    lower_line = smoothstep(1.0, 0.0, lower_line);
    
    // Correct line brightness below min_scanline_thickness
    upper_line *= upper_col / upper_thickness;
    lower_line *= lower_col / lower_thickness;
    
    // Combine the upper and lower scanlines
    vec3 combined = upper_line + lower_line;
    
    // Calculate dim version
    vec3 dark_upper = smoothstep(u_minScanlineThickness, 0.0, vec3(sawtooth));
    dark_upper *= upper_col;
    
    vec3 dark_lower = smoothstep(u_minScanlineThickness, 0.0, vec3(1.0 - sawtooth));
    dark_lower *= lower_col;
    
    vec3 dark_combined = dark_upper + dark_lower;
    
    // Mix bright and dim
    return mix(dark_combined, combined, u_brightness);
}

// don't worry, they'll be trimmed

const vec3 pattern1[4] = vec3[4](vec3(1, 0, 0), vec3(0, 1, 0), vec3(0, 0, 1), vec3(0, 0, 0));

const vec3 pattern2[2] = vec3[2](vec3(0, 1, 0), vec3(1, 0, 1));

const vec3 pattern3[4] = vec3[4](vec3(1, 0, 0), vec3(0, 1, 0), vec3(0, 0, 1), vec3(0, 0, 0));

const vec3 pattern4[4] = vec3[4](
vec3(1.0, 0.125, 0.0),
vec3(0.125, 1.0, 0.125),
vec3(0.0, 0.125, 1.0),
vec3(0.125, 0.0, 0.125)
);

const vec3 pattern5[16] = vec3[16](
vec3(1, 0, 1), vec3(0, 1, 0), vec3(1, 0, 1), vec3(0, 1, 0),
vec3(0, 0, 1), vec3(0, 1, 0), vec3(1, 0, 0), vec3(0, 0, 0),
vec3(1, 0, 1), vec3(0, 1, 0), vec3(1, 0, 1), vec3(0, 1, 0),
vec3(1, 0, 0), vec3(0, 0, 0), vec3(0, 0, 1), vec3(0, 1, 0)
);

vec4 generate_mask(vec2 fragcoord) {
    switch (u_maskType) {
    // Dots
        case 1:
            ivec2 icoords = ivec2(fragcoord);
            return vec4(pattern1[(icoords.y * 2 + icoords.x) % 4], 0.25);
    // Grille
        case 2:
            return vec4(pattern2[int(fragcoord.x) % 2], 0.5);
    // Wide grille
        case 3:
            return vec4(pattern3[int(fragcoord.x) % 4], 0.25);
    // Grille wide soft
        case 4:
            return vec4(pattern4[int(fragcoord.x) % 4], 0.3125);
    // Slotmask
        case 5:
            ivec2 icoords2 = ivec2(fragcoord) % 4;
            return vec4(pattern5[icoords2.y * 4 + icoords2.x], 0.375);
        default:
            return vec4(0.5); // No mask
    }
}

// Add phosphor mask/grill
vec3 mask(vec3 linear_color, vec2 fragcoord) {
    // Get the pattern for the mask. Mask.w equals avg. mask brightness
    vec4 mask = generate_mask(fragcoord);
    
    // How bright the color needs to be to maintain 100% brightness while masked
    vec3 target_color = linear_color / mask.w;
    
    // Target color limited to the 0 to 1 range.
    vec3 primary_col = clamp(target_color, 0.0, 1.0);
    
    // This calculates how bright the secondary subpixels will need to be
    vec3 highlights = target_color - primary_col;
    highlights /= 1.0 / mask.w - 1.0;
    
    primary_col *= mask.rgb;
    
    // Add the secondary subpixels
    primary_col += highlights * (1.0 - mask.rgb);
    
    // Blend with a dim version
    primary_col = mix(linear_color * mask.rgb, primary_col, u_brightness);
    
    return primary_col;
}

void main() {
    // Warp UV coordinates
    vec2 warped_coords = warp(v_texCoord, u_aspect, u_curve * 0.5);
    
    // Add wobble
    float wobble = cos(u_time * 6.28318 * 15.0) * u_wobbleStrength / 4000.0;
    warped_coords.x += wobble;
    
    // Sample the scanlines
    vec3 col = scanlines(warped_coords);
    
    // Apply phosphor mask
    col = mask(col, gl_FragCoord.xy);
    
    // Convert back to srgb
    col = linear_to_srgb(col);
    
    fragColour = vec4(col, 1.0);
    
    fragColour.rgb += gradientDither(fragColour.rgb);
}