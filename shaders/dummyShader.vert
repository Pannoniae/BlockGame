#version 440

layout (location = 0) in uvec3 vPos;
layout (location = 1) in uvec2 texCoord;
layout (location = 2) in vec4 colour;

uniform mat4 uMVP;
uniform vec3 uChunkPos;

out vec2 texCoords;

void main() {
    gl_Position = uMVP * vec4(uChunkPos + vPos / 256. - 16, 1.0);
    texCoords = texCoord / 32768.;
}
