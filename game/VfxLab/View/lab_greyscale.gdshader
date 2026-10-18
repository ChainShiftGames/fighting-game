// The VFX Lab's greyscale check (dev only): shows what is under it by brightness alone, to judge whether an effect
// reads by value without its colours.
shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

void fragment() {
	vec3 c = textureLod(screen_texture, SCREEN_UV, 0.0).rgb;
	COLOR = vec4(vec3(dot(c, vec3(0.2126, 0.7152, 0.0722))), 1.0);
}
