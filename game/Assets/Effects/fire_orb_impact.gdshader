// Where the fire orb ends, by kind: 0 hit, 1 block, 2 clash, 3 fizzle (lifetime ran out). A hit flashes white,
// erupts into a boiling fireball that cools from white through orange to soot, throws a shockwave that bends the
// stage and a ring of fire racing out along the floor plane, with a starburst of impact rays (the art node adds the
// flying embers, rolling flames and hanging smoke). A block splashes the fire against a curved barrier and back;
// a clash is a smaller hit; a fizzle gutters out into smoke. `age` is in logic frames since the event.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/fire.gdshaderinc"

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

group_uniforms burst;
uniform float rays : hint_range(0.0, 2.0) = 1.0;
uniform float shockwave : hint_range(0.0, 2.0) = 1.0;
group_uniforms;
uniform vec2 card_size = vec2(3.2, 3.2);
uniform float age = 0.0;
uniform int kind = 0;
uniform float lifetime = 30.0;
uniform float t = 0.0;
uniform float spin = 0.0;

const float RING = 0.3;

// The boiling fireball: noise-edged, swelling fast then slowly, cooling over its life.
void fireball(inout vec4 acc, vec2 p, float f, float scale) {
	float grow = 1.0 - exp(-f * 0.3);
	float radius = RING * scale * (0.9 + 2.6 * grow);
	float cool = smoothstep(0.0, lifetime * 0.95, f);
	float r = length(p);
	float n = effect_fbm(p / (radius + 1e-3) * 2.2 + vec2(0.0, -f * 0.06));
	float edge = r / radius + (n - 0.5) * 0.7;
	float body = 1.0 - smoothstep(0.55, 1.0, edge);
	float heat = (1.0 - edge * 0.55 + (n - 0.5) * 0.5) * (1.05 - cool);
	// Soot takes over from the outside in as it cools.
	vec3 colour = mix(fire_ramp(heat), soot_color.rgb, smoothstep(0.35, 0.9, cool) * smoothstep(0.3, 0.9, edge));
	fire_lay(acc, colour, body * (1.0 - smoothstep(0.75, 1.0, cool)));
}

void flash(inout vec4 acc, vec2 p, float f, float scale) {
	float r = length(p);
	float fade = 1.0 - smoothstep(0.0, 4.0, f);
	fire_lay(acc, core_color.rgb * intensity * 1.5, (1.0 - smoothstep(RING * 1.4 * scale, RING * 1.7 * scale, r)) * step(f, 1.5));
	fire_add(acc, blaze_color.rgb * intensity, exp(-r * 3.0 / scale) * fade * 1.5);
}

void shock_ring(inout vec4 acc, vec2 p, float f, float scale) {
	float u = clamp(f / 14.0, 0.0, 1.0);
	float radius = RING * scale * (0.8 + 5.0 * (1.0 - (1.0 - u) * (1.0 - u)));
	float r = length(p);
	float ring = 1.0 - smoothstep(0.01, 0.045 * scale, abs(r - radius));
	fire_add(acc, mix(core_color.rgb, flame_color.rgb, u) * intensity, ring * (1.0 - u) * shockwave);
	// A flattened ring of fire racing out along the floor plane, seen edge on.
	vec2 flat_ = vec2(p.x, (p.y + RING * 0.4 * scale) * 4.0);
	float floor_ring = 1.0 - smoothstep(0.02, 0.12, abs(length(flat_) - radius * 1.2));
	float n = effect_fbm(vec2(atan(flat_.y, flat_.x) * 3.0, f * 0.2));
	fire_lay(acc, fire_ramp(0.9 - u * 0.6), floor_ring * (1.0 - u) * n * shockwave);
}

