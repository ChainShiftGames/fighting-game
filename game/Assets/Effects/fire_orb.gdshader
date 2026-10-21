// The fire orb in flight: everything round the molten core (the core itself is the fire_orb model with
// fire_orb_core). Hot air bends the stage round and behind it, a plume of fire and churning smoke streams off
// behind, flame tongues lick round it like a sun's corona with flare arcs looping off its surface, its heartbeat
// throws hot shockwave rings, and speed streaks and a bloom halo sell its weight. One card facing the camera,
// behind the core; the art node scales it as the orb grows and mirrors it for an orb flying left.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/fire.gdshaderinc"

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

group_uniforms flight;
uniform float smoke : hint_range(0.0, 2.0) = 1.0;
uniform float flares : hint_range(0.0, 2.0) = 1.0;
uniform float shockwaves : hint_range(0.0, 2.0) = 1.0;
uniform float streaks : hint_range(0.0, 2.0) = 1.0;
// Heartbeats per second; the core's pulse_rate should match.
uniform float pulse_rate : hint_range(0.0, 6.0) = 2.3;
group_uniforms;
uniform vec2 card_size = vec2(3.6, 1.8);
uniform vec2 card_centre = vec2(-0.8, 0.0);
uniform float t = 0.0;
uniform float spin = 0.0;

// The core's radius in metres before the node's scale (the model is 0.6 m across in the art node).
const float RING = 0.3;

void smoke_trail(inout vec4 acc, vec2 q, float time) {
	float reach = 1.8 * trail;
	if (reach <= 0.0 || smoke <= 0.0) {
		return;
	}
	float along = clamp(-q.x / reach, 0.0, 1.0);
	// The smoke rises as it falls behind, and spreads.
	float rise = along * along * 0.35;
	float spread = RING * (0.8 + 1.1 * along);
	vec2 m = vec2(q.x * 1.4 + time * 2.4, (q.y - rise) / spread);
	float n = effect_fbm(m * 1.6 + vec2(0.0, time * 0.3));
	float body = smoothstep(0.38, 0.75, n) * (1.0 - smoothstep(0.5, 1.15, abs((q.y - rise) / spread)));
	body *= smoothstep(-RING * 0.2, RING * 1.0, -q.x) * (1.0 - smoothstep(0.55, 1.0, along));
	vec3 colour = mix(ember_color.rgb * 0.5, soot_color.rgb, smoothstep(0.1, 0.6, along));
	fire_lay(acc, colour, body * 0.75 * smoke);
}

void flame_plume(inout vec4 acc, vec2 q, float time) {
	float reach = 1.25 * trail;
	if (reach <= 0.0) {
		return;
	}
	float along = clamp(-q.x / reach, 0.0, 1.0);
	float spread = RING * (1.0 - 0.55 * along);
	// Flame noise flowing back along the trail, so the tongues stream off the orb.
	vec2 m = vec2(q.y / spread * 0.9, q.x * 2.2 + time * 4.0);
	float n = fire_flicker(m, time);
	float shape = 1.0 - smoothstep(0.35, 1.0, abs(q.y) / spread);
	float heat = shape * (1.15 - along * 1.3) + (n - 0.5) * 0.9 * shape;
	heat *= step(q.x, RING * 0.2) * (1.0 - smoothstep(0.75, 1.0, -q.x / reach));
	float a = smoothstep(0.08, 0.35, heat);
	fire_lay(acc, fire_ramp(heat), a);
}

void halo(inout vec4 acc, float r, float beat) {
	// Gone well inside the card (its half height is RING * 3), so the card's edge never shows.
	float reach = 1.0 - smoothstep(RING * 1.4, RING * 2.7, r);
	float glow = exp(-max(r - RING, 0.0) * 5.5 / max(corona, 0.05)) * reach;
	fire_add(acc, flame_color.rgb * intensity * 0.45, glow * (0.8 + 0.4 * beat));
	fire_add(acc, ember_color.rgb, exp(-max(r - RING, 0.0) * 4.0) * 0.3 * corona * reach);
}

