#version 440

layout (location = 0) in uvec3 vPos;
layout (location = 1) in uvec2 texCoord;
layout (location = 2) in vec4 colour;

uniform mat4 uMVP;
uniform vec3 uChunkPos;


out vec2 texCoords;
out vec4 tint;

out float vertexDist;

uniform vec3 uCameraPos;

const float m = 1 / 256.;
const float n = 1 / 32768.;

void main() {
    vec3 pos = uChunkPos + ((vPos * m) - 16);
    gl_Position = uMVP * vec4(pos, 1.0);
    texCoords = texCoord * n;
    tint = colour;
    vertexDist = length(pos - uCameraPos);
}