void impact_rays(inout vec4 acc, vec2 p, float f, float scale) {
	float u = clamp(f / 10.0, 0.0, 1.0);
	for (int i = 0; i < 14; i++) {
		float h = effect_hash(vec2(float(i), 5.5));
		float angle = float(i) / 14.0 * TAU + h * 0.4;
		vec2 dir = vec2(cos(angle), sin(angle));
		float along = dot(p, dir);
		float across = abs(dot(p, vec2(-dir.y, dir.x)));
		float start = RING * scale * (0.5 + 3.0 * u);
		float len = RING * scale * (1.0 + 1.5 * h) * (1.0 - u);
		float width = 0.02 * scale * (1.0 - u) * (1.0 - clamp((along - start) / max(len, 1e-3), 0.0, 1.0));
		float a = step(start, along) * step(along, start + len) * (1.0 - smoothstep(width * 0.3, width + 1e-4, across));
		fire_add(acc, mix(core_color.rgb, blaze_color.rgb, h) * intensity * 1.5, a * rays);
	}
}

// A block: fire splashing against a curved barrier in front of the defender and sliding round it.
void barrier_splash(inout vec4 acc, vec2 p, float f) {
	float u = clamp(f / 18.0, 0.0, 1.0);
	vec2 b = p - vec2(RING * 0.9, 0.0);
	float arc = abs(length(b + vec2(RING * 2.2, 0.0)) - RING * 2.2);
	float spread = RING * (0.6 + 2.4 * u);
	float body = (1.0 - smoothstep(0.0, RING * 0.35, arc)) * (1.0 - smoothstep(spread * 0.6, spread, abs(b.y)));
	float n = effect_fbm(vec2(b.y * 5.0, f * 0.25 - arc * 8.0));
	fire_lay(acc, fire_ramp((1.0 - u) * (0.7 + n * 0.5)), body * (0.5 + n) * (1.0 - u));
	float shield = (1.0 - smoothstep(0.004, 0.015, arc)) * (1.0 - smoothstep(RING * 2.0, RING * 2.6, abs(b.y))) * (1.0 - u);
	fire_add(acc, blaze_color.rgb * intensity, shield);
}

// A fizzle: the fire gutters, shrinking and guttering into a last puff of smoke.
void gutter(inout vec4 acc, vec2 p, float f) {
	float u = clamp(f / lifetime, 0.0, 1.0);
	float radius = RING * (1.0 - u * 0.9);
	float tongues = fire_corona(p, radius * 0.6, RING * 0.5 * (1.0 - u), t);
	fire_lay(acc, soot_color.rgb, effect_fbm(p * 5.0 + vec2(0.0, -t)) * (1.0 - smoothstep(0.0, RING * 1.4, length(p - vec2(0.0, u * 0.4)))) * u * 0.8);
	fire_lay(acc, fire_ramp(tongues * (1.0 - u)), smoothstep(0.05, 0.3, tongues));
}

void fragment() {
	vec2 p = (UV - 0.5) * card_size * vec2(1.0, -1.0);
	float f = age;
	float scale = kind == 2 ? 0.7 : 1.0;
	float r = length(p);

	// The blast bends the stage behind it: a ripple running out with the shockwave.
	float u = clamp(f / 14.0, 0.0, 1.0);
	float wave_r = RING * scale * (0.8 + 5.0 * (1.0 - (1.0 - u) * (1.0 - u)));
	float ripple = exp(-pow((r - wave_r) / 0.12, 2.0)) * (1.0 - u) * heat_haze * (kind == 3 ? 0.0 : 1.0);
	vec2 push = r > 1e-4 ? p / r * 0.06 * ripple : vec2(0.0);
	vec4 acc = fire_screen(screen_texture, push, clamp(ripple, 0.0, 1.0), VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);

	if (kind == 3) {
		gutter(acc, p, f);
	} else if (kind == 1) {
		shock_ring(acc, p, f, 0.6);
		barrier_splash(acc, p, f);
		flash(acc, p, f, 0.6);
	} else {
		shock_ring(acc, p, f, scale);
		fireball(acc, p, f, scale);
		impact_rays(acc, p, f, scale);
		flash(acc, p, f, scale);
	}

	vec2 edge = min(UV, 1.0 - UV) * card_size;
	acc *= smoothstep(0.0, 0.25, min(edge.x, edge.y));

	ALBEDO = acc.rgb;
	ALPHA = clamp(acc.a, 0.0, 1.0);
}
