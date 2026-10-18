// First toon pass (P4-B): a fixed light per material instead of the scene's lights, so the model always reads the
// same, and MToon's two-tone shading (shift moves the light/shade border, toony hardens it) so VRoid values carry over.

uniform sampler2D lit_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform vec4 lit_color : source_color = vec4(1.0);
uniform vec4 shade_color : source_color = vec4(0.8, 0.75, 0.85, 1.0);
uniform float shading_shift : hint_range(-1.0, 1.0) = 0.0;
uniform float shading_toony : hint_range(0.0, 1.0) = 0.95;
// World space, towards the light. Upper left front, as most fighting-game key lights.
uniform vec3 light_direction = vec3(-0.4, 0.7, 0.6);
uniform float alpha_cutoff : hint_range(0.0, 1.0) = 0.5;

void fragment() {
	vec4 texel = texture(lit_texture, UV);
	vec3 view_normal = FRONT_FACING ? NORMAL : -NORMAL;
	vec3 world_normal = normalize((INV_VIEW_MATRIX * vec4(view_normal, 0.0)).xyz);
	float shading = dot(world_normal, normalize(light_direction)) + shading_shift;
	float lit = smoothstep(-1.0 + shading_toony, 1.0 - shading_toony, shading);
	ALBEDO = texel.rgb * mix(shade_color.rgb, lit_color.rgb, lit);
	ALPHA = texel.a * lit_color.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
}
