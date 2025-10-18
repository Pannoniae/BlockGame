#version 440

layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 texCoord;
layout (location = 2) in vec4 color;

#ifdef HAS_NORMALS
layout (location = 3) in vec4 vNormal;
#endif

// Common vertex outputs
centroid out vec2 texCoords;
out vec4 colour;
out vec4 viewPosition;

#ifdef HAS_NORMALS
out vec3 normal;
#endif

uniform mat4 uMVP;
uniform mat4 uModelView;
#ifdef HAS_NORMALS
uniform mat4 uModel;
#endif

void main() {
    gl_Position = uMVP * vec4(vPos, 1.0);
    viewPosition = uModelView * vec4(vPos, 1.0);
    texCoords = texCoord;
    colour = color;
    #ifdef HAS_NORMALS
    normal = normalize(mat3(transpose(inverse(uModel))) * vec3(vNormal));
    #endif 
}