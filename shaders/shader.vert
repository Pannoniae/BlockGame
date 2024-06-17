#version 440

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 texCoord;
layout (location = 2) in uint iData;

uniform mat4 uMVP;
uniform vec3 uChunkPos;


out vec2 texCoords;
out float ao;
out uint direction;
out vec4 light;

out vec3 vertexPos;

uniform sampler2D lightTexture;

const float aoArray[4] = float[](1.0, 0.75, 0.5, 0.25);

void main() {
    uint directionValue = iData & 0x7u;
    uint aoValue = (iData >> 3) & 0x3u;
    uint lightValue = (iData >> 8) & 0xFFu;
    gl_Position = uMVP * vec4(uChunkPos + vPos, 1.0);
    texCoords = texCoord;
    ao = aoArray[aoValue];
    direction = directionValue;
    uint skylight = lightValue & 0xFu;
    uint blocklight = (lightValue >> 4) & 0xFu;
    ivec2 lightCoords = ivec2(blocklight, skylight);
    light = texelFetch(lightTexture, lightCoords, 0);
    vertexPos = uChunkPos + vPos;
}
