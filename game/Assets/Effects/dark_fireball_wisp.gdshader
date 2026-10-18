// One puff of the dark trail; each instance's colour carries its fade and INSTANCE_CUSTOM.x its noise seed.
shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_disabled;

#include "res://Assets/Effects/effect_noise.gdshaderinc"

uniform float t = 0.0;

varying float seed;

void vertex() {
	seed = INSTANCE_CUSTOM.x;
}

void fragment() {
	vec2 p = UV - 0.5;
	float puff = effect_fbm(p * 5.0 + vec2(seed * 17.0, t * 1.5));
	float dist = length(p) + (puff - 0.5) * 0.35;
	ALBEDO = COLOR.rgb;
	ALPHA = (1.0 - smoothstep(0.18, 0.48, dist)) * COLOR.a;
}