void shockwave_rings(inout vec4 acc, float r, float time) {
	for (int i = 0; i < 2; i++) {
		float u = fract(time * pulse_rate * 0.5 + float(i) * 0.5);
		float radius = RING * (1.05 + 1.8 * u);
		float width = 0.012 + 0.03 * u;
		float ring = 1.0 - smoothstep(width * 0.3, width, abs(r - radius));
		fire_add(acc, mix(blaze_color.rgb * intensity, ember_color.rgb, u), ring * (1.0 - u) * shockwaves);
	}
}

// Solar flare arcs: loops of plasma lifting off the surface and falling back, turning with the orb.
void flare_arcs(inout vec4 acc, vec2 q, float time, float turn) {
	for (int i = 0; i < 5; i++) {
		float h = effect_hash(vec2(float(i), 2.7));
		float life = fract(time * (0.45 + 0.35 * h) + h);
		float base = h * TAU + turn * (0.4 + h);
		float lift = sin(life * 3.14159) * RING * (0.35 + 0.4 * effect_hash(vec2(float(i), 9.1)));
		vec2 dir = vec2(cos(base), sin(base));
		// The arc is a half circle standing on the surface, centred on the base point.
		vec2 centre = dir * RING;
		vec2 local = fire_rotate(q - centre, -base);
		float arc_r = max(lift, 1e-3);
		float d = abs(length(local * vec2(1.0, 1.6)) - arc_r);
		float a = (1.0 - smoothstep(0.006, 0.02, d)) * step(0.0, local.x) * sin(life * 3.14159);
		fire_add(acc, mix(blaze_color.rgb, core_color.rgb, 0.4) * intensity, a * flares);
	}
}

void speed_streaks(inout vec4 acc, vec2 q, float time) {
	for (int i = 0; i < 12; i++) {
		float h = effect_hash(vec2(float(i), 4.1));
		float y = (h - 0.5) * RING * 2.0;
		float run = fract(time * (1.8 + h) + h * 3.0);
		float x0 = -RING * 0.5 - run * 1.4 * trail;
		float len = (0.2 + 0.35 * h) * trail;
		float d = abs(q.y - y) + max(max(q.x - x0, x0 - len - q.x), 0.0);
		float a = (1.0 - smoothstep(0.002, 0.008, d)) * (1.0 - run) * smoothstep(0.0, 0.1, run);
		fire_add(acc, mix(flame_color.rgb, blaze_color.rgb, h) * intensity, a * streaks);
	}
}

void fragment() {
	vec2 q = (UV - 0.5) * card_size * vec2(1.0, -1.0) + card_centre;
	float r = length(q);
	float time = t;
	float beat = 0.5 + 0.5 * sin(time * pulse_rate * TAU);
	beat = beat * beat * beat;

	// Hot air: the stage ripples upward round and behind the orb.
	float air = (1.0 - smoothstep(RING * 1.4, RING * 3.2, length(q * vec2(0.55, 1.0) + vec2(RING * 0.9, 0.0)))) * heat_haze;
	vec2 wobble = (vec2(effect_noise(q * 7.0 + vec2(time * 2.0, -time * 5.0)), effect_noise(q * 7.0 + vec2(3.1, -time * 6.0))) - 0.5) * 0.045;
	vec4 acc = fire_screen(screen_texture, wobble, clamp(air, 0.0, 1.0), VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);

	smoke_trail(acc, q, time);
	halo(acc, r, beat);
	flame_plume(acc, q, time);

	float tongues = fire_corona(q, RING * 0.92, RING * 0.75 * corona * (1.0 + 0.25 * beat), time);
	fire_lay(acc, fire_ramp(tongues * 1.1), smoothstep(0.05, 0.3, tongues));

	shockwave_rings(acc, r, time);
	flare_arcs(acc, q, time, spin);
	speed_streaks(acc, q, time);

	// Fade everything out before the card's edge, so its rectangle never shows.
	vec2 edge = min(UV, 1.0 - UV) * card_size;
	acc *= smoothstep(0.0, 0.25, min(edge.x, edge.y));

	ALBEDO = acc.rgb;
	ALPHA = clamp(acc.a, 0.0, 1.0);
}
