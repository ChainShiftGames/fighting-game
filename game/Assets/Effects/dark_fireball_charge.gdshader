// The dark fireball's startup charge, at the point it will spawn from: dark streams and sparks rush in from far
// out, the world bends harder and harder towards the point, a seed orb grows to the fireball's spawn size, and a
// pale ring snaps shut on the last frames. `progress` runs 0 to 1 over the move's startup.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/dark_orb.gdshaderinc"
#include "res://Assets/Effects/toon_effect.gdshaderinc"

// Toon look (developer setting): flat bands, ink, held drawings and no bending of the stage; off = the soft look.
uniform bool toon_look = true;

uniform vec2 card_size = vec2(2.6, 2.6);
uniform float progress = 0.0;
// The fireball's size on its first frame, so the seed hands over to it without a jump.
uniform float spawn_size = 0.2;

void streams(inout vec4 acc, vec2 q, float time, float strength, float progress_now) {
	float r = length(q);
	float angle = atan(q.y, q.x);
	// Four arms spiralling inwards, winding tighter as the charge builds.
	float phase = fract((4.0 * angle + r * (9.0 + 6.0 * progress_now) + time * 9.0) / TAU);
	float arm = smoothstep(0.0, 0.1, phase) * (1.0 - smoothstep(0.1, 0.5, phase));
	float leading = smoothstep(0.0, 0.04, phase) * (1.0 - smoothstep(0.04, 0.1, phase));
	float reach = mix(1.25, 0.55, progress_now);
	float band = smoothstep(0.06, 0.18, r) * (1.0 - smoothstep(reach * 0.6, reach, r));
	vec3 colour = mix(core_color.rgb, accent_color.rgb, leading * 0.8);
	lay(acc, colour, arm * band * strength * 0.9);
}

void rushing_sparks(inout vec4 acc, vec2 q, float time, float progress_now) {
	for (int i = 0; i < 48; i++) {
		if (float(i) >= 12.0 + 26.0 * particles) {
			break;
		}
		float h = effect_hash(vec2(float(i), 5.3));
		float h2 = effect_hash(vec2(float(i), 8.8));
		float u = fract(time * (1.4 + 1.2 * h2) + h);
		float u0 = max(u - 0.12, 0.0);
		float far = 0.5 + 0.7 * h2;
		float r1 = mix(far, 0.02, u * u);
		float r0 = mix(far, 0.02, u0 * u0);
		float a1 = h * TAU + u * 1.6;
		float a0 = h * TAU + u0 * 1.6;
		float d = seg_dist(q, r0 * vec2(cos(a0), sin(a0)), r1 * vec2(cos(a1), sin(a1)));
		float w = 0.004 + 0.006 * u;
		float shown = smoothstep(0.0, 0.2, u) * smoothstep(0.0, 0.25, progress_now);
		lay(acc, mix(core_color.rgb, accent_color.rgb, u), (1.0 - smoothstep(w * 0.4, w, d)) * shown);
	}
}

void fragment() {
	vec2 local = (UV - 0.5) * card_size * vec2(1.0, -1.0);
	float time = toon_look ? toon_held(t) : t;
	float turn = toon_look ? toon_held_with(spin, t) : spin;
	float charged = toon_look ? toon_held_with(progress, t) : progress;
	if (toon_look) {
		local += toon_boil_offset(local, time);
	}
	float edge = 1.0 - smoothstep(0.8, 1.25, length(local));
	float build = smoothstep(0.0, 1.0, charged);
	float seed = max(spawn_size * build * build, 0.02);
	vec2 q = local / seed;
	float r = length(local);
	vec4 acc = vec4(0.0);
	if (!toon_look) {
		// The pull reaches further out than the fireball's and tightens as the charge builds.
		float near = max(r - 0.05, 0.0);
		float pull = bend * 0.45 * build * exp(-near * 3.5);
		float twist = bend * 1.6 * build * exp(-near * 3.0);
		vec2 turned = vec2(cos(twist) * local.x - sin(twist) * local.y, sin(twist) * local.x + cos(twist) * local.y);
		acc = bent_screen(turned * (1.0 - pull) - local, edge, VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);
	}

	// The surroundings sink into shadow round the point while it charges.
	lay(acc, core_color.rgb, (1.0 - smoothstep(0.1, 1.1, r)) * 0.35 * build * edge);
	streams(acc, local, time, build, charged);
	rushing_sparks(acc, local, time, charged);

	// A pale ring closes in on the point over the last fifth of the startup.
	float close = smoothstep(0.8, 1.0, charged);
	float ring_r = mix(0.6, 0.05, close);
	float ring = 1.0 - smoothstep(0.004, 0.014, abs(r - ring_r));
	lay(acc, accent_color.rgb, ring * step(0.8, charged) * (0.4 + 0.6 * close));

	if (seed > 0.025) {
		orb_halo(acc, q, time, turn, 0.0);
		orb_core(acc, q, time, turn);
	}
	if (toon_look) {
		acc = toon_finish(acc, core_color.rgb, accent_color.rgb, VIEWPORT_SIZE.y);
	}
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
