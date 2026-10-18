// Round 1, "Rift": not a ball but a tear in the world flying forward. Inside the slit the stage shows in negative
// and pulled towards the seam; its lips crackle with pale lightning, and black flakes peel off the back edge.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled;

#include "res://Assets/Effects/dark_orb.gdshaderinc"

uniform vec2 card_size = vec2(2.0, 1.4);
uniform vec2 card_centre = vec2(-0.25, 0.0);
uniform float size = 1.0;

group_uniforms rift;
uniform float rift_length : hint_range(0.1, 1.2) = 0.46;
uniform float rift_open : hint_range(0.02, 0.6) = 0.17;
uniform float jaggedness : hint_range(0.0, 2.5) = 1.0;
uniform float negative : hint_range(0.0, 1.0) = 1.0;
uniform float lightning : hint_range(0.0, 2.0) = 1.0;
uniform float flakes : hint_range(0.0, 2.0) = 1.0;
uniform float tilt : hint_range(-0.8, 0.8) = 0.0;
group_uniforms;

// The slit's half-opening at x along it: 0 past the ends, widest just behind the front, breathing with the heartbeat.
float opening(float x, float time) {
	float u = clamp(x / rift_length, -1.0, 1.0);
	float shape = sqrt(max(0.0, 1.0 - u * u)) * (1.0 - 0.35 * smoothstep(0.0, 1.0, -u));
	return rift_open * shape * (0.85 + 0.15 * throb(time) * pulse);
}

void fragment() {
	vec2 local = (UV - 0.5) * card_size * vec2(1.0, -1.0) + card_centre;
	vec2 q = local / max(size, 0.01);
	float wobble = tilt + sin(t * 2.1) * 0.08;
	q = vec2(cos(wobble) * q.x + sin(wobble) * q.y, -sin(wobble) * q.x + cos(wobble) * q.y);
	float edge = 1.0 - smoothstep(0.55, 0.85, length(local - card_centre * 0.5));

	float jag = (effect_fbm(vec2(q.x * 14.0, floor(t * 20.0) * 0.37)) - 0.5) * 0.08 * jaggedness;
	float half_open = opening(q.x, t) + jag * step(0.01, opening(q.x, t));
	float d = abs(q.y) - half_open;
	float inside = 1.0 - smoothstep(-0.006, 0.0, d);

	// Outside, the stage is pulled towards the seam; inside, it is pulled harder and turned negative.
	float pull = bend * 0.12 * exp(-max(d, 0.0) * 9.0) * step(abs(q.x), rift_length * 1.4);
	vec2 towards = vec2(0.0, -sign(q.y) * pull);
	vec4 acc = bent_screen(towards * size, edge, VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);
	vec4 deep = bent_screen(vec2(-q.x * 0.35, -q.y * 0.8) * size, 1.0, VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);
	vec3 flipped = mix(deep.rgb, vec3(1.0) - deep.rgb, negative);
	// Deeper towards the middle of the slit, where a thin pale seam burns.
	float depth = 1.0 - abs(q.y) / max(half_open, 1e-3);
	flipped = mix(flipped, core_color.rgb, smoothstep(0.0, 0.8, depth) * 0.85);
	flipped = mix(flipped, accent_color.rgb, (1.0 - smoothstep(0.0, 0.01, abs(q.y))) * smoothstep(rift_length, rift_length * 0.3, abs(q.x)));

	// A shadow round the tear, then the tear itself.
	lay(acc, core_color.rgb, exp(-max(d, 0.0) * 22.0) * 0.55 * step(0.0, d) * smoothstep(rift_length * 1.3, rift_length * 0.6, abs(q.x)));
	lay(acc, flipped, inside);
	lay(acc, mix(glow(), accent_color.rgb, 0.7), (1.0 - smoothstep(0.002, 0.008, abs(d))) * step(0.0005, half_open));

	// Lightning along the lips, redrawn every three frames.
	float bolt = floor(t * 20.0);
	for (int side = 0; side < 2; side++) {
		float s = side == 0 ? 1.0 : -1.0;
		float arc = half_open + 0.012 + 0.05 * effect_noise(vec2(q.x * 18.0, bolt + float(side) * 7.0)) * lightning;
		float on = step(0.55, effect_noise(vec2(q.x * 3.0 + bolt * 0.9, bolt * 0.4 + float(side) * 3.0)));
		lay(acc, accent_color.rgb, (1.0 - smoothstep(0.002, 0.006, abs(q.y * s - arc))) * on * step(0.0, q.y * s) * lightning * step(0.01, half_open));
	}

	// Flakes of black peeling off the back and tumbling away.
	for (int i = 0; i < 24; i++) {
		if (float(i) >= 18.0 * flakes) { break; }
		float h = effect_hash(vec2(float(i), 3.1));
		float h2 = effect_hash(vec2(float(i), 8.3));
		float u = fract(t * (0.7 + 0.6 * h2) + h);
		float from_x = mix(-rift_length, rift_length * 0.4, h);
		vec2 at = vec2(from_x - u * 0.7 * max(trail, 0.3) * 2.0, sign(h2 - 0.5) * (opening(from_x, t) + u * 0.18));
		vec2 p = q - at;
		float a = h * TAU + u * 6.0;
		p = vec2(cos(a) * p.x + sin(a) * p.y, -sin(a) * p.x + cos(a) * p.y);
		float sz = 0.028 * (1.0 - u) + 0.004;
		float shape = abs(p.x) / sz + abs(p.y) / (sz * 0.45);
		lay(acc, accent_color.rgb, (1.0 - smoothstep(1.0, 1.3, shape)) * (1.0 - u) * 0.8);
		lay(acc, core_color.rgb, (1.0 - smoothstep(0.7, 0.95, shape)) * (1.0 - u));
	}
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
