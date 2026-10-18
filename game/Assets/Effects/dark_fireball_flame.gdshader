// Black flames licking back from the core, edged in violet; a card facing the camera, travel along +X.
shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_disabled;

#include "res://Assets/Effects/effect_noise.gdshaderinc"

uniform float t = 0.0;
uniform vec4 inner_color : source_color = vec4(0.02, 0.0, 0.04, 1.0);
uniform vec4 edge_color : source_color = vec4(0.55, 0.12, 0.95, 1.0);

void fragment() {
	vec2 p = (UV - 0.5) * 2.0;
	p.y = -p.y;
	vec2 shape = p;
	// The flames reach further back than forward.
	shape.x *= p.x < 0.0 ? 0.5 : 1.15;
	float flow = effect_fbm(vec2(p.x * 3.0 + t * 7.0, p.y * 3.5));
	float dist = length(shape) + (flow - 0.5) * 0.7;
	float body = 1.0 - smoothstep(0.42, 0.8, dist);
	float edge = smoothstep(0.3, 0.75, dist);
	ALBEDO = mix(inner_color.rgb, edge_color.rgb, edge);
	ALPHA = body;
}
