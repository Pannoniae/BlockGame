#version 440

in vec4 vColor;
in vec2 vTexCoords;

out vec4 FragColor;

uniform sampler2D tex;

void main() {
    vec4 texColor = texture(tex, vTexCoords);
    FragColor = texColor * vColor;

    // Discard fully transparent pixels
    if (FragColor.a <= 0.0) {
        discard;
    }
}