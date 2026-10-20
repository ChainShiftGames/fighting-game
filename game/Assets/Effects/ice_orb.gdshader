// The ice orb in flight: everything round the spinning ice ring (the ring itself is the ice_orb model with
// ice_orb_crystal). Cold air shimmers behind it, a frost halo glows round it, a six-fold sigil turns slowly at its
// back, and a misty trail streams off behind with snowflakes, tumbling shards and speed streaks; glints flash
// round the rim. One card facing the camera, behind the ring; the node mirrors it for an orb flying left.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/frost.gdshaderinc"

group_uniforms look;
// 0 = soft painted light, 1 = flat cel bands on the glow and mist.
uniform float cel : hint_range(0.0, 1.0) = 0.137;
// The sigil's size against the ring, and how fast it turns against the ring's spin.
uniform float sigil_size : hint_range(0.6, 2.0) = 0.926;
uniform float sigil_turn : hint_range(-2.0, 2.0) = -0.521;
group_uniforms;
uniform vec2 card_size = vec2(2.4, 1.3);
uniform vec2 card_centre = vec2(-0.6, 0.0);
// Grows from small to full size as it leaves the caster's hands; set per frame.
uniform float size = 1.0;

// The ring's outer radius at full size, in metres (the model is 0.6 m across in the art node).
const float RING = 0.3;

float banded(float v) {
	return mix(v, floor(v * 4.0 + 0.5) / 4.0, cel);
}

void trail_mist(inout vec4 acc, vec2 q, float time) {
	float reach = 1.3 * trail;
	if (reach <= 0.0) {
		return;
	}
	float along = clamp(-q.x / reach, 0.0, 1.0);
	float spread = RING * (0.75 - 0.35 * along);
	vec2 m = vec2((q.x + reach * 0.5) / (reach * 0.55), q.y / max(spread, 0.05));
	float body = frost_mist(m * 0.9 + vec2(time * 1.4, 0.0), time) * step(q.x, RING * 0.4);
	body *= smoothstep(1.0, 0.15, along) * smoothstep(-RING * 0.2, RING * 0.3, -q.x + RING);
	vec3 colour = mix(frost_glow(), deep_color.rgb * 1.6, along);
	frost_lay(acc, colour, banded(body) * 0.75);
}

void speed_streaks(inout vec4 acc, vec2 q, float time) {
	for (int i = 0; i < 9; i++) {
		float h = effect_hash(vec2(float(i), 5.3));
		float y = (h - 0.5) * RING * 1.5;
		float run = fract(time * (1.6 + h) + h * 3.0);
		float x0 = -RING * 0.6 - run * 1.1 * trail;
		float len = (0.18 + 0.25 * h) * trail;
		float d = frost_segment(q, vec2(x0, y), vec2(x0 - len, y));
		float a = (1.0 - smoothstep(0.003, 0.009, d)) * (1.0 - run) * smoothstep(0.0, 0.1, run);
		frost_lay(acc, mix(frost_color.rgb, frost_glow(), h), a * 0.8 * min(trail, 1.0));
	}
}

void trailing_flakes(inout vec4 acc, vec2 q, float time, float turn) {
	for (int i = 0; i < 26; i++) {
		if (float(i) >= 4.0 + 22.0 * flakes) {
			break;
		}
		float h = effect_hash(vec2(float(i), 1.9));
		float h2 = effect_hash(vec2(float(i), 8.3));
		float u = fract(time * (0.9 + 0.8 * h2) + h);
		vec2 start = RING * 0.9 * vec2(cos(h * TAU), sin(h * TAU));
		vec2 pos = start + vec2(-u * (0.9 + 0.6 * h2) * max(trail, 0.35), sin(u * 7.0 + h * 11.0) * 0.06 * u);
		float radius = (0.025 + 0.035 * h2) * (1.0 - 0.6 * u);
		float a = frost_snowflake(q - pos, radius, turn * (h - 0.5) * 3.0 + h * 6.0) * (1.0 - u) * smoothstep(0.0, 0.08, u);
		frost_lay(acc, mix(frost_color.rgb, aether_color.rgb, step(0.8, h)), a);
	}
}

void tumbling_shards(inout vec4 acc, vec2 q, float time) {
	for (int i = 0; i < 8; i++) {
		float h = effect_hash(vec2(float(i), 6.6));
		float h2 = effect_hash(vec2(float(i), 2.4));
		float u = fract(time * (0.7 + 0.5 * h2) + h);
		vec2 pos = vec2(-RING * 0.7 - u * 0.8 * max(trail, 0.35), (h - 0.5) * RING * 1.3 + (h2 - 0.5) * 0.2 * u);
		float a = frost_shard(q - pos, h * TAU + u * 9.0 * (h2 - 0.5), 0.05 * (1.0 - 0.5 * u), 0.016) * (1.0 - u);
		frost_lay(acc, mix(glow_color.rgb, frost_color.rgb, 0.5 + 0.5 * h2), a * min(flakes + 0.3, 1.0));
	}
}

void rim_glints(inout vec4 acc, vec2 q, float time, float turn) {
	for (int i = 0; i < 10; i++) {
		if (float(i) >= 2.0 + 8.0 * glints) {
			break;
		}
		float h = effect_hash(vec2(float(i), 3.1));
		float life = fract(time * (1.1 + h) + h * 5.0);
		float a = h * TAU + turn;
		vec2 pos = RING * (0.95 + 0.25 * effect_hash(vec2(float(i), 9.4))) * vec2(cos(a), sin(a));
		float flash = sin(life * 3.14159);
		frost_lay(acc, frost_color.rgb * 1.3, frost_glint(q - pos, 0.07 * flash) * flash);
	}
}

void fragment() {
	vec2 local = (UV - 0.5) * card_size * vec2(1.0, -1.0) + card_centre;
	vec2 q = local / max(size, 0.01);
	float r = length(q);
	float time = t;
	float turn = spin;

	// Cold air: the stage wobbles faintly round and behind the orb.
	float air = (1.0 - smoothstep(RING * 1.2, RING * 2.6, length(q * vec2(0.6, 1.0) + vec2(RING * 0.8, 0.0)))) * shimmer;
	vec2 wobble = (vec2(effect_noise(q * 9.0 + vec2(time * 3.0, 0.0)), effect_noise(q * 9.0 + vec2(0.0, time * 2.4))) - 0.5) * 0.03;
	vec4 acc = frost_screen(wobble, clamp(air, 0.0, 1.0), VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);

	trail_mist(acc, q, time);
	speed_streaks(acc, q, time);

	float glow = exp(-max(r - RING * 0.7, 0.0) * (6.0 / max(halo, 0.05))) * step(0.001, halo);
	vec3 halo_colour = mix(frost_glow() * 0.6, frost_color.rgb, smoothstep(0.55, 1.0, glow));
	frost_lay(acc, halo_colour, banded(glow) * 0.7);

	float rune = frost_sigil(q, RING * sigil_size, turn * sigil_turn, 1.0) * sigil;
	frost_lay(acc, mix(aether_color.rgb, frost_color.rgb, 0.35) * (0.8 + 0.4 * intensity), rune * 0.75);

	tumbling_shards(acc, q, time);
	trailing_flakes(acc, q, time, turn);
	rim_glints(acc, q, time, turn);

	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
