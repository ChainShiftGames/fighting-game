// Shared look for the ice orb's effects (flight, charge, impact): its palette and the frost shapes they all draw
// with (snowflakes, a six-fold sigil, ice shards, glints, cold mist). Layers stack front over back with `frost_lay`,
// premultiplied, as the other effects do. Look settings are developer settings (ADR-0008); the ranges and groups are
// the VFX Lab's sliders (ADR-0029). The defaults are the look picked in the Lab (Blizzard Comet, tweaked).

#include "res://Assets/Effects/effect_noise.gdshaderinc"

group_uniforms palette;
// Deep ice in the shadows, the glow round it, and the white-hot frost at its brightest.
uniform vec4 deep_color : source_color = vec4(0.04, 0.16, 0.42, 1.0);
uniform vec4 glow_color : source_color = vec4(0.32, 0.82, 1.0, 1.0);
uniform vec4 frost_color : source_color = vec4(0.9, 0.98, 1.0, 1.0);
// The Aether turquoise of the Aristocratic Region (setting.md §8.2), threaded through the sigil and sparks.
uniform vec4 aether_color : source_color = vec4(0.25, 1.0, 0.86, 1.0);
group_uniforms frost;
uniform float intensity : hint_range(0.0, 3.0) = 0.534;
uniform float halo : hint_range(0.0, 2.5) = 0.787;
uniform float sigil : hint_range(0.0, 2.0) = 0.082;
uniform float trail : hint_range(0.0, 2.5) = 0.788;
uniform float flakes : hint_range(0.0, 2.0) = 2.0;
uniform float glints : hint_range(0.0, 2.0) = 1.179;
uniform float shimmer : hint_range(0.0, 2.0) = 1.999;
group_uniforms;

// Driven by the art node: seconds from the part's first frame, and its turn.
uniform float t = 0.0;
uniform float spin = 0.0;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

void frost_lay(inout vec4 acc, vec3 colour, float a) {
	a = clamp(a, 0.0, 1.0);
	acc = vec4(colour * a, a) + acc * (1.0 - a);
}

vec3 frost_glow() {
	return glow_color.rgb * intensity;
}

vec2 frost_rotate(vec2 p, float angle) {
	float c = cos(angle);
	float s = sin(angle);
	return vec2(c * p.x - s * p.y, s * p.x + c * p.y);
}

float frost_segment(vec2 p, vec2 a, vec2 b) {
	vec2 pa = p - a;
	vec2 ba = b - a;
	return length(pa - ba * clamp(dot(pa, ba) / max(dot(ba, ba), 1e-6), 0.0, 1.0));
}

// A six-armed snowflake of `radius`, as coverage: arms with two pairs of side branches.
float frost_snowflake(vec2 p, float radius, float angle) {
	p = frost_rotate(p, angle) / max(radius, 1e-4);
	float sector = 1.0471976;
	float a = mod(atan(p.y, p.x) + sector * 0.5, sector) - sector * 0.5;
	vec2 q = length(p) * vec2(cos(a), abs(sin(a)));
	float d = frost_segment(q, vec2(0.0), vec2(1.0, 0.0));
	d = min(d, frost_segment(q, vec2(0.45, 0.0), vec2(0.7, 0.22)));
	d = min(d, frost_segment(q, vec2(0.7, 0.0), vec2(0.88, 0.14)));
	return 1.0 - smoothstep(0.05, 0.11, d);
}

// A four-pointed glint: a bright dot with long thin rays, as on polished ice.
float frost_glint(vec2 p, float radius) {
	p /= max(radius, 1e-4);
	float rays = max(exp(-abs(p.x) * 18.0) * exp(-abs(p.y) * 1.6), exp(-abs(p.y) * 18.0) * exp(-abs(p.x) * 1.6));
	return clamp(rays + exp(-dot(p, p) * 9.0), 0.0, 1.0);
}

// An ice shard: a long thin diamond pointing along `angle`.
float frost_shard(vec2 p, float angle, float length_, float width) {
	p = frost_rotate(p, -angle);
	float d = abs(p.x) / length_ + abs(p.y) / width;
	return 1.0 - smoothstep(0.85, 1.0, d);
}

// The six-fold frost sigil: two rings, a hexagram between them and runes ticked round the outer ring. `drawn`
// (0 to 1) traces it on round the circle, for the charge.
float frost_sigil(vec2 p, float radius, float angle, float drawn) {
	p = frost_rotate(p, angle) / max(radius, 1e-4);
	float r = length(p);
	float around = fract(atan(p.y, p.x) / TAU + 0.5);
	if (around > drawn) {
		return 0.0;
	}
	float line = 0.022;
	float rings = max(1.0 - smoothstep(line, line * 2.0, abs(r - 1.0)), 1.0 - smoothstep(line * 0.6, line * 1.4, abs(r - 0.82)));
	float hexagram = 0.0;
	for (int i = 0; i < 6; i++) {
		float a0 = float(i) * TAU / 6.0;
		float a1 = a0 + TAU / 3.0;
		float d = frost_segment(p, 0.82 * vec2(cos(a0), sin(a0)), 0.82 * vec2(cos(a1), sin(a1)));
		hexagram = max(hexagram, 1.0 - smoothstep(line * 0.5, line * 1.2, d));
	}
	float ticks = step(0.5, fract(around * 36.0)) * (1.0 - smoothstep(0.0, 0.05, abs(r - 0.91)));
	return max(max(rings, hexagram * 0.8), ticks * 0.7);
}

// Cold mist: soft, slowly churning noise, thickest at the middle of `p`'s unit circle.
float frost_mist(vec2 p, float time) {
	float n = effect_fbm(p * 2.2 + vec2(time * 0.6, -time * 0.25));
	return smoothstep(0.42, 0.75, n) * (1.0 - smoothstep(0.4, 1.0, length(p)));
}

// The screen behind the card seen through a small displacement (metres on the card), faded in by `mask`: cold air
// shimmering. The camera faces the plane square-on, so this is exact.
vec4 frost_screen(vec2 displacement, float mask, vec3 vertex, mat4 model, mat4 view, mat4 projection, vec2 screen_uv) {
	vec3 world_shift = (model * vec4(displacement * mask, 0.0, 0.0)).xyz;
	vec4 here = projection * vec4(vertex, 1.0);
	vec4 there = projection * vec4(vertex + (view * vec4(world_shift, 0.0)).xyz, 1.0);
	vec2 uv = screen_uv + (there.xy / there.w - here.xy / here.w) * 0.5;
	return vec4(textureLod(screen_texture, uv, 0.0).rgb * mask, mask);
}
