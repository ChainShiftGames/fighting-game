// Inverted-hull outline, used as a toon material's next pass. Pushed out in screen space, so the line keeps the
// same width whatever the camera zoom; the width is in pixels at 1080p and scales with the window height. The width
// wobbles along the line like an inked brush stroke (E's Laf++ look), switched with the brush shading.
shader_type spatial;
render_mode unshaded, cull_front, fog_disabled;

uniform sampler2D lit_texture : source_color, filter_linear_mipmap, repeat_enable;
// The ink colour; texture_tint 1 multiplies the texture by it instead, so the line is a darker shade of its part.
uniform vec4 outline_color : source_color = vec4(0.12, 0.1, 0.12, 1.0);
uniform float texture_tint : hint_range(0.0, 1.0) = 0.0;
uniform float outline_width_pixels : hint_range(0.0, 8.0) = 2.2;
uniform float alpha_cutoff : hint_range(0.0, 1.0) = 0.5;
// How much the width varies (0 = even, 1 = from a quarter to nearly twice as wide) and how often, per model unit.
uniform float width_wobble : hint_range(0.0, 1.0) = 0.8;
uniform float wobble_scale = 14.0;
// Set by LookToggles (BrushShading): 0 turns the wobble off with the brush strokes.
global uniform float fighter_brush;

#include "res://Assets/Shaders/brush_noise.gdshaderinc"

void vertex() {
	vec4 clip = PROJECTION_MATRIX * (MODELVIEW_MATRIX * vec4(VERTEX, 1.0));
	vec3 view_normal = normalize(MODELVIEW_NORMAL_MATRIX * NORMAL);
	vec2 screen_direction = (PROJECTION_MATRIX * vec4(view_normal, 0.0)).xy * VIEWPORT_SIZE;
	if (dot(screen_direction, screen_direction) > 0.0) {
		float wobble = mix(1.0, 0.25 + (1.5 * value_noise(VERTEX * wobble_scale)), width_wobble * fighter_brush);
		float pixels = outline_width_pixels * wobble * VIEWPORT_SIZE.y / 1080.0;
		clip.xy += normalize(screen_direction) * pixels * 2.0 / VIEWPORT_SIZE * clip.w;
	}
	POSITION = clip;
}

void fragment() {
	vec4 texel = texture(lit_texture, UV);
	ALBEDO = mix(outline_color.rgb, texel.rgb * outline_color.rgb, texture_tint);
	ALPHA = texel.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
}
