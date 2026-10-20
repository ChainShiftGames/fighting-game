// Where the ice orb ends, by kind: 0 hit, 1 block, 2 clash, 3 fizzle (lifetime ran out). A hit flashes white then
// cyan, bursts ice spikes out of the centre, throws a cracked frost shockwave and shards that fall as they fly, and
// leaves a snow puff and lingering glints. A block cracks against a hexagonal frost shield and its shards bounce
// back; a clash is a smaller hit; a fizzle melts into mist and falling snow. `age` is in logic frames since the event.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/frost.gdshaderinc"

group_uniforms burst;
uniform float shards : hint_range(0.0, 2.0) = 2.0;
uniform float spikes : hint_range(0.0, 2.0) = 2.0;
// Shards' fall, in metres per second squared.
uniform float gravity : hint_range(0.0, 20.0) = 12.0;
group_uniforms;
uniform vec2 card_size = vec2(2.6, 2.6);
uniform float age = 0.0;
uniform int kind = 0;
uniform float lifetime = 30.0;

const float RING = 0.3;

void flash(inout vec4 acc, vec2 p, float f, float scale) {
	float r = length(p);
	if (f < 1.0) {
		frost_lay(acc, vec3(1.0), 1.0 - smoothstep(RING * 1.2 * scale, RING * 1.5 * scale, r));
	} else if (f < 3.0) {
		frost_lay(acc, frost_glow(), (1.0 - smoothstep(RING * 1.3 * scale, RING * 1.45 * scale, r)) * 0.8);
		frost_lay(acc, frost_color.rgb, 1.0 - smoothstep(0.008, 0.02, abs(r - RING * 1.4 * scale)));
	}
}

void ice_spikes(inout vec4 acc, vec2 p, float f, float scale) {
	float grow = smoothstep(0.0, 3.0, f) * (1.0 - smoothstep(7.0, 12.0, f));
	for (int i = 0; i < 10; i++) {
		float h = effect_hash(vec2(float(i), 7.7));
		float angle = float(i) / 10.0 * TAU + h * 0.5;
		float length_ = (0.35 + 0.35 * h) * scale * spikes * grow;
		vec2 tip = length_ * vec2(cos(angle), sin(angle));
		float a = frost_shard(p - tip * 0.5, angle, length_ * 0.5 + 1e-4, 0.035 * scale * grow + 1e-4);
		frost_lay(acc, mix(frost_color.rgb, glow_color.rgb, h * 0.6), a);
	}
}

void shockwave(inout vec4 acc, vec2 p, float n, float scale) {
	float r = length(p);
	float angle = atan(p.y, p.x);
	float ring_r = (0.15 + 0.95 * sqrt(n)) * scale;
	// Cracked: the ring breaks into arcs.
	float arcs = step(0.25, effect_noise(vec2(angle * 4.0, 2.0)));
	float ring = exp(-pow((r - ring_r) / (0.03 * scale), 2.0)) * pow(1.0 - n, 1.5) * arcs;
	frost_lay(acc, mix(frost_color.rgb, frost_glow(), n), ring);
	float rune = frost_sigil(p, ring_r * 0.8, n * 2.0, 1.0) * sigil * pow(1.0 - n, 2.0);
	frost_lay(acc, aether_color.rgb, rune * 0.6);
}

void flying_shards(inout vec4 acc, vec2 p, float seconds, float n, float scale, float back) {
	for (int i = 0; i < 18; i++) {
		if (float(i) >= 4.0 + 14.0 * shards) {
			break;
		}
		float h = effect_hash(vec2(float(i), 1.1));
		float h2 = effect_hash(vec2(float(i), 9.9));
		float angle = h * TAU;
		vec2 velocity = (1.4 + 1.6 * h2) * scale * vec2(cos(angle) - back, sin(angle) + 0.5);
		vec2 pos = velocity * seconds - vec2(0.0, 0.5 * gravity * seconds * seconds);
		float a = frost_shard(p - pos, angle + seconds * 12.0 * (h2 - 0.5), 0.06 * scale, 0.02 * scale) * (1.0 - n);
		frost_lay(acc, mix(frost_color.rgb, glow_color.rgb, h2), a);
	}
}

