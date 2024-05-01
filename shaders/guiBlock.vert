#version 440

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 texCoord;
layout (location = 2) in uint iData;

uniform mat4 uMVP;

out vec2 texCoords;
out uint data;
out uint direction;

void main() {
    uint directionValue = iData & 0x7u;
    gl_Position = uMVP * vec4(vPos, 1.0);
    texCoords = texCoord;
    data = iData;
    direction = directionValue;
}
