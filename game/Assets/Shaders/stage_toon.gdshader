// Toon material for stage floors and props (ADR-0030): lit by the stage's own lights in hard bands and faded by fog
// with distance, unlike the fighters' toon shader. For an outline, add toon_outline as the next pass, thinner than
// the fighters'.
shader_type spatial;

#include "res://Assets/Shaders/stage_toon_light.gdshaderinc"

uniform sampler2D albedo_texture : source_color, filter_linear_mipmap, repeat_enable, hint_default_white;
uniform vec4 albedo_color : source_color = vec4(1.0);
// Repeats a tileable texture across large surfaces such as the floor.
uniform vec2 uv_scale = vec2(1.0);
uniform float alpha_cutoff : hint_range(0.0, 1.0) = 0.5;

void fragment() {
	vec4 texel = texture(albedo_texture, UV * uv_scale) * albedo_color;
	ALBEDO = texel.rgb;
	ALPHA = texel.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
	ROUGHNESS = 1.0;
}

void light() {
	DIFFUSE_LIGHT += toon_diffuse(NORMAL, LIGHT, ATTENUATION, LIGHT_COLOR);
}
