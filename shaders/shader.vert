#version 440

layout (location = 0) in uvec3 vPos;
layout (location = 1) in uvec2 texCoord;
layout (location = 2) in uint iData;

uniform mat4 uMVP;
uniform vec3 uChunkPos;


out vec2 texCoords;
out float ao;
flat out uint direction;
out vec4 light;

out vec3 vertexPos;
out vec3 vertexPosFromCamera;

uniform vec3 uCameraPos;
uniform sampler2D lightTexture;

const float aoArray[4] = float[](1.0, 0.75, 0.5, 0.25);

void main() {
    uint directionValue = iData & 0x7u;
    uint aoValue = (iData >> 3) & 0x3u;
    uint lightValue = (iData >> 8) & 0xFFu;
    vec3 pos = uChunkPos + vPos / 256. - 16;
    gl_Position = uMVP * vec4(pos, 1.0);
    texCoords = texCoord / 32768.;
    ao = aoArray[aoValue];
    direction = directionValue;
    ivec2 lightCoords = ivec2((lightValue >> 4) & 0xFu, lightValue & 0xFu);
    light = texelFetch(lightTexture, lightCoords, 0);
    vertexPos = pos;
    vertexPosFromCamera = pos - uCameraPos;
}
