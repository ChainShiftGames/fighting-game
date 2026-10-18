// Inverted-hull outline, used as a toon material's next pass. Pushed out in screen space, so the line keeps the
// same width whatever the camera zoom (Xrd-style constant-thickness outlines); the width is in pixels at 1080p and
// scales with the window height.
shader_type spatial;
render_mode unshaded, cull_front;

uniform sampler2D lit_texture : source_color, filter_linear_mipmap, repeat_enable;
// Multiplies the texture, so the line takes on a darker shade of the part it outlines.
uniform vec4 outline_color : source_color = vec4(0.3, 0.25, 0.3, 1.0);
uniform float outline_width_pixels : hint_range(0.0, 8.0) = 3.0;
uniform float alpha_cutoff : hint_range(0.0, 1.0) = 0.5;

void vertex() {
	vec4 clip = PROJECTION_MATRIX * (MODELVIEW_MATRIX * vec4(VERTEX, 1.0));
	vec3 view_normal = normalize(MODELVIEW_NORMAL_MATRIX * NORMAL);
	vec2 screen_direction = (PROJECTION_MATRIX * vec4(view_normal, 0.0)).xy * VIEWPORT_SIZE;
	if (dot(screen_direction, screen_direction) > 0.0) {
		float pixels = outline_width_pixels * VIEWPORT_SIZE.y / 1080.0;
		clip.xy += normalize(screen_direction) * pixels * 2.0 / VIEWPORT_SIZE * clip.w;
	}
	POSITION = clip;
}

void fragment() {
	vec4 texel = texture(lit_texture, UV);
	ALBEDO = texel.rgb * outline_color.rgb;
	ALPHA = texel.a;
	ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
}
