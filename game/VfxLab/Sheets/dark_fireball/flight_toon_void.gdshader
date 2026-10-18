// Round 3, "Graphic Void": the most reduced take: a flat black disc, a thick pale crescent, solid ribbons orbiting
// it and hard speed lines behind; on 3s, like a held anime key frame.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled;

#include "res://Assets/Effects/dark_orb.gdshaderinc"
#include "res://Assets/Effects/toon_effect.gdshaderinc"

uniform vec2 card_size = vec2(2.4, 1.3);
uniform vec2 card_centre = vec2(-0.5, 0.0);
uniform float size = 1.0;

group_uniforms graphic;
uniform float ribbons : hint_range(0.0, 4.0, 1.0) = 3.0;
uniform float ribbon_width : hint_range(0.01, 0.08) = 0.03;
uniform float speed_lines : hint_range(0.0, 8.0, 1.0) = 5.0;
uniform float crescent : hint_range(0.0, 0.8) = 0.4;
group_uniforms;

const float EDGE = 0.012;

float fill(float inside) {
	return smoothstep(-EDGE, EDGE, inside);
}

void lines_behind(inout vec4 acc, vec2 q, float radius, float drawing) {
	for (int i = 0; i < 8; i++) {
		if (float(i) >= speed_lines) {
			break;
		}
		float fi = float(i);
		float y = (fi / max(speed_lines - 1.0, 1.0) - 0.5) * radius * 2.2;
		float start = -radius * (0.6 + 0.6 * effect_hash(vec2(fi, drawing)));
		float len = radius * (2.0 + 4.0 * effect_hash(vec2(fi, drawing + 5.0))) * max(trail, 0.3) * 2.0;
		float half_width = 0.012 * (1.0 - abs(y) / (radius * 1.6));
		float inside = min(half_width - abs(q.y - y), min(start - q.x, q.x - (start - len)));
		lay(acc, mod(fi, 2.0) < 1.0 ? toon_mid_color.rgb : accent_color.rgb, fill(inside));
	}
}

void orbit_ribbons(inout vec4 acc, vec2 q, float radius, float turn) {
	for (int i = 0; i < 4; i++) {
		if (float(i) >= ribbons) {
			break;
		}
		float fi = float(i);
		// Tilted orbits seen edge-on: ellipses round the disc, each turned a little more.
		float tilt = fi * 3.14159 / max(ribbons, 1.0) + turn * 0.2;
		vec2 p = vec2(cos(tilt) * q.x + sin(tilt) * q.y, -sin(tilt) * q.x + cos(tilt) * q.y);
		float ellipse = length(p / vec2(radius * 1.55, radius * 0.45)) - 1.0;
		float inside = ribbon_width - abs(ellipse) * radius * 3.0;
		// Only the front half of each orbit is drawn over the disc.
		float front = step(0.0, p.y) + step(radius * 1.02, length(q));
		lay(acc, accent_color.rgb, fill(inside) * min(front, 1.0));
	}
}

void fragment() {
	vec2 local = (UV - 0.5) * card_size * vec2(1.0, -1.0) + card_centre;
	float time = toon_held(t);
	float turn = toon_held_with(spin, t);
	float drawing = floor(time * TOON_FRAMES_PER_SECOND / toon_hold_frames + 0.5);
	vec2 q = local / max(size, 0.01);
	q += toon_boil_offset(q, time);
	float radius = ORB_RADIUS * 1.1 * aura;
	float r = length(q);
	vec4 acc = vec4(0.0);

	lines_behind(acc, q, radius, drawing);
	lay(acc, core_color.rgb, fill(radius - r));
	vec2 light = normalize(vec2(0.6, 0.8));
	lay(acc, accent_color.rgb, fill(min(radius - r, length(q + light * radius * crescent) - radius * 1.02)));
	orbit_ribbons(acc, q, radius, turn);

	acc = toon_finish(acc, core_color.rgb, accent_color.rgb, VIEWPORT_SIZE.y);
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
