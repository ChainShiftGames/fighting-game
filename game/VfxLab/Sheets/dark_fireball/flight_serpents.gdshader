// Round 1, "Shadow Serpents": ribbons twist round the line of flight, crossing in front of and behind the core, and
// taper into a long wake. Reads as something alive. Ported from the aura mockups.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/dark_orb.gdshaderinc"

uniform vec2 card_size = vec2(2.6, 1.6);
uniform vec2 card_centre = vec2(-0.5, 0.0);
uniform float size = 1.0;

group_uniforms serpents;
uniform float ribbons : hint_range(1.0, 6.0, 1.0) = 3.0;
uniform float coil : hint_range(2.0, 16.0) = 7.0;
uniform float ribbon_width : hint_range(0.2, 3.0) = 1.0;
uniform float wake : hint_range(0.2, 2.5) = 1.1;
group_uniforms;

// x = body, y = edge (0 centre, 1 rim), z = cos(phase): above 0 the ribbon is in front of the core.
vec3 ribbon(vec2 q, float i, float turn) {
	float s = -q.x;
	float len = wake * max(trail, 0.3) * 3.3;
	if (s < -0.24 || s > len) {
		return vec3(0.0);
	}
	float phase = s * coil + turn + i * TAU / ribbons;
	float head = smoothstep(-0.24, 0.02, s);
	float amp = 0.2 * aura * head * (1.0 + 0.5 * smoothstep(0.0, 0.35, s)) * (1.0 - smoothstep(0.35, len, s));
	float y = amp * sin(phase);
	float slope = amp * coil * cos(phase);
	float d = abs(q.y - y) / sqrt(1.0 + slope * slope);
	float width = (0.045 * ribbon_width * (1.0 - smoothstep(0.0, len, s)) + 0.006) * head;
	if (width < 0.001) {
		return vec3(0.0);
	}
	return vec3(1.0 - smoothstep(width * 0.55, width, d), smoothstep(width * 0.15, width * 0.9, d), cos(phase));
}

void fragment() {
	vec2 local = (UV - 0.5) * card_size * vec2(1.0, -1.0) + card_centre;
	vec2 q = local / max(size, 0.01);
	float edge = 1.0 - smoothstep(0.45, 0.75, length(local - vec2(0.05, 0.0)));
	vec4 acc = bent_screen(orb_lens(q, t, bend) * size - local, edge, VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);
	for (int i = 0; i < 6; i++) {
		if (float(i) >= ribbons) { break; }
		vec3 rb = ribbon(q, float(i), spin);
		if (rb.z < 0.0) { lay(acc, mix(core_color.rgb, glow() * 0.7, rb.y), rb.x * 0.85); }
	}
	orb_halo(acc, q, t, spin, 1.0);
	orb_core(acc, q, t, spin);
	for (int i = 0; i < 6; i++) {
		if (float(i) >= ribbons) { break; }
		vec3 rb = ribbon(q, float(i), spin);
		if (rb.z >= 0.0) {
			vec3 c = mix(core_color.rgb, glow(), rb.y);
			c = mix(c, accent_color.rgb, smoothstep(0.75, 1.0, rb.z) * rb.y * 0.8);
			lay(acc, c, rb.x);
		}
	}
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
