#version 330

layout(location = 0) in vec3 position;
layout(location = 1) in vec4 color;

out vec3 vWorldPosition;

uniform mat4 uInverseView;
uniform mat4 uInverseProjection;
uniform vec3 cameraPos;

void main()
{
    // Start with the fullscreen quad position (NDC coordinates)
    gl_Position = vec4(position, 1.0);
    
    // Transform NDC back to world space for atmospheric scattering
    // First to view space, then to world space
    vec4 viewPos = uInverseProjection * vec4(position.xy, 1.0, 1.0);
    viewPos /= viewPos.w;
    
    // Transform to world space (far plane direction from camera)
    vec4 worldPos = uInverseView * vec4(viewPos.xyz, 0.0);
    vWorldPosition = normalize(worldPos.xyz) * 450000.0 + cameraPos;
}