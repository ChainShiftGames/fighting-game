// The ice orb's startup at its spawn point: cold mist and frost motes spiral in, the six-fold sigil traces itself
// round the circle, a bright seed grows to the orb's first-frame size, and a ring of frost snaps shut on it just
// before the orb appears (the art node shows the ring model forming over the last part). `progress` runs 0 to 1
// over the startup.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/frost.gdshaderinc"

uniform vec2 card_size = vec2(2.6, 2.6);
uniform float progress = 0.0;
// The orb's size on its first frame, as a share of full size: the seed grows to it.
uniform float spawn_size = 0.2;

const float RING = 0.3;

void motes(inout vec4 acc, vec2 q, float time, float turn) {
	for (int i = 0; i < 34; i++) {
		if (float(i) >= 8.0 + 26.0 * flakes) {
			break;
		}
		float h = effect_hash(vec2(float(i), 4.4));
		float h2 = effect_hash(vec2(float(i), 0.7));
		float u = fract(time * (0.8 + 0.9 * h2) + h);
		float far = RING * (2.2 + 1.8 * h2) * (1.2 - 0.4 * progress);
		float r1 = mix(far, RING * 0.25, u * u);
		float r0 = mix(far, RING * 0.25, max(u - 0.07, 0.0) * max(u - 0.07, 0.0));
		float a1 = h * TAU + u * 2.4 + turn * 0.2;
		float a0 = a1 - 0.17;
		float d = frost_segment(q, r0 * vec2(cos(a0), sin(a0)), r1 * vec2(cos(a1), sin(a1)));
		float w = 0.004 + 0.007 * u;
		vec3 colour = mix(frost_glow(), mix(frost_color.rgb, aether_color.rgb, step(0.75, h)), u);
		frost_lay(acc, colour, (1.0 - smoothstep(w * 0.4, w, d)) * smoothstep(0.0, 0.2, u) * (0.4 + 0.6 * progress));
	}
}

void fragment() {
	vec2 q = (UV - 0.5) * card_size * vec2(1.0, -1.0);
	float r = length(q);
	float time = t;
	float turn = spin;
	float seed_radius = RING * spawn_size * smoothstep(0.25, 1.0, progress);

	float air = (1.0 - smoothstep(RING * 0.8, RING * 3.0, r)) * shimmer * progress;
	vec2 pull = r > 1e-4 ? -q / r * 0.015 * progress : vec2(0.0);
	vec4 acc = frost_screen(pull, clamp(air, 0.0, 1.0), VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);

	// Mist gathering: a churning ring that closes in on the spawn point.
	float gather = mix(RING * 3.2, RING * 0.6, progress);
	float ring_mist = exp(-pow((r - gather) / (RING * 0.6), 2.0)) * frost_mist(q / (gather + RING), time * 2.0) * 1.6;
	frost_lay(acc, mix(deep_color.rgb * 1.5, frost_glow(), progress), ring_mist * 0.7);

	motes(acc, q, time, turn);

	float drawn = smoothstep(0.0, 0.8, progress);
	float closing = mix(1.0, 0.75, smoothstep(0.8, 1.0, progress));
	float rune = frost_sigil(q, RING * 1.5 * closing, turn * 0.3, drawn) * sigil;
	frost_lay(acc, mix(aether_color.rgb, frost_color.rgb, progress) * (0.7 + 0.5 * intensity), rune * 0.85);

	float seed_glow = exp(-max(r - seed_radius, 0.0) * 14.0 / max(halo, 0.05)) * smoothstep(0.1, 0.5, progress);
	frost_lay(acc, mix(frost_glow(), frost_color.rgb, seed_glow), seed_glow * 0.8);
	frost_lay(acc, frost_color.rgb * 1.2, 1.0 - smoothstep(seed_radius * 0.8, seed_radius, r));

	// The snap: a thin frost ring shrinks onto the seed over the last frames.
	float snap = smoothstep(0.82, 1.0, progress);
	float snap_r = mix(RING * 1.6, seed_radius, snap);
	frost_lay(acc, frost_color.rgb * 1.3, (1.0 - smoothstep(0.006, 0.016, abs(r - snap_r))) * snap * (1.0 - snap * 0.3));

	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