void snow_puff(inout vec4 acc, vec2 p, float n, float scale, float time) {
	float radius = (0.25 + 0.7 * sqrt(n)) * scale;
	float puff = frost_mist(p / radius, time * 0.5) * 1.5 * (1.0 - n);
	frost_lay(acc, mix(frost_color.rgb, deep_color.rgb * 2.0, n * 0.7), puff * 0.7);
}

void glitter(inout vec4 acc, vec2 p, float n, float scale) {
	for (int i = 0; i < 12; i++) {
		if (float(i) >= 3.0 + 9.0 * glints) {
			break;
		}
		float h = effect_hash(vec2(float(i), 5.5));
		float h2 = effect_hash(vec2(float(i), 2.8));
		vec2 pos = (0.2 + 0.7 * h2) * scale * vec2(cos(h * TAU), sin(h * TAU)) * sqrt(n);
		float flash = sin(clamp(n * (2.0 + 3.0 * h) - h, 0.0, 1.0) * 3.14159);
		frost_lay(acc, frost_color.rgb * 1.3, frost_glint(p - pos, 0.08 * scale * flash) * flash);
	}
}

// The block's frost shield: a hexagon-tiled disc facing back the way the orb came, cracking as it fades.
void shield(inout vec4 acc, vec2 p, float n) {
	vec2 s = p * vec2(2.2, 1.0);
	float r = length(s);
	float disc = (1.0 - smoothstep(RING * 1.5, RING * 1.6, r)) * step(-0.05, p.x);
	vec2 hex = s * 9.0;
	vec2 cell = hex - vec2(floor(hex.x + 0.5 * mod(floor(hex.y), 2.0)), floor(hex.y)) - 0.5;
	float edge = smoothstep(0.38, 0.48, max(abs(cell.x), abs(cell.y)));
	float crack = 1.0 - smoothstep(0.004, 0.012, abs(effect_noise(p * 14.0) - 0.5) * 0.06 + abs(p.y - (effect_noise(p * 6.0) - 0.5) * 0.3) * smoothstep(0.2, 0.6, n));
	float a = disc * (0.25 + 0.6 * edge) * (1.0 - n);
	frost_lay(acc, mix(frost_glow(), frost_color.rgb, edge), a);
	frost_lay(acc, frost_color.rgb, crack * disc * smoothstep(0.1, 0.3, n) * (1.0 - n));
}

void melt(inout vec4 acc, vec2 p, float n, float time) {
	float r = length(p - vec2(0.0, n * 0.3));
	float body = (1.0 - smoothstep(RING * (1.0 - n), RING * (1.0 - n) + 0.04, r)) * (1.0 - n);
	frost_lay(acc, frost_glow(), body * 0.6);
	snow_puff(acc, p - vec2(0.0, n * 0.25), n, 0.7, time);
	for (int i = 0; i < 14; i++) {
		float h = effect_hash(vec2(float(i), 6.1));
		vec2 pos = vec2((h - 0.5) * RING * 2.0 + sin(n * 6.0 + h * 9.0) * 0.05, -n * (0.4 + 0.5 * h));
		frost_lay(acc, frost_color.rgb, frost_snowflake(p - pos, 0.035, n * 4.0 + h * 6.0) * (1.0 - n));
	}
}

void fragment() {
	vec2 p = (UV - 0.5) * card_size * vec2(1.0, -1.0);
	float f = age;
	float n = clamp(f / max(lifetime * 0.8, 1.0), 0.0, 1.0);
	float seconds = f / 60.0;
	float scale = kind == 1 ? 0.7 : (kind == 2 ? 0.75 : 1.0);
	vec4 acc = vec4(0.0);
	if (kind == 3) {
		melt(acc, p, n, t);
	} else {
		snow_puff(acc, p, n, scale, t);
		shockwave(acc, p, n, scale);
		if (kind == 1) {
			shield(acc, p, n);
		} else {
			ice_spikes(acc, p, f, scale);
		}
		flying_shards(acc, p, seconds, n, scale, kind == 1 ? 1.6 : 0.0);
		glitter(acc, p, n, scale);
		flash(acc, p, f, scale);
	}
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
