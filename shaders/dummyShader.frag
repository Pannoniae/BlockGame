#version 440
#extension GL_NV_command_list : enable

//layout(commandBindableNV) uniform;

// don't, glass will be fucked
layout(early_fragment_tests) in;
layout(location = 0) out vec4 color;

centroid in vec2 texCoords;

uniform sampler2D blockTexture;

void main() {
    color = texture(blockTexture, texCoords);
}