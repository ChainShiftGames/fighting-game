// Plain stone (or wood, with long thin blocks) for the castle hall's solid parts, mapped in world space so blocks keep
// their size on any mesh.
shader_type spatial;

#include "res://Assets/Stages/castle_hall/castle_stone.gdshaderinc"
#include "res://Assets/Stages/castle_hall/toon_light.gdshaderinc"

uniform vec3 stone_color : source_color = vec3(0.74, 0.64, 0.5);
uniform vec3 mortar_color : source_color = vec3(0.5, 0.42, 0.33);
uniform vec2 block_size = vec2(0.8, 0.4);
uniform float variation : hint_range(0.0, 0.5) = 0.12;

varying vec3 world_position;
varying vec3 world_normal;

void vertex() {
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}

void fragment() {
	vec3 facing = abs(world_normal);
	vec2 position = facing.y > 0.5 ? world_position.xz : (facing.x > 0.5 ? world_position.zy : world_position.xy);
	ALBEDO = stone_courses(position, block_size, stone_color, mortar_color, variation);
	ROUGHNESS = 1.0;
}

void light() {
	DIFFUSE_LIGHT += toon_diffuse(NORMAL, LIGHT, ATTENUATION, LIGHT_COLOR);
}
