// The far end of the castle hall: wood panelling with a doorway below, stone above, and tall stained-glass windows
// that glow on their own.
shader_type spatial;
render_mode cull_disabled;

#include "res://Assets/Stages/castle_hall/castle_stone.gdshaderinc"
#include "res://Assets/Shaders/stage_toon_light.gdshaderinc"

group_uniforms layout;
uniform vec2 wall_size = vec2(25.0, 7.5);
uniform float panel_height = 2.0;
uniform float window_count = 3.0;
uniform float window_spacing = 2.4;
// Width and springing height of each window; its sill height.
uniform vec2 window_opening = vec2(1.2, 5.2);
uniform float window_sill = 3.0;
uniform vec2 door_opening = vec2(1.8, 1.6);
uniform float glass_cell = 0.28;
uniform float glow = 2.2;

group_uniforms colours;
uniform vec3 stone_color : source_color = vec3(0.74, 0.64, 0.5);
uniform vec3 mortar_color : source_color = vec3(0.5, 0.42, 0.33);
uniform vec3 wood_color : source_color = vec3(0.44, 0.29, 0.17);
uniform vec3 lead_color : source_color = vec3(0.06, 0.05, 0.05);
uniform vec3 glass_blue : source_color = vec3(0.2, 0.35, 0.8);
uniform vec3 glass_red : source_color = vec3(0.75, 0.18, 0.15);
uniform vec3 glass_gold : source_color = vec3(0.9, 0.7, 0.25);
uniform vec3 glass_green : source_color = vec3(0.25, 0.6, 0.35);
group_uniforms;

float arch_distance(vec2 position, float width, float spring, float sill) {
	if (position.y > spring) {
		return length(vec2(position.x, position.y - spring)) - width * 0.5;
	}
	return max(abs(position.x) - width * 0.5, sill - position.y);
}

vec3 glass_color(vec2 cell) {
	float pick = stone_hash(cell);
	return pick < 0.35 ? glass_blue : (pick < 0.6 ? glass_red : (pick < 0.85 ? glass_gold : glass_green));
}

void fragment() {
	vec2 position = vec2((UV.x - 0.5) * wall_size.x, (1.0 - UV.y) * wall_size.y);
	float window = 1e3;
	float centre = 0.0;
	for (float i = 0.0; i < window_count; i += 1.0) {
		float x = (i - (window_count - 1.0) * 0.5) * window_spacing;
		float d = arch_distance(vec2(position.x - x, position.y), window_opening.x, window_opening.y, window_sill);
		if (d < window) {
			window = d;
			centre = x;
		}
	}

	float door = arch_distance(position, door_opening.x, door_opening.y, -1.0);
	if (window < 0.0) {
		vec2 glass = (position - vec2(centre, 0.0)) / glass_cell;
		vec2 inside = fract(glass);
		float lead = 1.0 - smoothstep(0.04, 0.09, min(min(inside.x, 1.0 - inside.x), min(inside.y, 1.0 - inside.y)));
		lead = max(lead, 1.0 - smoothstep(0.03, 0.06, -window));
		vec3 colour = mix(glass_color(floor(glass) + centre), lead_color, lead);
		ALBEDO = colour * 0.3;
		EMISSION = colour * glow * (1.0 - lead);
	} else if (door < 0.0) {
		ALBEDO = vec3(0.04, 0.03, 0.03);
	} else if (position.y < panel_height) {
		float plank = floor(position.x / 0.6);
		float seam = 1.0 - smoothstep(0.0, 0.03, abs(fract(position.x / 0.6) - 0.5) * 0.6 - 0.27);
		ALBEDO = wood_color * (0.85 + 0.3 * stone_hash(vec2(plank, 3.0))) * (1.0 - 0.4 * seam);
		ALBEDO *= door < 0.15 ? 0.7 : 1.0;
	} else {
		bool frame = window < 0.2 || position.y < panel_height + 0.25;
		ALBEDO = stone_courses(position, vec2(0.8, 0.4), frame ? stone_color * 1.2 : stone_color, mortar_color, 0.1);
	}
	ROUGHNESS = 1.0;
}

void light() {
	DIFFUSE_LIGHT += toon_diffuse(NORMAL, LIGHT, ATTENUATION, LIGHT_COLOR);
}
