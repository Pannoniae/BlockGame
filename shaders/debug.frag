#version 440

in vec4 vColor;

layout(location = 0) out vec4 FragColor;

void main() {
    FragColor = vColor;

    // Discard fully transparent pixels
    if (FragColor.a <= 0.0) {
        discard;
    }
}