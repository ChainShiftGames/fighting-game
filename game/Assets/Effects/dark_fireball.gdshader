// The dark fireball in flight ("Void Vortex"): the orb with its spiral aura arm and short wisp trail, bending the
// screen behind it. One card facing the camera; the node mirrors it for a fireball flying left.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled;

#include "res://Assets/Effects/dark_orb.gdshaderinc"
#include "res://Assets/Effects/toon_effect.gdshaderinc"

// Toon look (developer setting, off until picked): flat bands, ink, held drawings and no bending of the stage.
uniform bool toon_look = false;

group_uniforms vortex;
uniform float arms : hint_range(1.0, 6.0, 1.0) = 1.0;
group_uniforms;
uniform vec2 card_size = vec2(1.8, 1.7);
uniform vec2 card_centre = vec2(-0.05, 0.0);
// Grows from small to full size as it leaves the caster's hands; set per frame.
uniform float size = 1.0;

void wisps(inout vec4 acc, vec2 q, float time, float turn) {
	for (int k = 0; k < 10; k++) {
		float fk = float(k);
		float along = (fk + 1.0) / 10.0;
		vec2 c = vec2(-(fk + 1.0) * 0.1 * trail, sin(turn * 0.6 + fk * 1.1) * 0.06 * along);
		float radius = 0.18 * (1.0 - 0.55 * along);
		vec2 d = (q - c) / radius;
		float puff = effect_fbm(d * 1.5 + vec2(fk * 7.0, time * 1.5));
		float dist = length(d) * 0.5 + (puff - 0.5) * 0.35;
		float a = (1.0 - smoothstep(0.18, 0.48, dist)) * 0.8 * (1.0 - along + 0.06);
		lay(acc, mix(core_color.rgb, glow() * 0.6, 0.45 + 0.4 * along), a);
	}
}

void spiral_arms(inout vec4 acc, vec2 q, float time, float turn) {
	vec2 p = q / (0.625 * aura);
	vec2 s = p;
	s.x *= p.x < 0.0 ? 0.6 : 1.05;
	float radius = length(s);
	float angle = atan(s.y, s.x);
	float wobble = effect_fbm(vec2(angle * 1.2, radius * 3.0 - time * 2.0));
	float phase = fract((arms * angle + radius * 7.0 - turn + wobble * 1.5) / TAU);
	float arm = smoothstep(0.0, 0.08, phase) * (1.0 - smoothstep(0.08, 0.45, phase));
	float leading = smoothstep(0.0, 0.05, phase) * (1.0 - smoothstep(0.05, 0.13, phase));
	float ring = smoothstep(0.25, 0.4, radius) * (1.0 - smoothstep(0.6, 0.97, radius));
	vec3 colour = mix(glow() * 0.75, core_color.rgb, smoothstep(0.45, 0.95, radius));
	colour = mix(colour, accent_color.rgb, leading * (1.0 - smoothstep(0.4, 0.8, radius)));
	lay(acc, colour, arm * ring * 1.15);
}

void fragment() {
	vec2 local = (UV - 0.5) * card_size * vec2(1.0, -1.0) + card_centre;
	vec2 q = local / max(size, 0.01);
	float time = toon_look ? toon_held(t) : t;
	float turn = toon_look ? toon_held_with(spin, t) : spin;
	vec4 acc = vec4(0.0);
	if (toon_look) {
		q += toon_boil_offset(q, time);
	} else {
		float edge = 1.0 - smoothstep(0.55, 0.82, length(local));
		vec2 seen = orb_lens(q, t, bend) * size;
		acc = bent_screen(seen - local, edge, VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);
	}
	wisps(acc, q, time, turn);
	spiral_arms(acc, q, time, turn);
	orb_halo(acc, q, time, turn, 1.0);
	orb_embers(acc, q, time);
	orb_core(acc, q, time, turn);
	if (toon_look) {
		acc = toon_finish(acc, core_color.rgb, accent_color.rgb, VIEWPORT_SIZE.y);
	}
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
