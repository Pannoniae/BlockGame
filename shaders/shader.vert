#version 440

layout (location = 0) in uvec3 vPos;
layout (location = 1) in uvec2 texCoord;
layout (location = 2) in vec4 colour;

uniform mat4 uMVP;
uniform vec3 uChunkPos;
uniform vec3 uCameraPos;
uniform float uSkyDarken;

out vec2 texCoords;
out float skyDarken;
out vec4 tint;

out float vertexDist;

const float m = 1 / 256.;
const float n = 1 / 32768.;

void main() {
    vec3 pos = uChunkPos + ((vPos * m) - 16);
    gl_Position = uMVP * vec4(pos, 1.0);
    texCoords = texCoord * n;
    skyDarken = uSkyDarken;
    tint = colour;
    vertexDist = length(pos - uCameraPos);
}
