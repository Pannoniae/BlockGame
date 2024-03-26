#version 440

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 texCoord;
layout (location = 2) in uint iData;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec2 texCoords;
out uint data;

void main() {
    vec3 v2Pos = vPos * 16;
    gl_Position = uProjection * uView * uModel * vec4(v2Pos, 1.0);
    texCoords = texCoord;
    data = iData;
}
