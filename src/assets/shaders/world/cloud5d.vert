#version 440

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 texCoord;
layout (location = 2) in vec4 color;

centroid out vec2 texCoords;
out vec4 colour;
out vec4 viewPosition;
out vec3 worldPos;

uniform mat4 uMVP;
uniform mat4 uModelView;
uniform mat4 uModel;

void main() {
    gl_Position = uMVP * vec4(vPos, 1.0);
    viewPosition = uModelView * vec4(vPos, 1.0);
    worldPos = vPos; // pass world pos for parallax/noise
    texCoords = texCoord;
    colour = color;
}