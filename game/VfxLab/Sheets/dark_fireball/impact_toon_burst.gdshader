// Round 3 impact, by kind (0 hit, 1 block, 2 clash, 3 fizzle): a white flash and a negative frame, then a flat
// spiky starburst, black with a pale edge, that hollows out and breaks into tumbling chunks while impact lines
// shoot out; inked and held like a drawn effect. A block is smaller, thrown back, with a pale barrier arc.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/dark_orb.gdshaderinc"
#include "res://Assets/Effects/toon_effect.gdshaderinc"

uniform vec2 card_size = vec2(2.6, 2.6);
uniform float age = 0.0;
uniform int kind = 0;
uniform float lifetime = 30.0;

group_uniforms burst;
uniform float spikes : hint_range(4.0, 16.0, 1.0) = 9.0;
uniform float spike_depth : hint_range(0.0, 0.8) = 0.45;
uniform float chunks : hint_range(0.0, 12.0, 1.0) = 8.0;
uniform float impact_lines : hint_range(0.0, 16.0, 1.0) = 10.0;
group_uniforms;

const float EDGE = 0.012;

float fill(float inside) {
	return smoothstep(-EDGE, EDGE, inside);
}

float star_radius(float angle, float base, float drawing) {
	float tri = abs(fract(angle * spikes / TAU + effect_hash(vec2(drawing, 2.0)) * 0.2) - 0.5) * 2.0;
	return base * (1.0 - spike_depth + spike_depth * (1.0 - tri) * 1.6);
}

void starburst(inout vec4 acc, vec2 p, float f, float n, float scale, float drawing) {
	float r = length(p);
	float angle = atan(p.y, p.x);
	if (f < 1.0) {
		lay(acc, vec3(1.0), fill(0.45 * scale - r));
		return;
	}
	if (f < 2.0) {
		lay(acc, core_color.rgb, fill(0.47 * scale - r));
		lay(acc, vec3(1.0), fill(0.02 - abs(r - 0.47 * scale)));
		return;
	}
	float swell = 0.5 * scale * (1.0 - exp(-f * 0.5));
	float outer = star_radius(angle, swell, drawing);
	float hollow = swell * smoothstep(0.25, 0.75, n);
	lay(acc, accent_color.rgb, fill(min(outer - r, r - hollow)));
	lay(acc, core_color.rgb, fill(min(outer * 0.82 - r, r - hollow * 1.1)));
	lay(acc, toon_mid_color.rgb, fill(min(outer * 0.45 - r, r - hollow)) * (1.0 - step(0.4, n)));
}

void impact_lines_out(inout vec4 acc, vec2 p, float f, float scale, float lean) {
	float r = length(p);
	float angle = atan(p.y, p.x);
	float reach = (0.35 + 0.95 * smoothstep(0.0, 6.0, f)) * scale;
	float shown = 1.0 - step(10.0, f);
	for (int i = 0; i < 16; i++) {
		if (float(i) >= impact_lines) {
			break;
		}
		float h = effect_hash(vec2(float(i), 12.7));
		float a = h * TAU + lean * cos(h * TAU) * 0.4;
		float da = abs(mod(angle - a + 3.14159, TAU) - 3.14159) * r;
		float half_width = 0.025 * (1.0 - r / reach);
		float inside = min(half_width - da, min(r - 0.15 * scale, reach - r));
		lay(acc, mod(float(i), 2.0) < 1.0 ? accent_color.rgb : core_color.rgb, fill(inside) * shown);
	}
}

void flying_chunks(inout vec4 acc, vec2 p, float f, float n, float scale, float lean) {
	for (int i = 0; i < 12; i++) {
		if (float(i) >= chunks) {
			break;
		}
		float h = effect_hash(vec2(float(i), 6.1));
		float h2 = effect_hash(vec2(float(i), 9.4));
		float a = h * TAU + lean * cos(h * TAU) * 0.5;
		vec2 at = (0.3 + 0.8 * h2) * scale * (1.0 - exp(-f * 0.1)) * 1.5 * vec2(cos(a), sin(a)) - vec2(0.0, 0.2 * n * n);
		float spinning = a + f * (h - 0.5) * 0.5;
		vec2 d = p - at;
		vec2 u = vec2(cos(spinning) * d.x + sin(spinning) * d.y, -sin(spinning) * d.x + cos(spinning) * d.y);
		float size_ = (0.06 + 0.05 * h2) * scale * (1.0 - n);
		// A triangle chunk: inside all three edges.
		float inside = min(min(size_ * 0.5 - u.y, u.y + u.x * 0.9 + size_ * 0.3), u.y - u.x * 0.9 + size_ * 0.3);
		float shown = step(4.0, f) * (1.0 - step(0.9, n));
		lay(acc, core_color.rgb, fill(inside) * shown);
	}
}

void fragment() {
	vec2 local = (UV - 0.5) * card_size * vec2(1.0, -1.0);
	float f = toon_held_frames(age);
	float drawing = floor(f / toon_hold_frames);
	local += toon_boil_offset(local, f / TOON_FRAMES_PER_SECOND);
	float n = clamp(f / lifetime, 0.0, 1.0);
	float scale = kind == 1 ? 0.65 : (kind == 2 ? 0.8 : (kind == 3 ? 0.5 : 1.0));
	vec4 acc = vec4(0.0);
	if (kind == 3) {
		// Fizzle: the ball shrinks to nothing, then a small pale ring pops.
		float shrink = ORB_RADIUS * aura * (1.0 - smoothstep(0.0, 0.7, n));
		lay(acc, core_color.rgb, fill(shrink - length(local)));
		float pop = step(0.65, n) * (1.0 - step(0.95, n));
		lay(acc, accent_color.rgb, fill(0.015 - abs(length(local) - (0.05 + 0.25 * smoothstep(0.65, 1.0, n)))) * pop);
	} else {
		// A blocked fireball bursts back the way it came (-X); a hit or clash bursts forward.
		float lean = kind == 1 ? -1.0 : 1.0;
		impact_lines_out(acc, local, f, scale, lean);
		flying_chunks(acc, local, f, n, scale, lean);
		starburst(acc, local, f, n, scale, drawing);
		if (kind == 1) {
			float arc = fill(0.02 - abs(length(local - vec2(0.08, 0.0)) - 0.34)) *
				(1.0 - smoothstep(0.9, 1.3, abs(atan(local.y, local.x - 0.08) - 3.14159)));
			lay(acc, accent_color.rgb, arc * (1.0 - step(0.7, n)) * step(2.0, f));
		}
	}
	acc = toon_finish(acc, core_color.rgb, accent_color.rgb, VIEWPORT_SIZE.y);
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
