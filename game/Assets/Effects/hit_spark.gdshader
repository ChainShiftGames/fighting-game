// The standard hit spark, after Guilty Gear Strive. Two styles: a star-shaped burst at the contact point (light hits),
// or a tear: a long tapered streak along the attack's direction, through and behind the defender, with a smaller
// burst at the contact (heavy hits and specials). A block instead shows a blue guard: an arc bowed toward the attacker
// over a small burst, with twinkles thrown back. Hits throw impact lines and debris streaks along the attack, and
// every spark's details vary with its seed. The first frame is the biggest and brightest; then it thins, hollows out
// and rips apart. Card space: metres, +X the way the attack travels (HitSparkArt turns the card), y up.
// Driven by `age` in logic frames since the hit, never TIME, so it follows rollback and hitstop.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, depth_test_disabled, cull_disabled, fog_disabled;

#include "res://Assets/Effects/effect_noise.gdshaderinc"

// Look (developer settings, presentation). The ranges and groups are the VFX Lab's sliders (ADR-0029).
group_uniforms hit_palette;
uniform vec4 hit_core : source_color = vec4(1.0, 1.0, 0.93, 1.0);
uniform vec4 hit_body : source_color = vec4(1.0, 0.8, 0.22, 1.0);
uniform vec4 hit_rim : source_color = vec4(1.0, 0.38, 0.06, 1.0);
group_uniforms counter_palette;
uniform vec4 counter_core : source_color = vec4(1.0, 0.96, 0.86, 1.0);
uniform vec4 counter_body : source_color = vec4(1.0, 0.42, 0.12, 1.0);
uniform vec4 counter_rim : source_color = vec4(0.86, 0.04, 0.06, 1.0);
group_uniforms block_palette;
uniform vec4 block_core : source_color = vec4(0.95, 0.98, 1.0, 1.0);
uniform vec4 block_body : source_color = vec4(0.5, 0.82, 1.0, 1.0);
uniform vec4 block_rim : source_color = vec4(0.12, 0.36, 1.0, 1.0);
group_uniforms clash_palette;
uniform vec4 clash_core : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 clash_body : source_color = vec4(0.88, 0.88, 0.96, 1.0);
uniform vec4 clash_rim : source_color = vec4(0.55, 0.55, 0.7, 1.0);
group_uniforms shape;
// Burst radius in metres at weight 0; heavier hits grow it.
uniform float burst_radius : hint_range(0.2, 1.0) = 0.5;
// How far the tear reaches past the contact point, in metres at weight 0.
uniform float tear_reach : hint_range(0.5, 3.0) = 1.7;
// The tear's half-width at the contact point, in metres.
uniform float tear_width : hint_range(0.02, 0.3) = 0.1;
// Multiplies the number of debris streaks and impact lines.
uniform float debris : hint_range(0.0, 2.0) = 1.0;
uniform float lines : hint_range(0.0, 2.0) = 1.0;
// Blocks: how far the guard arc reaches round from the attacker's side (radians each way), and how many twinkles.
uniform float guard_span : hint_range(0.3, 1.6) = 1.1;
uniform float guard_twinkles : hint_range(0.0, 2.0) = 1.0;
// Logic frames each drawing is held for after the first two (which always show): 1 smooth, 2-3 hand-drawn.
uniform float hold_frames : hint_range(1.0, 3.0, 1.0) = 2.0;
group_uniforms;

// Set per spark by HitSparkArt; the VFX Lab drives age, kind and the card itself.
uniform vec2 card_size = vec2(5.2, 2.8);
uniform vec2 card_centre = vec2(1.2, 0.0);
uniform float age = 0.0;
// HitSparkTone: 0 hit, 1 block, 2 clash, 3 counter.
uniform int kind = 0;
// HitSparkStyle: 0 burst, 1 tear.
uniform int style : hint_range(0, 1) = 0;
// The hit's strength: 0 the lightest, 1 the heaviest, up to 1.3 on counter hit.
uniform float weight : hint_range(0.0, 1.3) = 0.5;
uniform float seed : hint_range(0.0, 1.0) = 0.0;
// World down in card space, for debris that falls.
uniform vec2 down = vec2(0.0, -1.0);

// Premultiplied layer stack: each call puts a layer of colour `c` and coverage `a` on top.
void lay(inout vec4 acc, vec3 c, float a) {
	a = clamp(a, 0.0, 1.0);
	acc = vec4(c * a, a) + acc * (1.0 - a);
}

