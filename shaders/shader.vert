#version 440

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 texCoord;
layout (location = 2) in uint iData;

uniform mat4 uMVP;


out vec2 texCoords;
out uint data;
out float ao;
out uint direction;

out vec3 vertexPos;

const float aoArray[4] = float[](1.0, 0.75, 0.5, 0.25);

void main() {
    uint directionValue = iData & 0x7u;
    uint aoValue = (iData >> 3) & 0x3u;
    gl_Position = uMVP * vec4(vPos, 1.0);
    texCoords = texCoord;
    data = iData;
    ao = aoArray[aoValue];
    direction = directionValue;
    vertexPos = vPos;
}
