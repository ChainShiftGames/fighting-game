// Round 3, "Flipbook Puff": a cartoon fireball of clumped smoke puffs, redrawn on every held drawing like a hand-drawn
// flipbook: a black heart, violet body, pale rims where the light catches each puff, puffs shrinking off the back.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/dark_orb.gdshaderinc"
#include "res://Assets/Effects/toon_effect.gdshaderinc"

uniform vec2 card_size = vec2(2.2, 1.5);
uniform vec2 card_centre = vec2(-0.35, 0.0);
uniform float size = 1.0;

group_uniforms puff;
uniform float puffs : hint_range(3.0, 12.0, 1.0) = 8.0;
uniform float clump : hint_range(0.0, 0.2) = 0.06;
uniform float rim_light : hint_range(0.0, 0.08) = 0.035;
uniform float tail_length : hint_range(0.2, 2.5) = 1.0;
group_uniforms;

const float EDGE = 0.012;

float fill(float inside) {
	return smoothstep(-EDGE, EDGE, inside);
}

float smooth_union(float a, float b, float k) {
	float h = clamp(0.5 + 0.5 * (b - a) / max(k, 1e-4), 0.0, 1.0);
	return mix(b, a, h) - k * h * (1.0 - h);
}

// Distance to the puff cluster (negative inside). Each held drawing places the puffs anew.
float cluster(vec2 q, float drawing, float radius) {
	float d = length(q) - radius;
	float len = 1.2 * tail_length * max(trail, 0.3);
	for (int i = 0; i < 12; i++) {
		if (float(i) >= puffs) {
			break;
		}
		float fi = float(i);
		float along = (fi + effect_hash(vec2(fi, drawing))) / puffs;
		vec2 centre = vec2(-along * len + 0.04, (effect_hash(vec2(fi, drawing + 3.1)) - 0.5) * radius * 1.6 * (1.0 - along * 0.5));
		float puff_radius = radius * (0.75 - 0.55 * along) * (0.8 + 0.4 * effect_hash(vec2(fi, drawing + 7.7)));
		d = smooth_union(d, length(q - centre) - puff_radius, clump);
	}
	return d;
}

void fragment() {
	vec2 local = (UV - 0.5) * card_size * vec2(1.0, -1.0) + card_centre;
	float time = toon_held(t);
	float drawing = floor(time * TOON_FRAMES_PER_SECOND / toon_hold_frames + 0.5);
	vec2 q = local / max(size, 0.01);
	float radius = ORB_RADIUS * 1.1 * aura;
	vec4 acc = vec4(0.0);

	float d = cluster(q, drawing, radius);
	lay(acc, toon_mid_color.rgb, fill(-d));
	// Pale rim: inside the cluster, but outside it when shifted a little towards the light (up and ahead).
	float shifted = cluster(q + normalize(vec2(0.6, 0.8)) * rim_light, drawing, radius);
	lay(acc, accent_color.rgb, fill(min(-d, shifted)));
	// The black heart, its edge wobbling between drawings.
	float wobble = (effect_hash(vec2(floor(atan(q.y, q.x) * 3.0), drawing)) - 0.5) * 0.02;
	lay(acc, core_color.rgb, fill(radius * 0.72 + wobble - length(q)));

	acc = toon_finish(acc, core_color.rgb, accent_color.rgb, VIEWPORT_SIZE.y);
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
