// The VFX Lab's hitbox outline (dev only): a thin frame round a card the hitbox's size.
shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never;

uniform vec2 box_size = vec2(0.43, 0.43);
uniform vec4 colour : source_color = vec4(1.0, 0.25, 0.3, 1.0);
uniform float thickness = 0.012;

void fragment() {
	vec2 from_edge = box_size * 0.5 - abs(UV - 0.5) * box_size;
	ALBEDO = colour.rgb;
	ALPHA = step(min(from_edge.x, from_edge.y), thickness) * colour.a;
}
