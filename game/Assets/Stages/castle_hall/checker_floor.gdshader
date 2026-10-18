// The castle hall's floor: cream and green tiles in world space, kept low in contrast so the fighters stand out.
shader_type spatial;

#include "res://Assets/Stages/castle_hall/toon_light.gdshaderinc"

uniform vec3 light_tile : source_color = vec3(0.62, 0.56, 0.45);
uniform vec3 dark_tile : source_color = vec3(0.42, 0.46, 0.38);
uniform vec3 grout : source_color = vec3(0.34, 0.31, 0.26);
uniform float tile_size = 1.0;

varying vec3 world_position;

void vertex() {
	world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec2 tile = world_position.xz / tile_size;
	vec2 cell = floor(tile);
	vec2 inside = fract(tile);
	float edge = min(min(inside.x, 1.0 - inside.x), min(inside.y, 1.0 - inside.y)) * tile_size;
	vec3 face = mod(cell.x + cell.y, 2.0) < 1.0 ? light_tile : dark_tile;
	ALBEDO = mix(face, grout, 1.0 - smoothstep(0.01, 0.022, edge));
	ROUGHNESS = 0.8;
}

void light() {
	DIFFUSE_LIGHT += toon_diffuse(NORMAL, LIGHT, ATTENUATION, LIGHT_COLOR);
}
