// A glowing HUD gauge fill: a rounded bar with a three-colour gradient along it, a soft highlight and an outer glow.
// The node is the bar grown by glow_size on every side, so the glow has room.
shader_type canvas_item;

uniform vec2 node_size = vec2(100.0, 24.0);
uniform float glow_size = 6.0;
uniform float corner_radius = 8.0;
uniform vec4 start_color : source_color = vec4(0.35, 0.72, 1.0, 1.0);
uniform vec4 middle_color : source_color = vec4(0.25, 0.82, 0.76, 1.0);
uniform vec4 end_color : source_color = vec4(0.73, 1.0, 0.94, 1.0);
uniform vec4 glow_color : source_color = vec4(0.56, 0.96, 0.87, 0.55);
// 1 runs the gradient right to left, for P2's mirrored side.
uniform float flip = 0.0;

float rounded_box(vec2 p, vec2 half_size, float radius) {
	vec2 q = abs(p) - half_size + radius;
	return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
}

void fragment() {
	vec2 bar = max(node_size - 2.0 * glow_size, vec2(0.0));
	vec2 p = UV * node_size - node_size * 0.5;
	float radius = min(corner_radius, min(bar.x, bar.y) * 0.5);
	float d = rounded_box(p, bar * 0.5, radius);
	float along = clamp(p.x / max(bar.x, 1.0) + 0.5, 0.0, 1.0);
	along = mix(along, 1.0 - along, flip);
	vec3 fill = along < 0.5
		? mix(start_color.rgb, middle_color.rgb, along * 2.0)
		: mix(middle_color.rgb, end_color.rgb, along * 2.0 - 1.0);
	// A pale band along the top edge reads as light on glass.
	float across = clamp(p.y / max(bar.y, 1.0) + 0.5, 0.0, 1.0);
	fill = mix(fill, vec3(1.0), 0.35 * (1.0 - smoothstep(0.15, 0.4, across)));
	float inside = clamp(0.5 - d, 0.0, 1.0);
	float glow = glow_color.a * exp(-3.0 * max(d, 0.0) / max(glow_size, 0.001)) * (1.0 - inside);
	COLOR = vec4(mix(glow_color.rgb, fill, inside), max(inside * start_color.a, glow));
}
