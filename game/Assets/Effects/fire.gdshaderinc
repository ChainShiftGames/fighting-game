// Shared look for the fire orb's effects (flight, charge, impact, particles): its palette, the heat ramp every flame
// is coloured through, and the shapes they draw with (licking flames, heat haze). Layers stack front over back with
// `fire_lay`, premultiplied; `fire_add` adds light without covering. Look settings are developer settings
// (ADR-0008); the ranges and groups are the VFX Lab's sliders (ADR-0029).

#include "res://Assets/Effects/effect_noise.gdshaderinc"

group_uniforms palette;
// The heat ramp, coolest to hottest: soot, deep red, flame orange, yellow and the white-hot core.
uniform vec4 soot_color : source_color = vec4(0.09, 0.03, 0.03, 1.0);
uniform vec4 ember_color : source_color = vec4(0.75, 0.08, 0.02, 1.0);
uniform vec4 flame_color : source_color = vec4(1.0, 0.42, 0.05, 1.0);
uniform vec4 blaze_color : source_color = vec4(1.0, 0.82, 0.3, 1.0);
uniform vec4 core_color : source_color = vec4(1.0, 0.97, 0.88, 1.0);
group_uniforms fire;
// Overall brightness: above 1 pushes the hottest parts past the stage's glow threshold, so they bloom.
uniform float intensity : hint_range(0.0, 4.0) = 2.2;
uniform float corona : hint_range(0.0, 2.5) = 1.2;
uniform float trail : hint_range(0.0, 2.5) = 1.0;
uniform float heat_haze : hint_range(0.0, 2.0) = 1.2;
group_uniforms;

void fire_lay(inout vec4 acc, vec3 colour, float a) {
	a = clamp(a, 0.0, 1.0);
	acc = vec4(colour * a, a) + acc * (1.0 - a);
}

// Light added on top (additive in premultiplied blending): glow that doesn't hide what's behind it.
void fire_add(inout vec4 acc, vec3 colour, float amount) {
	acc.rgb += colour * max(amount, 0.0);
}

// Colour of fire at `heat` (0 cold soot to 1 white-hot), brightened by `intensity` from orange up.
vec3 fire_ramp(float heat) {
	heat = clamp(heat, 0.0, 1.0);
	vec3 c = mix(soot_color.rgb, ember_color.rgb, smoothstep(0.0, 0.25, heat));
	c = mix(c, flame_color.rgb, smoothstep(0.25, 0.5, heat));
	c = mix(c, blaze_color.rgb, smoothstep(0.5, 0.75, heat));
	c = mix(c, core_color.rgb, smoothstep(0.75, 1.0, heat));
	return c * mix(1.0, intensity, smoothstep(0.35, 0.9, heat));
}

vec2 fire_rotate(vec2 p, float angle) {
	float c = cos(angle);
	float s = sin(angle);
	return vec2(c * p.x - s * p.y, s * p.x + c * p.y);
}

// Turbulent flame noise rising along +Y in `p`'s space: ridged so it shows tongues, not blobs.
float fire_flicker(vec2 p, float time) {
	vec2 warp = vec2(effect_fbm(p * 1.7 + vec2(0.0, -time * 1.3)), effect_fbm(p * 1.7 + vec2(5.2, -time * 1.1)));
	float n = effect_fbm(p * 2.4 + (warp - 0.5) * 1.6 + vec2(0.0, -time * 2.6));
	return n;
}

// Flame tongues licking out round a circle of `radius`: heat 0..1 at `p`, trailing back along -X as it flies.
// `reach` is how far the tongues stretch beyond the circle, in metres.
float fire_corona(vec2 p, float radius, float reach, float time) {
	float r = length(p);
	float angle = atan(p.y, p.x);
	// Stretch the tongues backward: further reach behind the orb than in front of it.
	float back = 0.5 - 0.5 * cos(angle - 3.14159);
	float stretch = reach * mix(0.45, 1.6, back);
	float out_ = (r - radius) / max(stretch, 1e-3);
	vec2 polar = vec2(angle * 2.2, out_ * 2.5 - time * 3.2);
	float n = effect_fbm(polar + vec2(time * 0.4, 0.0));
	float wrap = effect_fbm(vec2((angle + TAU) * 2.2, out_ * 2.5 - time * 3.2) + vec2(time * 0.4, 0.0));
	// Blend across the seam at angle = ±pi so the tongues have no cut.
	n = mix(n, wrap, smoothstep(2.6, 3.14159, abs(angle)) * step(angle, 0.0));
	float body = 1.0 - smoothstep(0.0, 1.0, out_);
	return clamp(body * (0.55 + n * 0.9) - out_ * 0.35, 0.0, 1.0) * step(radius * 0.6, r);
}

// Hot air bending the stage behind: the screen seen through a small displacement (metres on the card), faded in by
// `mask`. The camera faces the plane square-on, so this is exact.
vec4 fire_screen(sampler2D screen, vec2 displacement, float mask, vec3 vertex, mat4 model, mat4 view, mat4 projection, vec2 screen_uv) {
	vec3 world_shift = (model * vec4(displacement * mask, 0.0, 0.0)).xyz;
	vec4 here = projection * vec4(vertex, 1.0);
	vec4 there = projection * vec4(vertex + (view * vec4(world_shift, 0.0)).xyz, 1.0);
	vec2 uv = screen_uv + (there.xy / there.w - here.xy / here.w) * 0.5;
	return vec4(textureLod(screen, uv, 0.0).rgb * mask, mask);
}
