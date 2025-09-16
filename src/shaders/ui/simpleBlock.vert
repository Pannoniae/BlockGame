#version 440

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 texCoord;
layout (location = 2) in vec4 tintValue;

uniform mat4 uMVP;


centroid out vec2 texCoords;
out vec4 tint;

out vec3 vertexPos;

void main() {
    gl_Position = uMVP * vec4(vPos, 1.0);
    texCoords = texCoord;
    tint = tintValue;
    vertexPos = vPos;
}
