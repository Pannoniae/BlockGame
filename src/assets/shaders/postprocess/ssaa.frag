#version 440 core

#include "/shaders/inc/dither.inc.glsl"

layout (binding = 0) uniform sampler2D u_colorTexture;

uniform vec2 u_texelStep;
uniform int u_ssaaFactor;
uniform int u_ssaaMode;

centroid in vec2 v_texCoord;

out vec4 fragColour;

vec4 ssaa(sampler2D screenTex, int samples, vec2 uv) {
    vec3 avg = vec3(0);
    for (int x = 0; x < samples; x++) {
        for (int y = 0; y < samples; y++) {
            avg += texture(screenTex, uv + vec2(x, y) * u_texelStep).rgb;
        }
    }
    return vec4(avg / (samples * samples), 1.0);
}

// same but rotate the grid sampling pattern
vec4 rgssaa(sampler2D screenTex, int samples, vec2 uv) {
    vec3 avg = vec3(0);
    for (int x = 0; x < samples; x++) {
        for (int y = 0; y < samples; y++) {
            // rotate the sampling pattern by 45 degrees
            float angle = (x + y) * 3.14159265358979323846;
            vec2 offset = vec2(cos(angle), sin(angle)) * u_texelStep;
            avg += texture(screenTex, uv + offset).rgb;
        }
    }
    return vec4(avg / (samples * samples), 1.0);
}

// weighted SSAA - center texels are weighted more
vec4 wgssaa(sampler2D screenTex, int samples, vec2 uv) {
    vec3 avg = vec3(0);
    float totalWeight = 0.0;

    float center = float(samples) * 0.5 - 0.5;

    for (int x = 0; x < samples; x++) {
        for (int y = 0; y < samples; y++) {
            // distance from center
            float dx = abs(float(x) - center);
            float dy = abs(float(y) - center);
            float dist = sqrt(dx * dx + dy * dy);

            // weight based on distance from center (gaussian-like)
            float weight = exp(-dist * dist * 0.5);

            avg += texture(screenTex, uv + vec2(x, y) * u_texelStep).rgb * weight;
            totalWeight += weight;
        }
    }
    return vec4(avg / totalWeight, 1.0);
}

// per-sample shading SSAA - relies on glMinSampleShading for hardware acceleration
// this function just returns the texture sample since per-sample work is done by GPU
vec4 psssaa(sampler2D screenTex, vec2 uv) {
    // with per-sample shading enabled, each sample is shaded independently
    // the GPU automatically handles the anti-aliasing through multisampling
    return texture(screenTex, uv);
}

void main(void)
{
    if (u_ssaaFactor == 0)
    {
        // No SSAA, just return the color.
        fragColour = texture(u_colorTexture, v_texCoord);
        fragColour.rgb += gradientDither(fragColour.rgb);
        return;
    }
    
    // dummy assignment
    fragColour = vec4(0.0);
    switch (u_ssaaMode) {
        case 0:
            fragColour = ssaa(u_colorTexture, u_ssaaFactor, v_texCoord);
            break;
        case 1:
            fragColour = wgssaa(u_colorTexture, u_ssaaFactor, v_texCoord);
            break;
        case 2:
            fragColour = psssaa(u_colorTexture, v_texCoord);
            break;
    }

    fragColour.rgb += gradientDither(fragColour.rgb);
}