// Round 1, "Sumi Ink": the void as a wet blot of ink dragging a calligraphy stroke. Dry-brush gaps open up towards
// the tail, the edges bleed, and droplets splatter off. The orb's veins still swirl in the blot.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/dark_orb.gdshaderinc"

uniform vec2 card_size = vec2(2.6, 1.5);
uniform vec2 card_centre = vec2(-0.55, 0.0);
uniform float size = 1.0;

group_uniforms ink;
uniform float stroke_length : hint_range(0.2, 2.5) = 1.0;
uniform float stroke_width : hint_range(0.3, 2.5) = 1.0;
uniform float dry_brush : hint_range(0.0, 1.0) = 0.6;
uniform float bleed : hint_range(0.0, 2.0) = 1.0;
uniform float splatter : hint_range(0.0, 2.0) = 1.0;
uniform float sway : hint_range(0.0, 2.0) = 1.0;
group_uniforms;

void fragment() {
	vec2 local = (UV - 0.5) * card_size * vec2(1.0, -1.0) + card_centre;
	vec2 q = local / max(size, 0.01);
	float edge = 1.0 - smoothstep(0.35, 0.6, length(local));
	vec4 acc = bent_screen(orb_lens(q, t, bend) * size - local, edge, VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);

	// The stroke: widest at the blot, thinning and drying out towards the tail.
	float len = 1.1 * stroke_length * max(trail, 0.3) * 2.2;
	float s = clamp(-q.x / len, 0.0, 1.0);
	float centre_y = sin(q.x * 5.0 + spin * 0.4) * 0.045 * sway * s;
	float width = 0.17 * stroke_width * pow(1.0 - s, 0.6) * aura;
	float rough = (effect_fbm(vec2(q.x * 6.0, q.y * 6.0 + t * 0.5)) - 0.5) * 0.5 * bleed;
	float body = (1.0 - smoothstep(width * (1.0 + rough) - 0.006, width * (1.0 + rough), abs(q.y - centre_y))) * step(q.x, 0.05);
	float bristles = effect_noise(vec2(q.x * 2.5 - t * 0.6, (q.y - centre_y) / max(width, 0.01) * 9.0));
	float dry = step(bristles, 1.0 - dry_brush * smoothstep(0.2, 1.0, s));
	float fade = 1.0 - smoothstep(0.8, 1.0, s);
	lay(acc, mix(core_color.rgb, glow() * 0.4, s * 0.6), body * dry * fade);
	// A thin pale wet edge on the stroke's top side.
	float rim = (1.0 - smoothstep(0.0, 0.006, abs(q.y - centre_y - width * (1.0 + rough)))) * (1.0 - s) * step(q.x, 0.0);
	lay(acc, accent_color.rgb, rim * 0.7 * dry);

	// Splatter: droplets flicked off the stroke, a few new ones every few frames.
	for (int i = 0; i < 26; i++) {
		if (float(i) >= 16.0 * splatter) { break; }
		float h = effect_hash(vec2(float(i), 6.6));
		float h2 = effect_hash(vec2(float(i), 1.9));
		float life = fract(t * (0.9 + h2) + h);
		vec2 at = vec2(-h * len * 0.8, sign(h2 - 0.5) * (0.12 + 0.25 * h2 * life) * aura);
		float radius = (0.008 + 0.018 * effect_hash(vec2(float(i), 4.4))) * (1.0 - life * 0.6);
		float dot_shape = length((q - at) * vec2(1.0, 1.0 + 0.6 * h));
		lay(acc, core_color.rgb, (1.0 - smoothstep(radius * 0.8, radius, dot_shape)) * (1.0 - smoothstep(0.7, 1.0, life)));
	}

	// The blot: a soft-edged ink drop with the orb's core inside it.
	float r = length(q);
	float blot_r = ORB_RADIUS * 1.25 * (1.0 + (effect_fbm(vec2(atan(q.y, q.x) * 2.0, t * 1.5)) - 0.5) * 0.35 * bleed);
	lay(acc, core_color.rgb, 1.0 - smoothstep(blot_r - 0.01, blot_r, r));
	orb_core(acc, q * 1.15, t, spin);
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
