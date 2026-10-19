// A sagging string of little triangular flags across the hall; the quad's middle sags down by sag metres.
shader_type spatial;
render_mode cull_disabled;

#include "res://Assets/Shaders/stage_toon_light.gdshaderinc"

uniform float flag_count = 24.0;
uniform float sag = 0.8;
uniform vec3 string_color : source_color = vec3(0.2, 0.17, 0.14);
uniform vec3 flag_a : source_color = vec3(0.6, 0.15, 0.13);
uniform vec3 flag_b : source_color = vec3(0.82, 0.68, 0.32);
uniform vec3 flag_c : source_color = vec3(0.18, 0.28, 0.5);
uniform vec3 flag_d : source_color = vec3(0.27, 0.45, 0.3);

void vertex() {
	float across = UV.x * 2.0 - 1.0;
	VERTEX.y -= sag * (1.0 - across * across);
}

void fragment() {
	float cell = floor(UV.x * flag_count);
	float inside = fract(UV.x * flag_count);
	if (UV.y < 0.08) {
		ALBEDO = string_color;
	} else {
		float flag_height = (UV.y - 0.08) / 0.92;
		if (abs(inside * 2.0 - 1.0) > 1.0 - flag_height || inside < 0.08 || inside > 0.92) {
			discard;
		}
		float pick = mod(cell, 4.0);
		ALBEDO = pick < 1.0 ? flag_a : (pick < 2.0 ? flag_b : (pick < 3.0 ? flag_c : flag_d));
	}
	ROUGHNESS = 1.0;
}

void light() {
	DIFFUSE_LIGHT += toon_diffuse(NORMAL, LIGHT, ATTENUATION, LIGHT_COLOR);
}
