// One side wall of the castle hall: a ground arcade of round arches, a stone band, then a gallery of smaller arches.
// The openings are cut out, so the aisle behind shows through; a lighter stone frames each arch.
shader_type spatial;
render_mode cull_disabled;

#include "res://Assets/Stages/castle_hall/castle_stone.gdshaderinc"
#include "res://Assets/Stages/castle_hall/toon_light.gdshaderinc"

group_uniforms layout;
uniform vec2 wall_size = vec2(24.0, 10.0);
uniform float bay_width = 3.0;
// Width and springing height (where the round top starts) of the ground arches, and of the gallery arches.
uniform vec2 arcade_opening = vec2(2.0, 2.2);
uniform vec2 gallery_opening = vec2(1.4, 4.5);
uniform float gallery_sill = 3.6;
uniform float band_height = 0.35;
uniform float trim_width = 0.22;

group_uniforms colours;
uniform vec3 stone_color : source_color = vec3(0.74, 0.64, 0.5);
uniform vec3 trim_color : source_color = vec3(0.86, 0.77, 0.61);
uniform vec3 mortar_color : source_color = vec3(0.5, 0.42, 0.33);
group_uniforms;

// Below zero inside a round-topped opening; how far outside it otherwise.
float arch_distance(vec2 position, float width, float spring, float sill) {
	if (position.y > spring) {
		return length(vec2(position.x, position.y - spring)) - width * 0.5;
	}
	return max(abs(position.x) - width * 0.5, sill - position.y);
}

void fragment() {
	vec2 position = vec2(UV.x * wall_size.x, (1.0 - UV.y) * wall_size.y);
	vec2 in_bay = vec2(mod(position.x, bay_width) - bay_width * 0.5, position.y);
	float ground = arch_distance(in_bay, arcade_opening.x, arcade_opening.y, -1.0);
	float gallery = arch_distance(in_bay, gallery_opening.x, gallery_opening.y, gallery_sill);
	float opening = min(ground, gallery);
	if (opening < 0.0) {
		discard;
	}

	bool band = position.y > gallery_sill - band_height && position.y < gallery_sill;
	vec3 face = opening < trim_width || band ? trim_color : stone_color;
	vec2 block = opening < trim_width ? vec2(0.35, 0.35) : vec2(0.8, 0.4);
	ALBEDO = stone_courses(position, block, face, mortar_color, 0.1);
	ROUGHNESS = 1.0;
}

void light() {
	DIFFUSE_LIGHT += toon_diffuse(NORMAL, LIGHT, ATTENUATION, LIGHT_COLOR);
}
