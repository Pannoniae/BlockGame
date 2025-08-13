#version 440

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec4 color;

uniform mat4 uMVP;
uniform mat4 uModelView;

out vec4 colour;
out vec4 viewPosition; // Changed from fogDepth to full position

void main() {
    viewPosition = uModelView * vec4(vPos, 1.0); // Pass the full view position
    gl_Position = uMVP * vec4(vPos, 1.0);
    colour = color;
}