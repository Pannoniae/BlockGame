#version 440

layout(location = 0) in vec3 vertex;
layout(location = 1) in vec3 icolor;
layout(location = 2) in vec2 texCoord;

uniform mat4 projection;
uniform sampler2D tex;

out vec3 color;
out vec2 texCoords;

void main() {
    gl_Position = projection * vec4(vertex.xyz, 1.0);
    color = icolor;
    texCoords = texCoord;
}
