#version 440 compatibility

layout (location = 0) in vec3 vPos;

uniform mat4 uView;
uniform mat4 uProjection;

void main() {
    gl_Position = uProjection * uView * vec4(vPos, 1.0);
}
