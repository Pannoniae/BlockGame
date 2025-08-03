#version 440

layout (location = 0) in uvec3 vPos;
layout (location = 1) in uvec2 texCoord;
layout (location = 2) in vec4 colour;

uniform mat4 uMVP;
uniform vec3 uChunkPos;
uniform vec3 uCameraPos;
uniform int uSkyDarken;

out vec2 texCoords;
out int skyDarken;
out vec4 tint;

out float vertexDist;


void main() {
    vec3 pos = uChunkPos + vPos / 256. - 16;
    gl_Position = uMVP * vec4(pos, 1.0);
    texCoords = texCoord / 32768.;
    // compute tint (light * ao * direction)
    // per-face lighting
    // float lColor = a[direction]
    skyDarken = uSkyDarken;
    tint = colour;
    vertexDist = length(pos - uCameraPos);
}
