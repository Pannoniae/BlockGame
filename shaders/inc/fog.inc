
// Fog calculation function for consistent fog across all shaders
// Supports linear, exponential and exp2 fog types

// Define fog parameters needed by any shader using fog
uniform float fogStart;
uniform float fogEnd;
uniform bool fogEnabled;
uniform int fogType; // 0 = linear, 1 = exp, 2 = exp2
uniform float fogDensity;
uniform vec4 fogColour;
uniform vec4 skyColour;

// Standard fog calculation for all non-terrain shaders
float getFog(float fogDepth) {
    // Calculate fog factor based on fog type
    float fogFactor = 1.0;

    if (fogType == 0) {
        // Linear fog
        fogFactor = (fogEnd - fogDepth) / (fogEnd - fogStart);
    } else if (fogType == 1) {
        // Exponential fog
        fogFactor = exp(-fogDensity * fogDepth);
    } else if (fogType == 2) {
        // Exponential squared fog
        fogFactor = exp(-fogDensity * fogDensity * fogDepth * fogDepth);
    }

    return clamp(fogFactor, 0.0, 1.0);
}

vec4 applyFog(vec4 color, float fogDepth) {
    if (!fogEnabled) {
        return color;
    }

    float fogFactor = getFog(fogDepth);
    return mix(fogColour, color, fogFactor);
}