// A hanging heraldic banner: a coloured field, a gold border and a diamond, with a swallowtail cut at the bottom.
shader_type spatial;
render_mode cull_disabled;

#include "res://Assets/Shaders/stage_toon_light.gdshaderinc"

uniform vec3 field_color : source_color = vec3(0.55, 0.12, 0.12);
uniform vec3 trim_color : source_color = vec3(0.85, 0.66, 0.25);
uniform float border = 0.07;
uniform float tail_depth = 0.12;

void fragment() {
	float tail = (1.0 - tail_depth) + tail_depth * abs(UV.x * 2.0 - 1.0);
	if (UV.y > tail) {
		discard;
	}

	vec2 middle = abs(UV - vec2(0.5, 0.42)) * vec2(1.0, 0.55);
	float diamond = middle.x + middle.y;
	bool emblem = diamond < 0.18 && diamond > 0.12;
	bool edge = UV.x < border || UV.x > 1.0 - border || UV.y < border * 0.5 || UV.y > tail - border * 0.5;
	ALBEDO = edge || emblem ? trim_color : field_color;
	ROUGHNESS = 1.0;
}

void light() {
	DIFFUSE_LIGHT += toon_diffuse(NORMAL, LIGHT, ATTENUATION, LIGHT_COLOR);
}
