// The turning aura: thin spiral arms swirling round the fireball, dragged out behind it, each with a bright leading
// edge. `spin` (radians) comes from the logic frame.
shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_disabled;

#include "res://Assets/Effects/effect_noise.gdshaderinc"

uniform float t = 0.0;
uniform float spin = 0.0;
uniform float arms = 3.0;
uniform vec4 arm_color : source_color = vec4(0.42, 0.08, 0.78, 1.0);
uniform vec4 edge_color : source_color = vec4(0.85, 0.55, 1.0, 1.0);
uniform vec4 shadow_color : source_color = vec4(0.03, 0.0, 0.06, 1.0);

void fragment() {
	vec2 p = (UV - 0.5) * 2.0;
	p.y = -p.y;
	vec2 shape = p;
	// Dragged out behind (-X) as it flies.
	shape.x *= p.x < 0.0 ? 0.6 : 1.05;
	float radius = length(shape);
	float angle = atan(shape.y, shape.x);
	float wobble = effect_fbm(vec2(angle * 1.2, radius * 3.0 - t * 2.0));
	float phase = fract((arms * angle + radius * 7.0 - spin + wobble * 1.5) / 6.2831853);
	// A sharp front and a long fading tail on each arm, so the turning reads.
	float arm = smoothstep(0.0, 0.08, phase) * (1.0 - smoothstep(0.08, 0.45, phase));
	float leading = smoothstep(0.0, 0.05, phase) * (1.0 - smoothstep(0.05, 0.13, phase));
	float ring = smoothstep(0.25, 0.4, radius) * (1.0 - smoothstep(0.6, 0.97, radius));
	vec3 colour = mix(arm_color.rgb, shadow_color.rgb, smoothstep(0.45, 0.95, radius));
	ALBEDO = mix(colour, edge_color.rgb, leading * (1.0 - smoothstep(0.4, 0.8, radius)));
	ALPHA = clamp(arm * ring * 1.15, 0.0, 1.0);
}
