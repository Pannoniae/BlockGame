#version 440 compatibility

#include "/shaders/inc/dither.inc.glsl"

layout (binding = 0) uniform sampler2D u_colorTexture;

centroid in vec2 v_texCoord;

out vec4 fragColour;

void main(void)
{
    // Simple pass-through for no antialiasing
    fragColour = texture(u_colorTexture, v_texCoord);
    
    fragColour.rgb += gradientDither(fragColour.rgb);
}