#version 330 core

layout (location = 0) in vec3 aVertex;
layout (location = 1) in vec4 aColor;
layout (location = 2) in vec2 aTexcoord;

out vec4 color;
out vec2 texcoord;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    gl_Position = projection * view * model * vec4(aVertex, 1.0);
    
    color = aColor;
    texcoord = aTexcoord;
}