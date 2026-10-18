// The dark fireball's impact, by kind: 0 hit, 1 block, 2 clash, 3 fizzle (lifetime ran out). A hit flashes white
// then negative, bursts into a jagged black sphere that shatters into shards, fires anime impact lines and embers,
// and sends a shockwave that ripples the screen; a block is smaller and deflected back along a pale barrier arc; a
// clash is a smaller hit; a fizzle collapses into itself. `age` is in logic frames since the event.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled;

#include "res://Assets/Effects/dark_orb.gdshaderinc"
#include "res://Assets/Effects/toon_effect.gdshaderinc"

// Toon look (developer setting, off until picked): flat bands, ink, held drawings and no bending of the stage.
uniform bool toon_look = false;

uniform vec2 card_size = vec2(2.6, 2.6);
uniform float age = 0.0;
uniform int kind = 0;
uniform float lifetime = 30.0;

float diamond(vec2 p, vec2 centre, float angle, float length_, float width) {
	vec2 d = p - centre;
	vec2 u = vec2(cos(angle) * d.x + sin(angle) * d.y, -sin(angle) * d.x + cos(angle) * d.y);
	return abs(u.x) / length_ + abs(u.y) / width;
}

void shockwave_lens(inout vec4 acc, vec2 local, float n, float scale, float edge, vec3 vertex, mat4 model, mat4 view,
		mat4 projection, vec2 screen_uv) {
	float r = length(local);
	float ring_r = (0.12 + 1.0 * sqrt(n)) * scale;
	float ring = exp(-pow((r - ring_r) / (0.07 * scale), 2.0)) * pow(1.0 - n, 2.0);
	vec2 push = r > 1e-4 ? local / r * ring * 0.09 * bend * scale : vec2(0.0);
	acc = bent_screen(-push, edge, vertex, model, view, projection, screen_uv);
}

void burst(inout vec4 acc, vec2 p, float f, float n, float scale, float lean) {
	float r = length(p);
	float angle = atan(p.y, p.x);
	// Frames 0-1: a white flash, then a negative frame (black disc, white edge), anime style.
	if (f < 1.0) {
		lay(acc, vec3(1.0), 1.0 - smoothstep(0.38 * scale, 0.5 * scale, r));
	} else if (f < 2.0) {
		lay(acc, core_color.rgb, 1.0 - smoothstep(0.42 * scale, 0.47 * scale, r));
		lay(acc, vec3(1.0), 1.0 - smoothstep(0.006, 0.018, abs(r - 0.46 * scale)));
	}
	// Impact lines: thin wedges thrown out from the centre over the first frames.
	float line_reach = (0.3 + 0.9 * smoothstep(0.0, 6.0, f)) * scale;
	float line_fade = 1.0 - smoothstep(5.0, 11.0, f);
	for (int i = 0; i < 16; i++) {
		float h = effect_hash(vec2(float(i), 12.7));
		float a = h * TAU + lean * cos(h * TAU) * 0.4;
		float width = 0.012 + 0.02 * effect_hash(vec2(float(i), 3.3));
		float da = abs(mod(angle - a + 3.14159, TAU) - 3.14159);
		float wedge = 1.0 - smoothstep(width * 0.5, width, da * r);
		float along = smoothstep(0.08 * scale, 0.14 * scale, r) * (1.0 - smoothstep(line_reach * 0.7, line_reach, r));
		vec3 colour = mod(float(i), 2.0) < 1.0 ? accent_color.rgb : core_color.rgb;
		lay(acc, colour, wedge * along * line_fade);
	}
	// The black burst: a jagged sphere that swells fast, then thins out and breaks up.
	float swell = 0.42 * scale * (1.0 - exp(-f * 0.55));
	float jag = (effect_fbm(vec2(angle * 3.0, f * 0.4)) - 0.5) * 0.18 * scale;
	float burst_edge = swell + jag;
	float body = (1.0 - smoothstep(burst_edge - 0.02, burst_edge, r)) * step(1.0, f);
	float hollow = smoothstep(0.0, 1.0, n * 1.8) * (1.0 - smoothstep(burst_edge * 0.2, burst_edge * 0.9, r));
	float breakup = smoothstep(0.35, 0.75, n + (effect_fbm(p * 7.0 + f * 0.2) - 0.5) * 0.5);
	float cover = body * (1.0 - hollow * 0.85) * (1.0 - breakup);
	lay(acc, mix(core_color.rgb, glow() * 0.5, smoothstep(0.0, burst_edge, r)), cover);
	lay(acc, accent_color.rgb, (1.0 - smoothstep(0.004, 0.012, abs(r - burst_edge))) * step(1.0, f) * (1.0 - n));
	// Black shards with pale edges, flung out and tumbling.
	for (int i = 0; i < 16; i++) {
		float h = effect_hash(vec2(float(i), 6.1));
		float h2 = effect_hash(vec2(float(i), 9.4));
		float a = h * TAU + lean * cos(h * TAU) * 0.5;
		float travel = (0.25 + 0.75 * h2) * scale * (1.0 - exp(-f * 0.12)) * 1.4;
		vec2 at = travel * vec2(cos(a), sin(a)) + vec2(0.0, -0.15 * n * n);
		float len = (0.05 + 0.06 * h2) * scale * (1.0 - n);
		float shape = diamond(p, at, a + f * (h - 0.5) * 0.6, len, len * 0.35);
		float shown = step(2.0, f) * (1.0 - smoothstep(0.7, 1.0, n));
		lay(acc, accent_color.rgb, (1.0 - smoothstep(1.0, 1.25, shape)) * shown);
		lay(acc, core_color.rgb, (1.0 - smoothstep(0.75, 0.95, shape)) * shown);
	}
	// Embers drifting out and up.
	for (int i = 0; i < 24; i++) {
		if (float(i) >= 8.0 + 12.0 * particles) {
			break;
		}
		float h = effect_hash(vec2(float(i), 2.9));
		float h2 = effect_hash(vec2(float(i), 5.5));
		float a = h * TAU;
		vec2 at = (0.2 + 0.8 * h2) * scale * sqrt(n) * vec2(cos(a), sin(a)) + vec2(0.0, 0.25 * n * scale);
		float radius = 0.014 * (1.0 - n) + 0.002;
		lay(acc, accent_color.rgb, (1.0 - smoothstep(radius * 0.5, radius, length(p - at))) * step(2.0, f) * (1.0 - n));
	}
}

