#version 440

layout(location = 0) out vec4 color;

in vec2 texCoords;

uniform sampler2D blockTexture;

void main() {
    color = texture(blockTexture, texCoords);
    if (color.a < 0.05) {
        discard;
    }
}