// A random 0-1 value per item `i` and purpose `salt`, different for every spark.
float pick(float i, float salt) {
	return effect_hash(vec2(i + seed * 61.7, salt + seed * 13.3));
}

// Coverage of a shape from a distance that is negative inside: hard edged, smoothed over one pixel.
float fill(float d) {
	float w = max(fwidth(d), 1e-5);
	return clamp(0.5 - d / w, 0.0, 1.0);
}

float seg_dist(vec2 p, vec2 a, vec2 b) {
	vec2 pa = p - a;
	vec2 ba = b - a;
	return length(pa - ba * clamp(dot(pa, ba) / max(dot(ba, ba), 1e-6), 0.0, 1.0));
}

void palette(out vec3 core, out vec3 body, out vec3 rim) {
	if (kind == 1) {
		core = block_core.rgb; body = block_body.rgb; rim = block_rim.rgb;
	} else if (kind == 2) {
		core = clash_core.rgb; body = clash_body.rgb; rim = clash_rim.rgb;
	} else if (kind == 3) {
		core = counter_core.rgb; body = counter_body.rgb; rim = counter_rim.rgb;
	} else {
		core = hit_core.rgb; body = hit_body.rgb; rim = hit_rim.rgb;
	}
}

// The edge of a star at angle `a`: irregular points between `inner` and `outer`, each `thin` times its own width.
float star_edge(float a, float points, float inner, float outer, float thin) {
	float r = inner;
	for (int i = 0; i < 14; i++) {
		if (float(i) >= points) {
			break;
		}
		float at = (float(i) + 0.6 * pick(float(i), 1.0)) / points * TAU;
		float reach = outer * (0.5 + 0.5 * pick(float(i), 2.0));
		float width = (0.22 + 0.2 * pick(float(i), 3.0)) * thin;
		float da = abs(mod(a - at + PI, TAU) - PI);
		r = max(r, inner + (reach - inner) * pow(max(0.0, 1.0 - da / width), 1.4));
	}
	return r;
}

// The star burst: a white flash on frame 0, then a yellow star with a white heart and an orange rim whose points
// thin out while its centre hollows and the rest rips apart.
void burst(inout vec4 acc, vec2 p, float f, float n, float size, vec3 core, vec3 body, vec3 rim) {
	float r = length(p);
	float a = atan(p.y, p.x);
	float points = 7.0 + floor(5.0 * pick(0.0, 4.0));
	float outer = size * (f < 1.0 ? 1.2 : 1.0 + 0.25 * smoothstep(1.0, 5.0, f));
	float inner = size * 0.4 * (1.0 - smoothstep(0.0, 0.55, n));
	float thin = 1.0 - 0.7 * smoothstep(0.0, 0.75, n);
	float edge = star_edge(a, points, inner, outer, thin);
	float hole = size * 1.1 * smoothstep(0.15, 0.8, n) * (0.6 + 0.4 * effect_noise(vec2(a * 2.5, seed * 9.0)));
	float rip = step(effect_noise(p * 9.0 / size + seed * 17.0), smoothstep(0.45, 1.0, n) * 1.3);
	float keep = (1.0 - fill(r - hole)) * (1.0 - rip);
	if (f < 1.0) {
		lay(acc, body, fill(r - edge * 1.06));
		lay(acc, core, fill(r - edge * 0.94));
		return;
	}
	lay(acc, rim, fill(r - edge) * keep);
	lay(acc, body, fill(r - edge * 0.84) * keep);
	lay(acc, core, fill(r - edge * (0.5 - 0.3 * n)) * keep);
}

// Half-width of the tear at x along it: a lens from `back` behind the contact to `front` ahead of it, thickest
// at the contact and tapering to needle points, with ragged edges. Past its ends it goes on below 0 without a jump,
// so the edge smoothing never sees a step there (a step shows as a hairline across the card).
float tear_half_width(float x, float front, float back, float width, float salt) {
	float k = 1.0 - (x >= 0.0 ? x / front : -x / back);
	float rag = 0.7 + 0.6 * effect_noise(vec2(x * 6.0 + salt, salt));
	return width * rag * (k > 0.0 ? pow(k, 1.6) : k);
}

