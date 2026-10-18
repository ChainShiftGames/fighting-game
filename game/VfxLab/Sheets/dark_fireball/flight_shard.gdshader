// Round 1, "Obsidian Comet": a faceted black crystal instead of a soft orb, hard cel highlights on its faces, a ring
// of small shards orbiting it, and a trail of sharp triangles that flicker out behind. Clean, graphic, readable.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled;

#include "res://Assets/Effects/dark_orb.gdshaderinc"

uniform vec2 card_size = vec2(2.0, 1.6);
uniform vec2 card_centre = vec2(-0.2, 0.0);
uniform float size = 1.0;

group_uniforms crystal;
uniform float facets : hint_range(3.0, 10.0, 1.0) = 6.0;
uniform float crystal_size : hint_range(0.5, 1.8) = 1.0;
uniform float stretch : hint_range(1.0, 2.5) = 1.35;
uniform float shards : hint_range(0.0, 16.0, 1.0) = 7.0;
uniform float orbit_size : hint_range(0.2, 1.2) = 0.48;
uniform float trail_shards : hint_range(0.0, 2.0) = 1.0;
uniform float highlight : hint_range(0.0, 1.0) = 0.8;
group_uniforms;

float diamond(vec2 p, vec2 centre, float angle, float len, float width) {
	vec2 d = p - centre;
	vec2 u = vec2(cos(angle) * d.x + sin(angle) * d.y, -sin(angle) * d.x + cos(angle) * d.y);
	return abs(u.x) / len + abs(u.y) / width;
}

void fragment() {
	vec2 local = (UV - 0.5) * card_size * vec2(1.0, -1.0) + card_centre;
	vec2 q = local / max(size, 0.01);
	float edge = 1.0 - smoothstep(0.55, 0.85, length(local - card_centre * 0.5));
	vec4 acc = bent_screen(orb_lens(q, t, bend * 0.7) * size - local, edge, VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);

	// Triangles left behind: one per two frames, each flickering off as it ages.
	for (int k = 0; k < 14; k++) {
		if (float(k) >= 10.0 * trail_shards) { break; }
		float fk = float(k);
		float born = floor(t * 30.0) - fk;
		float h = effect_hash(vec2(born, 2.7));
		vec2 at = vec2(-0.2 - fk * 0.075 * max(trail, 0.3) * 2.2, (h - 0.5) * 0.22);
		float sz = 0.07 * (1.0 - fk / 14.0);
		float shape = diamond(q, at, h * TAU, sz, sz * 0.5);
		float alive = step(fk / 14.0, effect_hash(vec2(born, 9.9)) * 0.6 + 0.4);
		lay(acc, accent_color.rgb, (1.0 - smoothstep(1.0, 1.15, shape)) * alive * 0.9);
		lay(acc, core_color.rgb, (1.0 - smoothstep(0.75, 0.85, shape)) * alive);
	}

	orb_halo(acc, q, t, spin, 0.6);

	// Orbiting shards: the back half before the crystal, the front half after it.
	for (int pass = 0; pass < 2; pass++) {
		for (int i = 0; i < 16; i++) {
			if (float(i) >= shards) { break; }
			float a = float(i) / max(shards, 1.0) * TAU + spin * 0.8;
			bool front = sin(a) < 0.0;
			if (front != (pass == 1)) { continue; }
			vec2 at = vec2(cos(a) * orbit_size * aura, sin(a) * orbit_size * aura * 0.35);
			float shape = diamond(q, at, a * 2.0 + t * 3.0, 0.04, 0.016);
			lay(acc, accent_color.rgb, (1.0 - smoothstep(1.0, 1.2, shape)) * (front ? 1.0 : 0.6));
			lay(acc, core_color.rgb, (1.0 - smoothstep(0.65, 0.8, shape)) * (front ? 1.0 : 0.6));
		}
		if (pass == 0) {
			// The crystal: a polygon stretched along the line of flight, its faces shaded in hard steps.
			vec2 c = vec2(q.x / stretch, q.y);
			float r = length(c);
			float angle = atan(c.y, c.x) + spin * 0.25;
			float wedge = TAU / facets;
			float facet = floor(angle / wedge);
			float within = angle - facet * wedge - wedge * 0.5;
			float radius = ORB_RADIUS * crystal_size * cos(wedge * 0.5) / cos(within) * (0.95 + 0.05 * throb(t) * pulse);
			float inside = 1.0 - smoothstep(radius - 0.004, radius, r);
			float shade = effect_hash(vec2(facet, 1.0));
			vec3 face = mix(core_color.rgb, glow() * 0.35 + accent_color.rgb * 0.12, step(0.55, shade) * (r / radius));
			face = mix(face, accent_color.rgb, step(0.82, shade) * step(0.55, r / radius) * highlight);
			lay(acc, face, inside);
			float seam = (1.0 - smoothstep(0.0, 0.006, abs(within) * r)) * inside * step(0.3, r / radius);
			lay(acc, accent_color.rgb, seam * 0.6 * highlight);
			lay(acc, accent_color.rgb, (1.0 - smoothstep(0.003, 0.008, abs(r - radius))));
		}
	}
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
