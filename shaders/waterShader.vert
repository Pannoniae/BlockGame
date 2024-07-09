#version 440

layout (location = 0) in uvec3 vPos;
layout (location = 1) in uvec2 texCoord;
layout (location = 2) in uint iData;

uniform mat4 uMVP;
uniform vec3 uChunkPos;


out vec2 texCoords;
out vec4 tint;

out float vertexDist;

uniform vec3 uCameraPos;
uniform sampler2D lightTexture;

const float aoArray[4] = float[4](1.0, 0.75, 0.5, 0.25);
const float a[6] = float[6](0.8, 0.8, 0.6, 0.6, 0.6, 1);

void main() {
    uint direction = iData & 0x7u;
    uint aoValue = (iData >> 3) & 0x3u;
    uint lightValue = (iData >> 8) & 0xFFu;
    vec3 pos = uChunkPos + vPos / 256. - 16;
    gl_Position = uMVP * vec4(pos, 1.0);
    texCoords = texCoord / 32768.;
    ivec2 lightCoords = ivec2((lightValue >> 4) & 0xFu, lightValue & 0xFu);
    // compute tint (light * ao * direction)
    // per-face lighting
    // float lColor = a[direction]
    tint = texelFetch(lightTexture, lightCoords, 0) * a[direction] * aoArray[aoValue];
    vertexDist = length(pos - uCameraPos);
}
