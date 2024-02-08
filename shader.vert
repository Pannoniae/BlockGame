#version 440

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 texCoord;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec2 texCoords;

void main() {
    gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
    texCoords = texCoord;
}