void block_arc(inout vec4 acc, vec2 p, float f, float n) {
	// The barrier the block hit: a pale arc on the defender's side, facing the fireball, that flares and fades.
	vec2 c = p - vec2(0.08, 0.0);
	float r = length(c);
	float angle = atan(c.y, c.x);
	float arc = (1.0 - smoothstep(0.008, 0.02, abs(r - 0.32))) * (1.0 - smoothstep(0.9, 1.3, abs(angle - 3.14159)));
	lay(acc, accent_color.rgb, arc * (1.0 - smoothstep(0.0, 0.8, n)) * step(1.0, f));
}

void fizzle(inout vec4 acc, vec2 p, float f, float n, float time, float turn) {
	// The orb collapses into itself, pulling a last swirl of sparks in, and ends in a small pale pop.
	float shrink = 1.0 - smoothstep(0.0, 0.7, n);
	if (shrink > 0.02) {
		vec2 q = p / shrink;
		orb_halo(acc, q, time, turn, 1.0);
		orb_core(acc, q, time, turn);
	}
	float pop = smoothstep(0.65, 0.75, n) * (1.0 - smoothstep(0.75, 1.0, n));
	float ring_r = 0.04 + 0.25 * smoothstep(0.7, 1.0, n);
	lay(acc, accent_color.rgb, (1.0 - smoothstep(0.004, 0.012, abs(length(p) - ring_r))) * pop);
}

void fragment() {
	vec2 local = (UV - 0.5) * card_size * vec2(1.0, -1.0);
	float frames = toon_look ? toon_held_frames(age) : age;
	float time = toon_look ? toon_held(t) : t;
	float turn = toon_look ? toon_held_with(spin, t) : spin;
	if (toon_look) {
		local += toon_boil_offset(local, frames / TOON_FRAMES_PER_SECOND);
	}
	float edge = 1.0 - smoothstep(0.9, 1.28, length(local));
	float n = clamp(frames / lifetime, 0.0, 1.0);
	float scale = kind == 1 ? 0.65 : (kind == 2 ? 0.8 : (kind == 3 ? 0.5 : 1.0));
	vec4 acc = vec4(0.0);
	if (!toon_look) {
		shockwave_lens(acc, local, n, scale, edge, VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);
	}
	if (kind == 3) {
		fizzle(acc, local, frames, n, time, turn);
	} else {
		// A blocked fireball bursts back towards where it came from (-X); a hit or clash bursts forward.
		float lean = kind == 1 ? -1.0 : 1.0;
		burst(acc, local, frames, n, scale, lean);
		if (kind == 1) {
			block_arc(acc, local, frames, n);
		}
	}
	if (toon_look) {
		acc = toon_finish(acc, core_color.rgb, accent_color.rgb, VIEWPORT_SIZE.y);
	}
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
