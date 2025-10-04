
// Fog calculation function for consistent fog across all shaders
// Supports linear, exponential and exp2 fog types

// Define fog parameters needed by any shader using fog
uniform vec4 fogColour;
uniform vec4 horizonColour;

uniform float fogStart;
uniform float fogEnd;
uniform bool fogEnabled;
uniform int fogType; // 0 = linear, 1 = exp, 2 = exp2
uniform float fogDensity; // For exp and exp2 fog

// Calculate fog factor based on fog type and depth
float calculateFogFactor(float fogDepth) {
    float fogFactor = 1.0;
    
    switch (fogType) {
        case 0:
    // Linear fog
            fogFactor = (fogEnd - fogDepth) / (fogEnd - fogStart);
            break;
        
        case 1:
    // Exponential fog
            fogFactor = exp(-fogDensity * fogDepth);
            break;
        
        case 2:
    // Exponential squared fog
            fogFactor = exp(-fogDensity * fogDensity * fogDepth * fogDepth);
            break;
    }
    
    return clamp(fogFactor, 0.0, 1.0);
}

// Apply fog to color if enabled
vec4 applyFog(vec4 color, vec4 viewPosition) {
    if (!fogEnabled) {
        return color;
    }
    
    float fogDepth = length(viewPosition.xyz);
    float fogFactor = calculateFogFactor(fogDepth);
    return mix(fogColour, color, fogFactor);
}