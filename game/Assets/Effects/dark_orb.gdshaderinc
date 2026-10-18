// The dark orb shared by the fireball, its startup charge and its impact: look settings, the uneven heartbeat, the
// lens that bends what is behind it, and layer helpers. Positions are in metres on the effect's card, +X the way
// the projectile flies. Every effect is driven by `t` (seconds from its logic frame), never TIME, so it follows
// rollback and hitstop.
#include "res://Assets/Effects/effect_noise.gdshaderinc"

const float ORB_RADIUS = 0.19;

// Look (developer settings, presentation): Lukas' pick from the aura mockups. The ranges and groups are the VFX Lab's
// sliders (ADR-0029).
group_uniforms palette;
uniform vec4 core_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform vec4 glow_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform vec4 accent_color : source_color = vec4(0.953, 0.761, 1.0, 1.0);
group_uniforms orb;
uniform float intensity : hint_range(0.0, 3.0) = 0.98;
uniform float aura : hint_range(0.3, 2.5) = 0.92;
uniform float trail : hint_range(0.0, 2.5) = 0.3;
uniform float pulse : hint_range(0.0, 3.0) = 1.0;
uniform float particles : hint_range(0.0, 2.0) = 1.39;
uniform float bend : hint_range(0.0, 3.0) = 1.13;
group_uniforms;

// Set by the effect's node every displayed frame.
uniform float t = 0.0;
uniform float spin = 0.0;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

// Premultiplied layer stack: each call puts a layer of colour `c` and coverage `a` on top.
void lay(inout vec4 acc, vec3 c, float a) {
	a = clamp(a, 0.0, 1.0);
	acc = vec4(c * a, a) + acc * (1.0 - a);
}

float seg_dist(vec2 p, vec2 a, vec2 b) {
	vec2 pa = p - a;
	vec2 ba = b - a;
	return length(pa - ba * clamp(dot(pa, ba) / max(dot(ba, ba), 1e-6), 0.0, 1.0));
}

vec3 glow() {
	return glow_color.rgb * intensity;
}

// The heartbeat: two drifting rhythms plus rare hard spikes, so it never pulses evenly.
float throb(float time) {
	float n = effect_noise(vec2(time * 2.3, 1.7)) * 0.6 + effect_noise(vec2(time * 5.7, 4.2)) * 0.4;
	return n + pow(effect_noise(vec2(time * 1.4, 9.1)), 4.0) * 2.4;
}

float core_radius(float time, float angle) {
	float wobble = (effect_noise(vec2(angle * 2.5 + time * 3.0, time * 4.0)) - 0.5) * 0.09 * pulse;
	return ORB_RADIUS * (0.9 + 0.1 * throb(time) * pulse + wobble);
}

// Shockwave rings on an irregular beat: some beats fire, some don't.
float shock(float r, float time) {
	float beat = time * 1.6;
	float age = fract(beat);
	if (effect_hash(vec2(floor(beat), 3.7)) < 0.35) {
		return 0.0;
	}
	float ring = ORB_RADIUS * (1.0 + age * 2.6 * aura);
	float width = 0.015 + 0.03 * age;
	return (1.0 - smoothstep(0.0, width, abs(r - ring))) * pow(1.0 - age, 2.0) * min(pulse, 1.5);
}

// Where the background is seen from near the orb: pulled in and twisted round it, rippled by the shockwaves.
vec2 orb_lens(vec2 q, float time, float strength) {
	float r = length(q);
	float near = max(r - ORB_RADIUS, 0.0);
	float pull = strength * 0.34 * exp(-near * 5.0);
	float twist = strength * 1.1 * exp(-near * 4.0);
	vec2 turned = vec2(cos(twist) * q.x - sin(twist) * q.y, sin(twist) * q.x + cos(twist) * q.y);
	return turned * (1.0 - pull + 0.12 * shock(r, time) * strength);
}

// The screen behind the card, seen through a local displacement (metres on the card), faded in by `mask` so the
// card's edge blends into what is around it. The camera faces the plane square-on, so this is exact.
vec4 bent_screen(vec2 displacement, float mask, vec3 vertex, mat4 model, mat4 view, mat4 projection, vec2 screen_uv) {
	vec3 world_shift = (model * vec4(displacement * mask, 0.0, 0.0)).xyz;
	vec4 here = projection * vec4(vertex, 1.0);
	vec4 there = projection * vec4(vertex + (view * vec4(world_shift, 0.0)).xyz, 1.0);
	vec2 uv = screen_uv + (there.xy / there.w - here.xy / here.w) * 0.5;
	vec3 seen = textureLod(screen_texture, uv, 0.0).rgb;
	return vec4(seen * mask, mask);
}

