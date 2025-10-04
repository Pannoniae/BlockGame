#version 440

in vec4 vColor;
centroid in vec2 vTexCoords;

layout(location = 0) out vec4 FragColor;

uniform sampler2D tex;

void main() {
    vec4 texColor = texture(tex, vTexCoords);
    

    // Discard fully transparent pixels
    if (vColor.a <= 0.0) {
        discard;
    }
    FragColor = texColor * vColor;
}