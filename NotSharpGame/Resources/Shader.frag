#version 330 core

out vec4 fragColor;

in vec4 color;
in vec2 texcoord;

uniform sampler2D uTexture;

void main()
{
    fragColor = texture(uTexture, texcoord) * color;
}