// The tear: shoots out to full length in a few frames, already long on the first; then it thins to a line, slides
// on and rips into pieces, while thinner slivers peel off beside it.
void tear(inout vec4 acc, vec2 p, float f, float n, float w, vec3 core, vec3 body, vec3 rim) {
	float shoot = 1.0 - exp(-(f + 1.0) * 0.8);
	float front = tear_reach * (0.8 + 0.6 * w) * (0.55 + 0.45 * shoot) * (1.0 + 0.2 * n);
	float back = front * 0.3;
	float width = tear_width * (0.8 + 0.6 * w) * (1.0 - smoothstep(0.1, 0.85, n));
	float rip = step(effect_noise(vec2(p.x * 2.5 + seed * 17.0, 3.0)), smoothstep(0.3, 1.0, n) * 1.25);
	float hw = tear_half_width(p.x, front, back, width, seed * 11.0);
	float d = abs(p.y) - hw;
	float keep = 1.0 - rip;
	if (f < 1.0) {
		lay(acc, body, fill(abs(p.y) - hw * 1.4));
		lay(acc, core, fill(d));
	} else {
		lay(acc, rim, fill(abs(p.y) - hw * 1.4) * keep);
		lay(acc, body, fill(d) * keep);
		lay(acc, core, fill(abs(p.y) - hw * 0.4) * keep);
	}
	// Slivers: thin streaks beside the main one, sliding on along the attack.
	for (int j = 0; j < 3; j++) {
		float side = pick(float(j), 30.0) < 0.5 ? -1.0 : 1.0;
		float off = side * (0.06 + 0.12 * pick(float(j), 31.0)) * (0.8 + 0.6 * w);
		float slide = front * (0.1 + 0.25 * pick(float(j), 32.0)) + 0.05 * f;
		float length_ = front * (0.35 + 0.3 * pick(float(j), 33.0));
		float s = tear_half_width(p.x - slide, length_, length_ * 0.3, width * 0.35, float(j) * 5.0 + seed * 7.0);
		float shown = step(1.0, f) * (1.0 - smoothstep(0.5, 0.9, n));
		lay(acc, body, fill(abs(p.y - off) - s) * shown);
	}
}

// The guard, for blocks: a bright arc bowed toward the attacker, as if the hit met a shield. It flashes full on
// frame 0, then swells, thins from its ends inward and fades.
void guard_arc(inout vec4 acc, vec2 p, float f, float n, float size, vec3 core, vec3 body, vec3 rim) {
	vec2 centre = vec2(size * 0.55, 0.0);
	vec2 q = p - centre;
	float r = length(q);
	float a = abs(atan(q.y, -q.x));
	float radius = size * (0.75 + 0.45 * smoothstep(0.0, 6.0, f));
	float span = guard_span * (1.0 - 0.45 * smoothstep(0.2, 1.0, n));
	float taper = clamp(1.0 - a / span, 0.0, 1.0);
	float thick = size * 0.09 * (f < 1.0 ? 1.6 : 1.0) * (1.0 - 0.6 * n) * sqrt(taper);
	float d = abs(r - radius) - thick;
	float shown = step(a, span);
	lay(acc, rim, fill(d - thick * 0.9) * shown * (1.0 - n));
	lay(acc, body, fill(d) * shown);
	lay(acc, core, fill(abs(r - radius) - thick * 0.4) * shown);
}

// Twinkles, for blocks: small four-point stars thrown back toward the attacker, slowing, each blinking out on its
// own frame.
void twinkles(inout vec4 acc, vec2 p, float f, float size, float count, vec3 core, vec3 body) {
	for (int i = 0; i < 16; i++) {
		if (float(i) >= count) {
			break;
		}
		float at = PI + (pick(float(i), 40.0) - 0.5) * 2.4;
		vec2 dir = vec2(cos(at), sin(at));
		float travel = size * (0.6 + 1.1 * pick(float(i), 41.0)) * (1.0 - exp(-0.35 * f));
		vec2 q = p - dir * (size * 0.35 + travel);
		float life = 6.0 + 8.0 * pick(float(i), 42.0);
		float s = size * (0.1 + 0.1 * pick(float(i), 43.0)) * (1.0 - f / life);
		float star = min(abs(q.x) * 6.0 + abs(q.y), abs(q.y) * 6.0 + abs(q.x)) - s;
		float fade = 1.0 - step(life, f);
		lay(acc, body, fill(star) * fade);
		lay(acc, core, fill(star + s * 0.55) * fade);
	}
}

