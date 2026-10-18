// Round 3, "Ink Comet": the void as a flat black ball with a pale crescent where the light catches it, a swirl of
// violet inside, flame tongues streaming back and two swooshes wrapping round it; inked and held on 2s, like a
// drawn Xrd effect.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/dark_orb.gdshaderinc"
#include "res://Assets/Effects/toon_effect.gdshaderinc"

uniform vec2 card_size = vec2(2.4, 1.4);
uniform vec2 card_centre = vec2(-0.45, 0.0);
uniform float size = 1.0;

group_uniforms comet;
uniform float tail_length : hint_range(0.2, 2.5) = 1.0;
uniform float tongues : hint_range(1.0, 6.0, 1.0) = 3.0;
uniform float swooshes : hint_range(0.0, 3.0, 1.0) = 2.0;
uniform float rim_width : hint_range(0.0, 0.6) = 0.3;
group_uniforms;

// Shapes get a slightly soft edge so the toon finish can find the silhouette and ink it.
const float EDGE = 0.012;

float fill(float inside) {
	return smoothstep(-EDGE, EDGE, inside);
}

void flame_tongues(inout vec4 acc, vec2 q, float radius, float time, float drawing) {
	float len = 1.45 * tail_length * max(trail, 0.3);
	float s = clamp(-q.x / len, 0.0, 1.0);
	for (int i = 0; i < 6; i++) {
		if (float(i) >= tongues) {
			break;
		}
		float fi = float(i);
		float lane = tongues > 1.0 ? fi / (tongues - 1.0) - 0.5 : 0.0;
		float centre_y = lane * radius * 1.3 * (1.0 - s * 0.4) + sin(q.x * 7.0 + time * 9.0 + fi * 2.1) * 0.05 * s;
		float half_width = radius * 0.6 * pow(1.0 - s, 1.2) * (1.0 - abs(lane) * 0.6);
		float reach = len * (0.65 + 0.35 * effect_hash(vec2(fi, drawing)));
		float inside = min(min(half_width - abs(q.y - centre_y), reach + q.x), -q.x);
		lay(acc, mod(fi, 2.0) < 1.0 ? toon_mid_color.rgb : core_color.rgb, fill(inside));
	}
}

void swoosh_arcs(inout vec4 acc, vec2 q, float radius, float turn) {
	float r = length(q);
	float angle = atan(q.y, q.x);
	for (int i = 0; i < 3; i++) {
		if (float(i) >= swooshes) {
			break;
		}
		float fi = float(i);
		float start = turn * 0.6 + fi * TAU / max(swooshes, 1.0);
		float along = fract((angle - start) / TAU);
		float span = 0.3;
		float taper = along < span ? sin(along / span * 3.14159) : 0.0;
		float ring = radius * (1.3 + 0.12 * fi);
		float inside = 0.035 * taper - abs(r - ring);
		lay(acc, mod(fi, 2.0) < 1.0 ? accent_color.rgb : toon_mid_color.rgb, fill(inside) * step(0.001, taper));
	}
}

void fragment() {
	vec2 local = (UV - 0.5) * card_size * vec2(1.0, -1.0) + card_centre;
	float time = toon_held(t);
	float turn = toon_held_with(spin, t);
	float drawing = floor(time * TOON_FRAMES_PER_SECOND / toon_hold_frames + 0.5);
	vec2 q = local / max(size, 0.01);
	q += toon_boil_offset(q, time) * 2.0;
	float radius = ORB_RADIUS * 1.15 * aura;
	float r = length(q);
	vec4 acc = vec4(0.0);

	flame_tongues(acc, q, radius, time, drawing);
	// The ball: black, a violet swirl inside, and a pale crescent on the side the light comes from (up and ahead).
	lay(acc, core_color.rgb, fill(radius - r));
	float swirl = step(0.55, fract((2.0 * atan(q.y, q.x) + r * 14.0 - turn) / TAU));
	lay(acc, toon_mid_color.rgb, fill(radius * 0.7 - r) * swirl);
	vec2 light = normalize(vec2(0.6, 0.8));
	lay(acc, accent_color.rgb, fill(min(radius - r, length(q + light * radius * rim_width) - radius)));
	swoosh_arcs(acc, q, radius, turn);

	acc = toon_finish(acc, core_color.rgb, accent_color.rgb, VIEWPORT_SIZE.y);
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
