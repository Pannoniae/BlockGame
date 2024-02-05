#version 440

layout(location = 0) out vec4 oColor;

in vec3 color;
in vec2 texCoords;

uniform vec4 uColor;
uniform sampler2D tex;

void main() {
    vec4 texColor = texture(tex, texCoords);
    oColor = mix(texColor, uColor, 0.2);
}
