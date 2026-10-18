// Round 1, "Eclipse": a black sun. A flame corona round the void, two tilted orbit rings whose comets pass behind and
// in front of the core, and speed streaks behind. Ported from the aura mockups.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled;

#include "res://Assets/Effects/dark_orb.gdshaderinc"

uniform vec2 card_size = vec2(2.0, 1.8);
uniform vec2 card_centre = vec2(-0.1, 0.0);
uniform float size = 1.0;

group_uniforms eclipse;
uniform float comets : hint_range(1.0, 6.0, 1.0) = 2.0;
uniform float ring_tilt : hint_range(-1.5, 1.5) = 0.38;
uniform float ring_flatness : hint_range(0.05, 1.0) = 0.28;
uniform float corona : hint_range(0.0, 2.5) = 1.0;
uniform float streaks : hint_range(0.0, 2.0) = 1.0;
group_uniforms;

// One orbit ring: x = how much line shows, y = how near the comet head, z = 1 on the front half.
vec3 orbit(vec2 q, float tilt, float a, float b, float heads, float turn) {
	float c = cos(tilt);
	float s = sin(tilt);
	vec2 u = vec2(c * q.x + s * q.y, -s * q.x + c * q.y);
	float f = (u.x * u.x) / (a * a) + (u.y * u.y) / (b * b) - 1.0;
	vec2 g = vec2(2.0 * u.x / (a * a), 2.0 * u.y / (b * b));
	float d = abs(f) / max(length(g), 1e-4);
	float phi = atan(u.y / b, u.x / a);
	float comet = pow(fract((phi - turn) * heads / TAU), 4.0);
	float line = 1.0 - smoothstep(0.004, 0.014, d);
	float faint = (1.0 - smoothstep(0.0, 0.006, d)) * 0.25;
	return vec3(line * comet + faint, comet, u.y < 0.0 ? 1.0 : 0.0);
}

void fragment() {
	vec2 local = (UV - 0.5) * card_size * vec2(1.0, -1.0) + card_centre;
	vec2 q = local / max(size, 0.01);
	float edge = 1.0 - smoothstep(0.6, 0.9, length(local));
	vec4 acc = bent_screen(orb_lens(q, t, bend) * size - local, edge, VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);
	float r = length(q);
	float angle = atan(q.y, q.x);
	if (q.x < 0.0) {
		float lane = effect_noise(vec2(q.y * 55.0, 3.0));
		float streak = step(0.72, lane) * smoothstep(0.0, 0.25, effect_noise(vec2(q.x * 3.0 + t * 9.0, q.y * 55.0)));
		float fade = (1.0 - smoothstep(0.0, 0.9 * max(trail, 0.3), -q.x)) * (1.0 - smoothstep(0.08, 0.2 * aura, abs(q.y)));
		lay(acc, mix(glow(), accent_color.rgb, 0.4), streak * fade * 0.8 * streaks);
	}
	orb_halo(acc, q, t, spin, 1.0);
	vec3 o1 = orbit(q, ring_tilt, 0.46 * aura, 0.46 * aura * ring_flatness, comets, spin);
	vec3 o2 = orbit(q, -ring_tilt * 1.45, 0.4 * aura, 0.4 * aura * ring_flatness * 0.9, comets, -spin * 1.3 + 2.0);
	if (o1.z < 0.5) { lay(acc, mix(glow(), accent_color.rgb, o1.y), o1.x * 0.7); }
	if (o2.z < 0.5) { lay(acc, mix(glow(), accent_color.rgb, o2.y), o2.x * 0.7); }
	float lick = effect_fbm(vec2(angle * comets * 0.7, t * 3.0)) + 0.4 * effect_fbm(vec2(angle * 5.0, r * 9.0 - t * 7.0));
	float reach = ORB_RADIUS + (0.03 + 0.11 * lick) * aura * corona;
	reach *= 1.0 + 0.6 * max(0.0, -cos(angle)) * trail;
	float tongue = 1.0 - smoothstep(reach - 0.03, reach, r);
	lay(acc, mix(core_color.rgb, mix(glow(), accent_color.rgb, 0.35), smoothstep(reach - 0.05, reach, r)), tongue);
	orb_core(acc, q, t, spin);
	if (o1.z > 0.5) { lay(acc, mix(glow(), accent_color.rgb, o1.y), o1.x); }
	if (o2.z > 0.5) { lay(acc, mix(glow(), accent_color.rgb, o2.y), o2.x); }
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
