// The fire orb's startup at its spawn point: hot air sucks the stage inward, a vortex of flame spirals in and
// tightens, a white-hot seed swells to the orb's first-frame size and throbs faster and faster, and a ring of fire
// slams shut on it just before the orb appears (the art node adds embers spiralling in and the core model forming
// over the last part). `progress` runs 0 to 1 over the startup.
shader_type spatial;
render_mode unshaded, blend_premul_alpha, depth_draw_never, cull_disabled, fog_disabled;

#include "res://Assets/Effects/fire.gdshaderinc"

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

uniform vec2 card_size = vec2(2.6, 2.6);
uniform float progress = 0.0;
// The orb's size on its first frame, as a share of full size: the seed grows to it.
uniform float spawn_size = 0.2;
uniform float t = 0.0;
uniform float spin = 0.0;

const float RING = 0.3;

void vortex(inout vec4 acc, vec2 q, float r, float time) {
	float gather = mix(RING * 3.0, RING * 0.7, progress);
	float angle = atan(q.y, q.x);
	// Spiral arms: the angle twisted by distance, turning faster as it closes.
	float twist = angle + log(max(r, 1e-3)) * 2.4 + spin;
	float arms = 0.5 + 0.5 * sin(twist * 3.0);
	float n = effect_fbm(vec2(twist * 1.3, r * 6.0 - time * 4.0));
	float band = exp(-pow((r - gather) / (RING * (0.9 - 0.4 * progress)), 2.0));
	float heat = band * (0.45 + 0.6 * arms * n) * (0.4 + 0.8 * progress);
	fire_lay(acc, fire_ramp(heat * 1.2), smoothstep(0.1, 0.4, heat) * 0.9);
}

void fragment() {
	vec2 q = (UV - 0.5) * card_size * vec2(1.0, -1.0);
	float r = length(q);
	float time = t;

	float air = (1.0 - smoothstep(RING * 0.8, RING * 3.4, r)) * heat_haze * progress;
	vec2 pull = r > 1e-4 ? -q / r * 0.02 * progress : vec2(0.0);
	pull += (vec2(effect_noise(q * 8.0 + vec2(0.0, -time * 6.0)), effect_noise(q * 8.0 + vec2(4.0, -time * 5.0))) - 0.5) * 0.02;
	vec4 acc = fire_screen(screen_texture, pull, clamp(air, 0.0, 1.0), VERTEX, MODEL_MATRIX, VIEW_MATRIX, PROJECTION_MATRIX, SCREEN_UV);

	// A dull red glow builds round the spawn point.
	fire_add(acc, ember_color.rgb, exp(-r * 4.0) * progress * 0.8);
	vortex(acc, q, r, time);

	// The seed throbs faster as the startup runs out.
	float seed_radius = RING * spawn_size * smoothstep(0.15, 1.0, progress);
	float throb = 0.5 + 0.5 * sin(time * mix(10.0, 34.0, progress));
	float tongues = fire_corona(q, seed_radius, RING * 0.4 * progress, time * 1.5);
	fire_lay(acc, fire_ramp(tongues * 1.2), smoothstep(0.05, 0.3, tongues) * progress);
	float glow = exp(-max(r - seed_radius, 0.0) * 12.0 / max(corona, 0.05)) * smoothstep(0.1, 0.4, progress);
	fire_add(acc, flame_color.rgb * intensity, glow * (0.5 + 0.5 * throb));
	fire_lay(acc, core_color.rgb * intensity, 1.0 - smoothstep(seed_radius * 0.75, seed_radius, r));

	// The snap: a ring of fire slams shut on the seed over the last frames.
	float snap = smoothstep(0.78, 1.0, progress);
	float snap_r = mix(RING * 2.0, seed_radius, snap * snap);
	float ring = 1.0 - smoothstep(0.01, 0.03, abs(r - snap_r));
	fire_add(acc, blaze_color.rgb * intensity * 1.5, ring * snap);

	vec2 edge = min(UV, 1.0 - UV) * card_size;
	acc *= smoothstep(0.0, 0.25, min(edge.x, edge.y));

	ALBEDO = acc.rgb;
	ALPHA = clamp(acc.a, 0.0, 1.0);
}