// Impact lines: thin tapered wedges thrown out from the contact, `spread` radians wide around the attack's way.
void impact_lines(inout vec4 acc, vec2 p, float f, float size, float count, float spread, vec3 colour) {
	float fade = 1.0 - smoothstep(3.0, 9.0, f);
	for (int i = 0; i < 16; i++) {
		if (float(i) >= count) {
			break;
		}
		float at = (pick(float(i), 20.0) - 0.5) * spread;
		vec2 dir = vec2(cos(at), sin(at));
		float start = size * (0.45 + 0.9 * smoothstep(0.0, 7.0, f));
		float end = start + size * (0.8 + 1.4 * pick(float(i), 21.0));
		float width = size * (0.02 + 0.025 * pick(float(i), 22.0));
		float along = dot(p, dir);
		float across = abs(dot(p, vec2(-dir.y, dir.x)));
		float t = clamp((along - start) / (end - start), 0.0, 1.0);
		float d = max(across - width * (1.0 - t), max(start - along, along - end));
		lay(acc, colour, fill(d) * fade);
	}
}

// Debris: fast streaks flung out in a `cone` around the attack's way (a share of them backwards), slowing down,
// stretched by their speed, the slower ones dropping.
void debris_streaks(inout vec4 acc, vec2 p, float f, float size, float count, float cone, float back_share,
		vec3 near, vec3 far) {
	for (int i = 0; i < 40; i++) {
		if (float(i) >= count) {
			break;
		}
		float h = pick(float(i), 13.0);
		float at = (pick(float(i), 10.0) - 0.5) * cone + (pick(float(i), 14.0) < back_share ? PI : 0.0);
		vec2 dir = vec2(cos(at), sin(at));
		float speed = size * (0.1 + 0.22 * pick(float(i), 11.0));
		float slowing = 0.2;
		float travel = speed * (1.0 - exp(-slowing * f)) / slowing;
		float velocity = speed * exp(-slowing * f);
		vec2 head = dir * (size * 0.2 + travel) + down * 0.003 * f * f * (1.0 - h);
		vec2 tail = head - dir * max(velocity * 2.5, size * 0.03);
		float life = 7.0 + 10.0 * h;
		float width = size * (0.008 + 0.012 * h) * (1.0 - f / life);
		float fade = 1.0 - step(life, f);
		lay(acc, pick(float(i), 12.0) < 0.6 ? near : far, fill(seg_dist(p, tail, head) - width) * fade);
	}
}

void fragment() {
	vec2 p = (UV - 0.5) * card_size * vec2(1.0, -1.0) + card_centre;
	// The first two frames always show; after that drawings are held, as on the characters.
	float f = age < 2.0 ? age : 2.0 + floor((age - 2.0) / hold_frames) * hold_frames;
	float w = clamp(weight, 0.0, 1.3);
	float life = mix(12.0, 20.0, w);
	float n = clamp(f / life, 0.0, 1.0);
	vec3 core;
	vec3 body;
	vec3 rim;
	palette(core, body, rim);
	float size = burst_radius * (0.75 + 0.45 * w);
	vec4 acc = vec4(0.0);
	if (n < 1.0) {
		if (kind == 1) {
			impact_lines(acc, p, f, size, (4.0 + 3.0 * w) * lines, TAU, body);
			twinkles(acc, p, f, size, (6.0 + 6.0 * w) * guard_twinkles, core, body);
			burst(acc, p, f, n, size * 0.55, core, body, rim);
			guard_arc(acc, p, f, n, size, core, body, rim);
		} else if (style == 1) {
			debris_streaks(acc, p, f, size, (12.0 + 20.0 * w) * debris, 1.2, 0.2, body, rim);
			impact_lines(acc, p, f, size, (5.0 + 5.0 * w) * lines, 1.8, body);
			tear(acc, p, f, n, w, core, body, rim);
			burst(acc, p, f, n, size * 0.6, core, body, rim);
		} else {
			debris_streaks(acc, p, f, size, (8.0 + 14.0 * w) * debris, TAU * 0.8, 0.0, body, rim);
			impact_lines(acc, p, f, size, (6.0 + 4.0 * w) * lines, TAU, core);
			burst(acc, p, f, n, size, core, body, rim);
		}
	}
	ALBEDO = acc.rgb;
	ALPHA = acc.a;
}