// Cel-shaded glow round the rim, shockwaves, sparks drawn in and swallowed, and embers flung off the back.
void orb_halo(inout vec4 acc, vec2 q, float time, float turn, float spark_reach) {
	float r = length(q);
	float angle = atan(q.y, q.x);
	float rim = core_radius(time, angle);
	float beat = throb(time) * pulse;
	float g = exp(-max(r - rim, 0.0) * (7.5 / aura)) * (0.55 + 0.22 * beat);
	float banded = mix(g, floor(g * 5.0) / 5.0, 0.7);
	vec3 halo = mix(glow() * 0.7, glow(), smoothstep(0.15, 0.7, banded));
	halo = mix(halo, accent_color.rgb, smoothstep(0.72, 1.0, banded));
	lay(acc, halo, banded * 0.95 * step(rim, r));
	lay(acc, mix(glow(), accent_color.rgb, 0.6), shock(r, time) * 0.85);
	for (int i = 0; i < 40; i++) {
		if (float(i) >= 8.0 + 30.0 * particles) {
			break;
		}
		float h = effect_hash(vec2(float(i), 1.3));
		float h2 = effect_hash(vec2(float(i), 7.9));
		float u = fract(time / (0.45 + 0.6 * h2) + h);
		float u0 = max(u - 0.08, 0.0);
		float far = ORB_RADIUS * (2.0 + 2.4 * h2) * aura * spark_reach;
		float r1 = mix(far, ORB_RADIUS, u * u);
		float r0 = mix(far, ORB_RADIUS, u0 * u0);
		float a1 = h * TAU + u * 3.0 + turn * 0.15;
		float a0 = h * TAU + u0 * 3.0 + turn * 0.15;
		float d = seg_dist(q, r0 * vec2(cos(a0), sin(a0)), r1 * vec2(cos(a1), sin(a1)));
		float w = 0.003 + 0.006 * u;
		lay(acc, mix(glow(), accent_color.rgb, u), (1.0 - smoothstep(w * 0.4, w, d)) * smoothstep(0.0, 0.25, u));
	}
}

void orb_embers(inout vec4 acc, vec2 q, float time) {
	for (int i = 0; i < 28; i++) {
		if (float(i) >= 6.0 + 20.0 * particles) {
			break;
		}
		float h = effect_hash(vec2(float(i), 4.1));
		float h2 = effect_hash(vec2(float(i), 2.2));
		float u = fract(time * (0.8 + 0.7 * h2) + h);
		float side = (h - 0.5) * 2.0;
		vec2 start = 0.2 * vec2(-cos(side * 1.2), sin(side * 1.2));
		vec2 pos = start + vec2(-u * 0.9 * max(trail, 0.3), side * 0.2 * u + sin(u * 6.0 + h * 9.0) * 0.03);
		float radius = 0.016 * (1.0 - u) + 0.003;
		lay(acc, mix(accent_color.rgb, glow(), u), (1.0 - smoothstep(radius * 0.5, radius, length(q - pos))) * (1.0 - u));
	}
}

// The void itself: swirling veins inside, a crisp rim, a flickering second ring, rim lightning and a glint on spikes.
void orb_core(inout vec4 acc, vec2 q, float time, float turn) {
	float r = length(q);
	float angle = atan(q.y, q.x);
	float rim = core_radius(time, angle);
	float beat = throb(time) * pulse;
	if (r < rim) {
		float x = r / rim;
		float veins = effect_fbm(vec2(angle * 2.0 + turn * 0.35 + x * 3.0, x * 5.0 - time * 2.5));
		float vein = smoothstep(0.52, 0.58, veins) * (1.0 - smoothstep(0.6, 0.68, veins));
		vec3 c = mix(core_color.rgb, glow() * 0.25, pow(x, 3.0));
		c = mix(c, mix(glow(), accent_color.rgb, 0.3), vein * pow(x, 1.3) * (0.55 + 0.35 * beat));
		lay(acc, mix(core_color.rgb, c, smoothstep(0.12, 0.45, x)), 1.0);
	}
	lay(acc, accent_color.rgb, 1.0 - smoothstep(0.004, 0.012, abs(r - rim)));
	float flicker = 0.4 + 0.6 * step(0.5, effect_noise(vec2(time * 18.0, 1.0)));
	lay(acc, glow(), (1.0 - smoothstep(0.003, 0.008, abs(r - rim * (1.13 + 0.05 * beat)))) * flicker);
	float bolt = floor(time * 20.0);
	float bolt_r = rim * (1.05 + 0.18 * effect_noise(vec2(angle * 9.0, bolt)));
	float on = step(0.6, effect_noise(vec2(angle * 1.6 + bolt * 0.7, bolt * 0.3)));
	lay(acc, accent_color.rgb, (1.0 - smoothstep(0.002, 0.007, abs(r - bolt_r))) * on * min(particles, 1.0));
	vec2 g = q - rim * 0.75 * vec2(cos(0.9), sin(0.9));
	float star = max(0.0, 1.0 - abs(g.x) * 70.0 - abs(g.y) * 6.0) + max(0.0, 1.0 - abs(g.y) * 70.0 - abs(g.x) * 6.0);
	lay(acc, vec3(1.0), clamp(star, 0.0, 1.0) * smoothstep(1.0, 1.7, beat));
}
