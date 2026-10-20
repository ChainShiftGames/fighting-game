// The key art projected from the round-start camera (stage_projection.gdshaderinc), for floors: shown as painted
// where the stage's lights reach it, tinted toward shadow_color where something blocks them, so fighters cast real
// shadows on a painted floor. No ambient light: the painting already holds the scene's light.
shader_type spatial;
render_mode ambient_light_disabled;

#include "res://Assets/Shaders/stage_projection.gdshaderinc"

// Developer setting (presentation): the painting's colour multiplied by this in full shadow.
uniform vec4 shadow_color : source_color = vec4(0.62, 0.68, 0.82, 1.0);

void fragment() {
	vec4 texel = projected_key_art();
	ALBEDO = texel.rgb;
	ALPHA = texel.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
	ROUGHNESS = 1.0;
	SPECULAR = 0.0;
}

void light() {
	// One stage light gives the painting's own colour; ATTENUATION carries its shadow.
	DIFFUSE_LIGHT += mix(shadow_color.rgb, vec3(1.0), ATTENUATION);
}
