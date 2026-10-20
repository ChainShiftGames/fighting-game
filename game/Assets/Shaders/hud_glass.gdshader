// Frosted Aether glass for the HUD: the stage behind is blurred and tinted inside a rounded box with a bright rim.
shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;
// The panel's size in pixels, so the corners stay round at any aspect.
uniform vec2 panel_size = vec2(100.0, 24.0);
uniform float corner_radius = 12.0;
uniform float blur_lod = 2.5;
uniform vec4 tint : source_color = vec4(0.07, 0.2, 0.25, 0.42);
uniform vec4 rim_color : source_color = vec4(1.0, 1.0, 1.0, 0.75);
uniform float rim_width = 1.5;

float rounded_box(vec2 p, vec2 half_size, float radius) {
	vec2 q = abs(p) - half_size + radius;
	return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
}

void fragment() {
	vec2 p = (UV - 0.5) * panel_size;
	float radius = min(corner_radius, min(panel_size.x, panel_size.y) * 0.5);
	float d = rounded_box(p, panel_size * 0.5, radius);
	float inside = clamp(0.5 - d, 0.0, 1.0);
	float rim = clamp(d + rim_width + 0.5, 0.0, 1.0);
	vec3 behind = textureLod(screen_texture, SCREEN_UV, blur_lod).rgb;
	vec3 glass = mix(behind, tint.rgb, tint.a);
	COLOR = vec4(mix(glass, rim_color.rgb, rim * rim_color.a), inside);
}
