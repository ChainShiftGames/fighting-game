// The key art projected from the round-start camera (stage_projection.gdshaderinc). The painting already holds its
// light, so no lighting here; fog still fades it with distance. For a floor that shows the fighters' shadows:
// stage_projection_shadowed.
shader_type spatial;
render_mode unshaded;

#include "res://Assets/Shaders/stage_projection.gdshaderinc"

void fragment() {
	vec4 texel = projected_key_art();
	ALBEDO = texel.rgb;
	ALPHA = texel.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
}
