#version 440 compatibility

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 texCoord;
layout (location = 2) in vec4 colour;
layout (location = 3) in uvec2 vLight;


centroid out vec2 texCoords;
out vec4 tint;
out vec4 lightColour;

uniform mat4 uMVP;
uniform mat4 uModelView;

uniform sampler2D lightTexture;

void main() {
    gl_Position = uMVP * vec4(vPos, 1.0);
    texCoords = texCoord;
    
    uint light = vLight.x & 0xFFu;
    
    // extract skylight and blocklight from packed light data (0-15)
    ivec2 lightCoords = ivec2((light >> 4) & 0xFu, light & 0xFu);
    lightColour = texelFetch(lightTexture, lightCoords, 0);
    
    tint = colour;
}
