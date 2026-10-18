// Round 1, "Grasping Shadow": the void reaches for the opponent. Shadow fingers stretch forward from the orb and
// clench and open as it flies, pale at the tips; a smoky wake trails behind.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/dark_orb.gdshaderinc"

uniform vec2 card_size = vec2(2.2, 1.8);
uniform vec2 card_centre = vec2(0.0, 0.0);
uniform float size = 1.0;

group_uniforms claws;
uniform float fingers : hint_range(2.0, 7.0, 1.0) = 4.0;
uniform float reach : hint_range(0.15, 1.2) = 0.5;
uniform float clench_speed : hint_range(0.0, 4.0) = 1.4;
uniform float curl : hint_range(0.0, 2.5) = 1.1;
uniform float finger_width : hint_range(0.2, 2.5) = 1.0;
uniform float spread : hint_range(0.2, 1.6) = 1.1;
uniform float smoke : hint_range(0.0, 2.0) = 1.0;
group_uniforms;

// How far a point is from finger i, and how far along it (0 knuckle, 1 tip).
vec2 finger(vec2 q, float i, float clench) {
	float n = max(fingers - 1.0, 1.0);
	float side = fingers < 1.5 ? 0.0 : mix(-1.0, 1.0, i / n);
	float base_angle = side * spread * mix(1.0, 0.45, clench);
	float best = 1e3;
	float along = 0.0;
	vec2 prev = ORB_RADIUS * 0.9 * vec2(cos(base_angle), sin(base_angle));
	for (int k = 1; k <= 10; k++) {
		float s = float(k) / 10.0;
		// Fingers bend inwards (towards the line of flight) more the more the hand clenches.
		float bendk = base_angle - side * curl * clench * s * s * 1.4;
		float len = ORB_RADIUS * 0.9 + reach * aura * s * mix(1.0, 0.8, clench);
		vec2 p = len * vec2(cos(bendk), sin(bendk));
		float d = seg_dist(q, prev, p);
		float width = (0.032 * (1.0 - s) + 0.006) * finger_width;
		if (d - width < best) {
			best = d - width;
			along = s;
		}
		prev = p;
	}
	return vec2(best, along);
}

void fragment() {
	vec2 local = (UV - 0.5) * card_size * vec2(1.0, -1.0) + card_centre;
	vec2 q = local / max(size, 0.01);
	float edge = 1.0 - smoothstep(0.6, 0.95, length(local));
	vec4 acc = bent_screen(orb_lens(q, t, bend) * size - local, edge, VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);

	// Smoke wake: puffs that swell and thin as they fall behind.
	for (int k = 0; k < 12; k++) {
		float fk = float(k);
		float along = (fk + 1.0) / 12.0;
		vec2 c = vec2(-(fk + 1.0) * 0.09 * max(trail, 0.25) * 1.6, sin(t * 3.0 + fk * 0.9) * 0.05 * along);
		float radius = (0.13 + 0.1 * along) * smoke;
		float puff = effect_fbm((q - c) / max(radius, 0.01) * 1.3 + vec2(fk * 5.0, t * 1.2));
		float dist = length(q - c) / max(radius, 0.01) + (puff - 0.5) * 0.7;
		lay(acc, mix(core_color.rgb, glow() * 0.5, along), (1.0 - smoothstep(0.35, 0.8, dist)) * 0.75 * (1.0 - along) * min(smoke, 1.0));
	}

	// The hand clenches on an uneven rhythm, snapping shut and easing open.
	float beat = fract(t * clench_speed * 0.5);
	float clench = smoothstep(0.0, 0.12, beat) * (1.0 - smoothstep(0.45, 1.0, beat));
	for (int i = 0; i < 7; i++) {
		if (float(i) >= fingers) { break; }
		vec2 f = finger(q, float(i), clench);
		float body = 1.0 - smoothstep(0.0, 0.006, f.x);
		vec3 colour = mix(core_color.rgb, mix(glow(), accent_color.rgb, 0.8), smoothstep(0.7, 1.0, f.y));
		lay(acc, accent_color.rgb, (1.0 - smoothstep(0.0, 0.004, abs(f.x + 0.002))) * 0.7 * f.y);
		lay(acc, colour, body);
	}
	orb_halo(acc, q, t, spin, 0.8);
	orb_core(acc, q, t, spin);
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
