#version 430 core

layout (binding = 0) uniform sampler2D u_colorTexture;

centroid in vec2 v_texCoord;

out vec4 fragColor;

void main(void)
{
    // Simple pass-through for no antialiasing
    fragColor = texture(u_colorTexture, v_texCoord);
}