#version 440

uniform mat4 uMVP;
uniform mat4 uModelView;

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec4 color;

out vec4 colour;
out vec4 viewPosition;

void main() {
    viewPosition = uModelView * vec4(vPos, 1.0);
    gl_Position = uMVP * vec4(vPos, 1.0);
    colour = color;
}