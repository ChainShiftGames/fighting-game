// The ice orb's light on the floor: a soft cyan pool under it, stretched along its path like a reflection on
// polished stone, with a few glints in it. A flat card lying on the floor: it tints the floor toward its colour,
// so it reads on pale stone too, and adds its glints.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/effect_noise.gdshaderinc"

uniform vec4 glow_color : source_color = vec4(0.18, 0.7, 1.0, 1.0);
// How strongly the pool's middle tints the floor (0 to 1).
uniform float tint : hint_range(0.0, 1.0) = 0.7;
// 0 = no light; the art node fades it with the orb's size and height.
uniform float strength : hint_range(0.0, 2.0) = 1.0;
uniform float t = 0.0;

void fragment() {
	vec2 p = (UV - 0.5) * 2.0;
	// Stretched along the path, softer behind (the orb flies +X before the node mirrors it).
	vec2 s = vec2(p.x < 0.0 ? p.x * 0.7 : p.x, p.y * 1.6);
	float pool = exp(-dot(s, s) * 3.2);
	float streak = exp(-p.y * p.y * 40.0) * exp(-abs(p.x) * 2.5) * 0.6;
	float shimmer = 0.85 + 0.15 * effect_noise(p * 6.0 + vec2(t * 2.0, 0.0));
	float glint = step(0.985, effect_hash(floor((p + vec2(t * 0.3, 0.0)) * 24.0))) * pool * 2.0;
	float cover = clamp((pool * shimmer + streak) * tint * strength, 0.0, 1.0);
	ALBEDO = glow_color.rgb * cover + glow_color.rgb * glint * strength;
	ALPHA = cover;
}
